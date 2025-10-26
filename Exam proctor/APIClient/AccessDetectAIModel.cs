using Exam_proctor.DTO;
using Exam_proctor.Services;
using Exam_proctor.Sessions;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Exam_proctor.APIClient.AccessKeyStrokeModel;

namespace Exam_proctor.APIClient
{
    public class AccessDetectAIModel
    {
        private static string _sessionId;
        private class AIModelResponse
        {

            
            public string prediction { get; set; } 
            public double? confidence { get; set; } 
        }

        public  class Variables
        {
            public string SId { get; set; }
        }

        public async Task SendTextToModel(string pastedText)
        {

            string apiUrl = ConfigurationManager.AppSettings["Api_urll"];
            string baseUrl = ConfigurationManager.AppSettings["Base_url"];
            var baseUrlEndPoint = $"{baseUrl.TrimEnd('/')}/examSession/addCopiedText";
            var endpoint = $"{apiUrl.TrimEnd('/')}/detect-ai";

            try
            {
                using (var client = new HttpClient())
                {
                    
                    var modelPayload = new { text = pastedText };
                    var modelPayloadString = JsonConvert.SerializeObject(modelPayload);
                    var modelContent = new StringContent(modelPayloadString, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(endpoint, modelContent);
                    var result = await response.Content.ReadAsStringAsync();

                    var parsed = JsonConvert.DeserializeObject<AIModelResponse>(result);

                    //realtime update
                    bool outPut = false;
                    if (parsed.prediction == "ai")
                    {
                        outPut = true;
                    }
                    ProctorUpdatesHub.Instance.Publish(new ProctorUpdate
                    {
                        Source = "paste",
                        ModelOutput = outPut ? "true" : "false",
                        Confidence = null, 
                        Extra = null
                    });


                    var modelOutput = parsed?.prediction ?? "unknown";
                    double modelConfidence = parsed?.confidence ?? (modelOutput.Equals("ai", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0);

                    Console.WriteLine("Paste text prediction: " + result);

                    
                    var backendPayload = new
                    {
                        studentId = StudentSession.Id,
                        pastedText = pastedText,
                        modelOutput = modelOutput,
                        confidence = modelConfidence,
                        timestamp = DateTime.UtcNow.ToString("o")
                    };

                    var backendPayloadString = JsonConvert.SerializeObject(backendPayload);
                    var backendContent = new StringContent(backendPayloadString, Encoding.UTF8, "application/json");

                    //var backendResponse = await client.PostAsync("http://localhost:3000/api/examSession/addCopiedText", backendContent);
                    var backendResponse = await client.PostAsync(baseUrlEndPoint, backendContent);
                    var backendResult = await backendResponse.Content.ReadAsStringAsync();
                    dynamic obj = JsonConvert.DeserializeObject(backendResult);
                    _sessionId = _sessionId ?? (string)(obj?.sessionId ?? obj?.data?.sessionId ?? obj?.SessionId);
                    //MessageBox.Show("sessionid: ", _sessionId);

                    byte[] png = ScreenCapture.CaptureAllScreensToPng();

                    if (png != null && png.Length > 0)
                    {
                       // MessageBox.Show("got it");
                        await UploadPasteScreenshotAsync(png);
                    }



                    Console.WriteLine("Copied text + detectAI saved: " + backendResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending text to model: " + ex.Message);
            }
        }



        private static async Task UploadPasteScreenshotAsync(byte[] pngBytes)
        {
            if (pngBytes == null || pngBytes.Length == 0)
            {
                MessageBox.Show("empty image, skipping upload.");
                return;
            }

            try
            {
                string baseUrl = ConfigurationManager.AppSettings["Base_url"]; // if this already has /api, keep it
                string studentId = StudentSession.Id ?? string.Empty;
                string sessionId = _sessionId ?? string.Empty;

                // Put ids in the query so multer sees them immediately
                string endpoint = string.Concat(
                    baseUrl.TrimEnd('/'),
                    "/paste/upload?studentId=", Uri.EscapeDataString(studentId),
                    "&sessionId=", Uri.EscapeDataString(sessionId)
                );

                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                {
                    // Also send them in headers
                    client.DefaultRequestHeaders.Remove("x-student-id");
                    client.DefaultRequestHeaders.Remove("x-session-id");
                    client.DefaultRequestHeaders.Add("x-student-id", studentId);
                    client.DefaultRequestHeaders.Add("x-session-id", sessionId);

                    // And still add fields first in the multipart body
                    form.Add(new StringContent(studentId), "studentId");
                    form.Add(new StringContent(sessionId), "sessionId");
                    form.Add(new StringContent(DateTime.UtcNow.ToString("o")), "capturedAt");

                    using (var fileContent = new ByteArrayContent(pngBytes))
                    {
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                        string filename = $"paste-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png";
                        form.Add(fileContent, "file", filename);

                        var res = await client.PostAsync(endpoint, form).ConfigureAwait(false);
                        var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                        Console.WriteLine("Paste screenshot uploaded: " + res.StatusCode + " " + body);
                        MessageBox.Show("Paste screenshot uploaded: " + res.StatusCode + " " + body);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("UploadPasteScreenshotAsync error: " + ex.Message);
                MessageBox.Show("UploadPasteScreenshotAsync error: " + ex.Message);
            }
        }





    }




}
