using Exam_proctor.Sessions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.Services
{
    public static class BackgroundAppMonitor
    {

        private static HashSet<string> trackedApps = new HashSet<string>();
        private static List<(string name, string path, DateTime detectedAt)> newApps = new List<(string, string, DateTime)>();
        private static Thread monitoringThread;

        public static void StartMonitoring()
        {
            //MessageBox.Show("Starttttttttttttttttttttttttt");

            monitoringThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Process[] processes = Process.GetProcesses();

                        foreach (var proc in processes)
                        {
                            try
                            {
                                string name = proc.ProcessName;
                                string path = proc.MainModule?.FileName;

                                if (!trackedApps.Contains(name))
                                {
                                    trackedApps.Add(name);
                                    newApps.Add((name, path, DateTime.Now));
                                    //MessageBox.Show($"APP NAME: {name}");
                                    //Console.WriteLine("Tracked apps: " + string.Join(", ", trackedApps));

                                }
                            }
                            catch { } // Ignore protected/system processes
                        }
                    }
                    catch { }

                    Thread.Sleep(TimeSpan.FromMinutes(0.01)); // wait 5 minutes
                }
            });

            monitoringThread.IsBackground = true;
            monitoringThread.Start();
        }

        public static List<object> GetDetectedApps()
        {
            return newApps.Select(app => new
            {
                name = app.name,
                path = app.path,
                firstDetectedAt = app.detectedAt
            }).ToList<object>();
        }



        public static async Task SendBackroundAppData()
        {
            try
            {


                using (var client = new HttpClient())
                {


                    var apps = GetDetectedApps();

                    var payload = new
                    {
                        studentId = StudentSession.Id,
                        apps = apps
                    };

                    var json = JsonConvert.SerializeObject(payload);

                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    Console.WriteLine("Here the contentt: ", content);

                    var responseString = await client.PostAsync("http://localhost:3000/api/examSession/addBackgroundAppData", content);

                    string resContent = await responseString.Content.ReadAsStringAsync();
                    Console.WriteLine("Send BackgroundApp Data Response: " + resContent);

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while sending background app data: ", ex.ToString());
            }

        }
    }
}
