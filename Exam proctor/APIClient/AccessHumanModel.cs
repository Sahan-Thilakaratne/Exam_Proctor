using Exam_proctor.DTO;
using Exam_proctor.Services;
using Exam_proctor.Sessions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.APIClient
{

    public class HumanModelResponse
    {
        public bool cheating { get; set; }
        public int humans_detected { get; set; }
    }
    public class AccessHumanModel
    {

        public async Task SendFrameToHumanModel(string imagePath)
        {

            string baseUrl = ConfigurationManager.AppSettings["Base_url"];
            var baseUrlEndPoint = $"{baseUrl.TrimEnd('/')}/examSession/updateSession";

            try
            {
                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(imagePath))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    form.Add(fileContent, "file", Path.GetFileName(imagePath));

                    var response = await client.PostAsync("http://127.0.0.1:5000/predict-humans", form);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Human detection: " + result);

                    // Deserialize string into C# object
                    var parsedResult = JsonConvert.DeserializeObject<HumanModelResponse>(result);

                    //realtime update
                    ProctorUpdatesHub.Instance.Publish(new ProctorUpdate
                    {
                        Source = "multiHuman",
                        ModelOutput = parsedResult.cheating ? "true" : "false",
                        Confidence = parsedResult.humans_detected.ToString(), 
                        Extra = Path.GetFileName(imagePath)
                    });


                    if (parsedResult.cheating)
                    {
                        //MessageBox.Show(" Multiple humans detected! Possible cheating.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Console.WriteLine(" Multiple humans detected! Possible cheating." );
                    }


                    // Send result to backend
                        var backendPayload = new
                            {
                            //studentId = "688b4d8e932af0d839d77290",
                            studentId = StudentSession.Id,
                            inputType = "multiHUman",
                                //inputDataId = Path.GetFileName(imagePath),
                                modelOutput = parsedResult.cheating.ToString(),
                                confidence = ""
                            };

                    var json = new StringContent(JsonConvert.SerializeObject(backendPayload), Encoding.UTF8, "application/json");
                    var backendResponse = await client.PostAsync(baseUrlEndPoint, json);
                    var backendResult = await backendResponse.Content.ReadAsStringAsync();

                    Console.WriteLine("Sent to backend: " + backendResult);





                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending frame to human detection model: " + ex.Message);
            }
        }
    }
}
