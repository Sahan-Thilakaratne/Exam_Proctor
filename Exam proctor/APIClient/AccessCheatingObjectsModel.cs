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
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.APIClient
{
    public class AccessCheatingObjectsModel
    {

        public class Detection
        {
            public string label { get; set; }
            public double confidence { get; set; }
        }


        public class CheatingObjectModelResponse
        {
            public bool cheating { get; set; }
            [JsonProperty("detections")]           // <-- map API key "detections" -> detection
            public List<Detection> detection { get; set; }
        }

        public async Task SendFrameToVideoModel(string imagePath)
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

                    var response = await client.PostAsync("http://127.0.0.1:5000/predict-objects", form);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Video prediction: " + result);

                    // Deserialize string into C# object
                    var parsedResult = JsonConvert.DeserializeObject<CheatingObjectModelResponse>(result);

                    //realtime update
                    ProctorUpdatesHub.Instance.Publish(new ProctorUpdate
                    {
                        Source = "cheatingObjects",
                        ModelOutput = parsedResult.cheating ? "true" : "false",
                        Confidence = null, 
                        Extra = Path.GetFileName(imagePath)
                    });

                    /*if (result.Contains("\"cheating\": true"))
                    {
                        MessageBox.Show("Cheating behavior detected!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }*/

                    // Send result to backend
                    var backendPayload = new
                    {
                        //studentId = "688b4d8e932af0d839d77290",
                        studentId = StudentSession.Id,
                        inputType = "cheatingObjects",
                        //inputDataId = Path.GetFileName(imagePath),
                        modelOutput = parsedResult.cheating.ToString(),
                        confidence = parsedResult?.detection != null
        ? string.Join(" | ", parsedResult.detection.Select(d => $"{d.label}:{d.confidence}"))
        : ""
                    };

                    var json = new StringContent(JsonConvert.SerializeObject(backendPayload), Encoding.UTF8, "application/json");
                    var backendResponse = await client.PostAsync(baseUrlEndPoint, json);
                    var backendResult = await backendResponse.Content.ReadAsStringAsync();

                    Console.WriteLine("Sent to backend: " + backendResult);


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending frame to video model: " + ex.Message);
            }
        }

    }
}
