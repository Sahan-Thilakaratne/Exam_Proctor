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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.APIClient
{
    public class AccessFacePoseModel
    {

        public class FacePoseModelResponse
        {
            public bool cheating { get; set; }
            public string pose { get; set; }
        }

        public async Task SendFrameToFacePoseModel(string imagePath)
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
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    form.Add(fileContent, "file", Path.GetFileName(imagePath));

                    var response = await client.PostAsync("http://127.0.0.1:5000/predict-face-pose", form);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Face Pose: " + result);

                    var parsedResult = JsonConvert.DeserializeObject<FacePoseModelResponse>(result);

                    //realtime update
                    ProctorUpdatesHub.Instance.Publish(new ProctorUpdate
                    {
                        Source = "facePose",
                        ModelOutput = parsedResult.cheating ? "true" : "false",
                        Confidence = parsedResult.pose.ToString(), // or keep empty if not a score
                        Extra = Path.GetFileName(imagePath)
                    });


                    var backendPayload = new
                    {
                        
                        studentId = StudentSession.Id,
                        inputType = "facePose",
                        //inputDataId = Path.GetFileName(imagePath),
                        modelOutput = parsedResult.cheating.ToString(),
                        confidence = 0
                    };

                    var json = new StringContent(JsonConvert.SerializeObject(backendPayload), Encoding.UTF8, "application/json");
                    //var backendResponse = await client.PostAsync("http://localhost:3000/api/examSession/updateSession", json);
                    var backendResponse = await client.PostAsync(baseUrlEndPoint, json);
                    var backendResult = await backendResponse.Content.ReadAsStringAsync();

                    Console.WriteLine("Sent to backend: " + backendResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending frame to face pose model or when sending result to database: " + ex.Message);
            }
        }
    }
}
