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
    public class AccessAudioModel
    {

        public class AudioModelResponse
        {
            public bool human_voice_detected { get; set; }
            public double human_voice_score { get; set; }
        }

        public async Task SendAudioToModel(string audioPath)
        {
            string baseUrl = ConfigurationManager.AppSettings["Base_url"];
            var baseUrlEndPoint = $"{baseUrl.TrimEnd('/')}/examSession/updateSession";

            try
            {
                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(audioPath))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                    form.Add(fileContent, "file", Path.GetFileName(audioPath));

                    var response = await client.PostAsync("http://127.0.0.1:5000/predict-audio", form);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Audio prediction: " + result);

                    // Parse response
                    var parsedResult = JsonConvert.DeserializeObject<AudioModelResponse>(result);

                    if (parsedResult.human_voice_detected)
                    {
                        
                        Console.WriteLine("Human voice detected during exam!");
                        // MessageBox.Show("Human voice detected during exam!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    
                    var backendPayload = new
                    {
                        
                        studentId = StudentSession.Id,
                        inputType = "audio",
                        modelOutput = parsedResult.human_voice_detected.ToString(), 
                        confidence = parsedResult.human_voice_score.ToString("F6")
                    };

                    var backendContent = new StringContent(JsonConvert.SerializeObject(backendPayload), Encoding.UTF8, "application/json");
                    var backendResponse = await client.PostAsync(baseUrlEndPoint, backendContent);
                    var backendResult = await backendResponse.Content.ReadAsStringAsync();

                    Console.WriteLine("Sent to backend: " + backendResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending audio to model: " + ex.Message);
            }
        }
    }
}
