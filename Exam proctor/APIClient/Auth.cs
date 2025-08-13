using Exam_proctor.Services;
using Exam_proctor.Sessions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Exam_proctor.APIClient
{
    public class Auth
    {
        private String email;
        private String password;

        public async Task LoginAsync(String email, String password, Form parentForm)
        {

            string baseUrl = ConfigurationManager.AppSettings["Base_url"];
            var baseUrlEndPoint = $"{baseUrl.TrimEnd('/')}/student/login";
            

            var loadingForm = new LoadingForm();
            loadingForm.Show(parentForm);
            loadingForm.BringToFront();
                
            
            using (var client = new HttpClient())
            {
                var payload = new
                {
                    email = email,
                    password = password
                };


  
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(baseUrlEndPoint, content);

                    var responseString = await response.Content.ReadAsStringAsync();

                    loadingForm.Close();

                    if (response.IsSuccessStatusCode)
                    {
                        // MessageBox.Show("Login successful!\n" + responseString, "Success");
                        DialogResult result = MessageBox.Show(
                                 "You're about to start the exam proctor application.\n\n" +
                                    "You may minimize this window and start your exam using any application.\n" +
                                     "We will capture your video, audio and keystrokes you type.\n\n" +
                                 "If you're okay with this, please press OK to proceed.",
                             "Warning",
                             MessageBoxButtons.OKCancel,
                                 MessageBoxIcon.Warning
                                 );

                        if (result == DialogResult.OK)
                        {
                            var jsonObject = JsonConvert.DeserializeObject<dynamic>(responseString);
                            var student = jsonObject.student;

                            StudentSession.Id = student._id;
                            StudentSession.StudentCustomId = student.studentId;
                            StudentSession.Namef = student.nameF;
                            StudentSession.NameL = student.nameL;
                            StudentSession.Email = student.email;

                            BackgroundAppMonitor.StartMonitoring();

                            
                            if (parentForm is Form1 form1)
                            {
                                form1.TriggerLoadManually(); 
                            }

                            if (result == DialogResult.OK)
                            {
                                // Close the login form and open the exam form
                                if (parentForm is Form1 form2)
                                {
                                    form2.Hide(); 
                                    var examForm = new ExamForm();
                                    examForm.Show(); // Show exam interface
                                }
                            }


                        }
                        else
                        {
                            Application.Exit();  // Exit app if user clicks Cancel
                        }
                    }
                    else
                    {
                        MessageBox.Show("Login failed!\n" + responseString, "Error");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Exception");
                }
            }


        }




        //Logout 
        public static async Task<bool> logoutAsync()
        {
            string baseUrl = ConfigurationManager.AppSettings["Base_url"];
            var baseUrlEndPointLogOut = $"{baseUrl.TrimEnd('/')}/examSession/endExamSession";

            using (var client = new HttpClient())
            {


                var payload = new
                {
                    //studentId = "688b4d8e932af0d839d77290",
                    studentId = StudentSession.Id

                };



                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(baseUrlEndPointLogOut, content);

                    var responseString = await response.Content.ReadAsStringAsync();

                    

                    StudentSession.Clear();

                    return response.IsSuccessStatusCode;

                }
                catch (Exception ex)
                {

                    MessageBox.Show("Error: " + ex.Message, "Exception");
                    return false;
                }


            }
        }

    }
}








