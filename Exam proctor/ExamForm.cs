using Exam_proctor.APIClient;
using Exam_proctor.Services;
using Exam_proctor.Sessions;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor
{
    public partial class ExamForm : Form
    {
        private Guna2BorderlessForm borderless;
        private Guna2Panel card;
        private Guna2HtmlLabel lblName;
        private Guna2HtmlLabel lblStudentId;
        private Guna2HtmlLabel lblLoggedAt;
        private Guna2Button btnLogout;

        private LiveMonitoringForm _monitorForm;


        public ExamForm()
        {
            InitializeComponent();
            ProctorUpdatesHub.Instance.InitializeOnUIThread();

            if (SynchronizationContext.Current != null)
            {
                // Safe if you picked the Shown/OnHandleCreated approach already
                // ProctorUpdatesHub.Instance.InitializeOnUIThread(); // not needed if already done
            }

            // Create & show monitoring form once
            if (_monitorForm == null || _monitorForm.IsDisposed)
            {
                _monitorForm = new LiveMonitoringForm
                {
                    TopMost = true // optional, you already set this inside the form
                };
                _monitorForm.Show();
                _monitorForm.BringToFront();
            }

            // Remove legacy controls created by the Designer
            RemoveLegacyExamControls();

            // Modern window + background
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 10f);

            borderless = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 20, TransparentWhileDrag = true, ShadowColor = Color.Black };

            BuildExamCard();

            // fill values
            lblName.Text = $"Logged in as <b>{StudentSession.Namef} {StudentSession.NameL}</b>";
            lblStudentId.Text = $"Student ID: <b>{StudentSession.StudentCustomId}</b>";
            lblLoggedAt.Text = $"Logged in at: <b>{DateTime.Now}</b>";
        }


        private void RemoveLegacyExamControls()
        {
            string[] legacy = { "button1", "label1", "labelName", "lableNameValue", "label3", "lableStudentIdValue", "loggedInAtValue" };
            foreach (var n in legacy)
            {
                var c = this.Controls.Find(n, true).FirstOrDefault();
                c?.Dispose();
            }
        }

        private void BuildExamCard()
        {
            card = new Guna2Panel
            {
                Size = new Size(640, 360),
                FillColor = Color.White,
                BorderRadius = 16,
                ShadowDecoration = { Enabled = true },
                Anchor = AnchorStyles.None
            };
            Controls.Add(card);

            // center on load + resize
            void center() => card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2);
            this.Load += (s, e) => center();
            this.Resize += (s, e) => center();

            var title = new Label
            {
                Text = "Exam Session",
                Dock = DockStyle.Top,
                Height = 70,
                Font = new Font("Segoe UI Semibold", 20f),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(24, 24, 27)
            };
            card.Controls.Add(title);

            lblName = new Guna2HtmlLabel
            {
                BackColor = Color.Transparent,
                Location = new Point(40, 90),
                AutoSize = false,
                Size = new Size(560, 30)
            };
            lblStudentId = new Guna2HtmlLabel
            {
                BackColor = Color.Transparent,
                Location = new Point(40, 125),
                AutoSize = false,
                Size = new Size(560, 30)
            };
            lblLoggedAt = new Guna2HtmlLabel
            {
                BackColor = Color.Transparent,
                Location = new Point(40, 160),
                AutoSize = false,
                Size = new Size(560, 30)
            };

            var tip = new Label
            {
                Text = "You may close this window after your exam finishes.",
                Location = new Point(40, 210),
                AutoSize = false,
                Size = new Size(560, 40),
                Font = new Font("Segoe UI", 12f),
            };

            btnLogout = new Guna2Button
            {
                Text = "Logout",
                BorderRadius = 12,
                Size = new Size(200, 44),
                Location = new Point((card.Width - 200) / 2, 270),
                Anchor = AnchorStyles.Bottom,
                FillColor = Color.FromArgb(59, 130, 246)
            };
            btnLogout.Click += async (s, e) => await LogoutAsync();

            card.Controls.Add(lblName);
            card.Controls.Add(lblStudentId);
            card.Controls.Add(lblLoggedAt);
            card.Controls.Add(tip);
            card.Controls.Add(btnLogout);
        }

        private async Task LogoutAsync()
        {
            btnLogout.Enabled = false;

            var mainForm = Application.OpenForms["Form1"] as Form1;
            mainForm?.CleanShutdown();

            await BackgroundAppMonitor.SendBackroundAppData();
            await Task.Delay(1000);

            if (await Auth.logoutAsync())
                Environment.Exit(0);
            else
            {
                MessageBox.Show("Logout failed. Please try again.", "Error");
                btnLogout.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                MessageBox.Show("Please use the 'Logout' button to close the application.");
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

    }
}

