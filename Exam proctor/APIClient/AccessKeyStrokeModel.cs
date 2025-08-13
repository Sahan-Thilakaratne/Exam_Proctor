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
using static Exam_proctor.APIClient.AccessCheatingObjectsModel;

namespace Exam_proctor.APIClient
{
    public class AccessKeyStrokeModel
    {

        public class Prediction
        {
            public string prediction {  get; set; }
        }


        public async Task SendKeylogToModel(double[] features)
        {

            string baseUrl = ConfigurationManager.AppSettings["Base_url"];
            var baseUrlEndPoint = $"{baseUrl.TrimEnd('/')}/examSession/updateSession";

            try
            {
                using (var client = new HttpClient())
                {
                    // Step 1: Send features to ML model
                    var modelPayload = new { features = features };
                    var modelPayloadString = JsonConvert.SerializeObject(modelPayload);
                    var modelContent = new StringContent(modelPayloadString, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://127.0.0.1:5000/predict-keylog", modelContent);
                    var result = await response.Content.ReadAsStringAsync();

                    dynamic obj = JsonConvert.DeserializeObject(result);
                    double score = obj.prediction[0][0];

                    Console.WriteLine("Keylog prediction: " + result);

                    

                    // Step 3: Send model result to backend
                    var backendPayload = new
                    {
                        //studentId = "688b4d8e932af0d839d77290",
                        studentId = StudentSession.Id,
                        inputType = "keystroke",
                        modelOutput = score.ToString("F6"),
                        confidence = 1.0 // or extract from model output
                    };

                    var backendPayloadString = JsonConvert.SerializeObject(backendPayload);
                    var backendContent = new StringContent(backendPayloadString, Encoding.UTF8, "application/json");

                    var backendResponse = await client.PostAsync(baseUrlEndPoint, backendContent);
                    var backendResult = await backendResponse.Content.ReadAsStringAsync();

                    Console.WriteLine("Sent to backend: " + backendResult);

                    // Optional alert
                    if (score > 0.95)
                    {
                        //MessageBox.Show("⚠️ Suspicious typing detected!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Console.WriteLine("⚠️ Suspicious typing detected!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending keylog to model: " + ex.Message);

            }
        }

    }
}
