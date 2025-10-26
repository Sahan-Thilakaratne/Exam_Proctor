using Exam_proctor.DTO;
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
    public class AccessDetectPlagiarsm
    {
        private class AIModelResponse
        {
            public string prediction { get; set; }
            public double? confidence { get; set; }
        }

        public async Task SendTextToModel(string typedText)
        {

            string apiUrl = ConfigurationManager.AppSettings["Api_urll"];
            string baseUrl = ConfigurationManager.AppSettings["Base_url"];
            var baseUrlEndPoint = $"{baseUrl.TrimEnd('/')}/examSession/addTypedText";
            var endpoint = $"{apiUrl.TrimEnd('/')}/detect-ai";

            try
            {
                using (var client = new HttpClient())
                {

                    var modelPayload = new { text = typedText };
                    var modelPayloadString = JsonConvert.SerializeObject(modelPayload);
                    var modelContent = new StringContent(modelPayloadString, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(endpoint, modelContent);
                    var result = await response.Content.ReadAsStringAsync();

                    var parsed = JsonConvert.DeserializeObject<AIModelResponse>(result);

                    //realtime update
                    //bool outPut = false;
                    //if (parsed.prediction == "ai")
                    //{
                    //    outPut = true;
                    //}
                    //ProctorUpdatesHub.Instance.Publish(new ProctorUpdate
                    //{
                    //    Source = "paste",
                    //    ModelOutput = outPut ? "true" : "false",
                    //    Confidence = null,
                    //    Extra = null
                    //});


                    var modelOutput = parsed?.prediction ?? "unknown";
                    double modelConfidence = parsed?.confidence ?? (modelOutput.Equals("ai", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0);

                    Console.WriteLine("Typed text prediction: " + result);
                    //MessageBox.Show("Typed text prediction: " + result);


                    var backendPayload = new
                    {
                        studentId = StudentSession.Id,
                        typedText = typedText,
                        modelOutput = modelOutput,
                        confidence = modelConfidence,
                        timestamp = DateTime.UtcNow.ToString("o")
                    };

                    var backendPayloadString = JsonConvert.SerializeObject(backendPayload);
                    var backendContent = new StringContent(backendPayloadString, Encoding.UTF8, "application/json");

                    //var backendResponse = await client.PostAsync("http://localhost:3000/api/examSession/addCopiedText", backendContent);
                    var backendResponse = await client.PostAsync(baseUrlEndPoint, backendContent);
                    var backendResult = await backendResponse.Content.ReadAsStringAsync();

                    Console.WriteLine("Typed text + detectAI saved: " + backendResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending text to model: " + ex.Message);
            }
        }
    }
}
