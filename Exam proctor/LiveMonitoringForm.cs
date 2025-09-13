using Exam_proctor.DTO;
using Exam_proctor.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor
{
    public partial class LiveMonitoringForm : Form
    {
        // Keep the actual event list
        private readonly BindingList<ProctorUpdate> _events = new BindingList<ProctorUpdate>();
        // BindingSource protects the grid from re-entrancy issues
        private readonly BindingSource _bs = new BindingSource();

        private Label lblHumans;
        private Label lblAudio;
        private Label lblCheatingObjects;
        private Label lblFacePose;
        private Label lblKeystroke;
        private Label lblPaste;

        private DataGridView grid;

        public LiveMonitoringForm()
        {
            InitializeComponent();

            Text = "Proctor – Live Monitoring";
            Width = 620;
            Height = 640;
            TopMost = true;  

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8),
                WrapContents = true
            };

            lblHumans = MakeBadge("Humans");
            lblAudio = MakeBadge("Audio");
            lblCheatingObjects = MakeBadge("Objects");
            lblFacePose = MakeBadge("Face/Pose");
            lblKeystroke = MakeBadge("Keystroke");
            lblPaste = MakeBadge("Paste");

            panel.Controls.AddRange(new Control[]
            {
                lblHumans, lblAudio, lblCheatingObjects, lblFacePose, lblKeystroke, lblPaste
            });

            // Bind BindingSource to the events list
            _bs.DataSource = _events;

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false, 
                DataSource = _bs
            };

            // Define columns explicitly
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Source",
                HeaderText = "Source",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ModelOutput",
                HeaderText = "ModelOutput",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Confidence",
                HeaderText = "Confidence",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Timestamp",
                HeaderText = "Timestamp",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Extra",
                HeaderText = "Extra",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            Controls.Add(grid);
            Controls.Add(panel);

            // Subscribe to the hub
            ProctorUpdatesHub.Instance.UpdateReceived += OnUpdateReceived;

            // Seed badges from current snapshot
            RefreshBadgesFromLatest();
        }

        private Label MakeBadge(string title)
        {
            return new Label
            {
                Text = $"{title}: —",
                AutoSize = false,
                Width = 180,
                Height = 36,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(6)
            };
        }

        private void OnUpdateReceived(object sender, ProctorUpdate e)
        {
            if (IsDisposed) return;

            BeginInvoke((MethodInvoker)(() =>
            {
                // Prevent mid-paint re-entrancy
                _bs.RaiseListChangedEvents = false;
                try
                {
                    _events.Add(e);
                    if (_events.Count > 500)
                        _events.RemoveAt(0);
                }
                finally
                {
                    _bs.RaiseListChangedEvents = true;
                    _bs.ResetBindings(false);
                }

                UpdateBadge(e);
            }));
        }

        private void UpdateBadge(ProctorUpdate u)
        {
            void SetBadge(Label lbl, ProctorUpdate up)
            {
                lbl.Text = $"{lbl.Text.Split(':')[0]}: {up.ModelOutput}" +
                           (string.IsNullOrEmpty(up.Confidence) ? "" : $" ({up.Confidence})");

                var mo = up.ModelOutput ?? "";
                bool isAlert =
                    mo.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    mo.IndexOf("human", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    mo.IndexOf("cheat", StringComparison.OrdinalIgnoreCase) >= 0;

                lbl.BackColor = isAlert ? Color.LightCoral : Color.LightGreen;
            }

            switch (u.Source)
            {
                case "multiHuman":
                case "human":
                    SetBadge(lblHumans, u);
                    break;

                case "audio":
                    SetBadge(lblAudio, u);
                    break;

                case "cheatingObjects":
                    SetBadge(lblCheatingObjects, u);
                    break;

                case "facePose":
                    SetBadge(lblFacePose, u);
                    break;

                case "keystroke":
                    SetBadge(lblKeystroke, u);
                    break;

                case "paste":
                    SetBadge(lblPaste, u);
                    break;
            }
        }

        private void RefreshBadgesFromLatest()
        {
            var hub = ProctorUpdatesHub.Instance;
            foreach (var src in new[] { "multiHuman", "audio", "cheatingObjects", "facePose", "keystroke", "paste" })
            {
                var u = hub.GetLatest(src);
                if (u != null) UpdateBadge(u);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ProctorUpdatesHub.Instance.UpdateReceived -= OnUpdateReceived;
            base.OnFormClosed(e);
        }
    }
}
