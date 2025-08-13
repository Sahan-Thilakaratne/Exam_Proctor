using Exam_proctor.APIClient;
using Exam_proctor.Services;
using Exam_proctor.Sessions;
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
    public partial class ExamForm : Form
    {
        public ExamForm()
        {
            InitializeComponent();

            lableNameValue.Text = $"Logged in as {StudentSession.Namef} {StudentSession.NameL}";
            lableStudentIdValue.Text = $"Student ID: {StudentSession.StudentCustomId}";
            loggedInAtValue.Text = $"Logged in at: {DateTime.Now}";

        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                MessageBox.Show("Please use the 'Logout' button to close the application.");
                e.Cancel = true; // Prevent closing
            }
            base.OnFormClosing(e);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false; 

            Form1 mainForm = Application.OpenForms["Form1"] as Form1;

            
            mainForm?.CleanShutdown();

            
            await BackgroundAppMonitor.SendBackroundAppData();

            
            await Task.Delay(1000);

            
            bool logoutSuccess = await Auth.logoutAsync();

            if (logoutSuccess)
            {
                

                
                Environment.Exit(0);
            }
            else
            {
                MessageBox.Show("Logout failed. Please try again.", "Error");
                button1.Enabled = true;
            }
        }

    }
}

