using Exam_proctor.Sessions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Exam_proctor.APIClient
{
    public class SendCopyPasteText
    {

        public async Task SendToBackend(string pastedText)
        {
            try
            {
                var payload = new
                {
                    studentId = StudentSession.Id,
                    pastedText = pastedText,
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync("http://localhost:3000/api/examSession/addCopiedText", content);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Copied text sent to backend: " + result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending copied text: " + ex.Message);
            }
        }
    }
}
