using NAudio.Wave;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Exam_proctor
{
    public partial class Form1 : Form
    {

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;

        private GlobalKeyboardHook _keyLogger;


        private WaveInEvent waveIn;
        private List<byte> audioBuffer = new List<byte>();

        private System.Windows.Forms.Timer audioSaveTimer;
        private int audioFileIndex = 0;

        public Form1()
        {
            InitializeComponent();

            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Exam Monitor";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            this.Resize += new EventHandler(Form1_Resize);

            this.Load += Form1_Load; // Attach Form1_Load

        }


        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.Visible = true;
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }


        public void StartWebcamCapture()
        {
            new Thread(() =>
            {
                var capture = new VideoCapture(0);
                var mat = new Mat();
                while (true)
                {
                    capture.Read(mat);
                    if (!mat.Empty())
                    {
                        string path = $"frames/frame_{DateTime.Now.Ticks}.jpg";
                        Cv2.ImWrite(path, mat);
                        // You can also send to your model here
                        // 🔁 Send this frame to the Flask server
                        _ = SendFrameToVideoModel(path);
                        _ = SendFrameToHumanModel(path);
                    }
                    Thread.Sleep(1000); // Capture every 1 sec
                }
            }).Start();
        }



        public void StartAudioCapture()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            waveIn.DataAvailable += (s, a) =>
            {
                audioBuffer.AddRange(a.Buffer.Take(a.BytesRecorded));
                // Optional: save every 5 seconds or send to model
            };
            waveIn.StartRecording();
        }




        private void Form1_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory("frames"); // For webcam images
            Directory.CreateDirectory("audio");  // For audio WAVs

            StartWebcamCapture();
            StartAudioCapture();

            _keyLogger = new GlobalKeyboardHook();
            _keyLogger.KeyPressed += async (s, key) =>
            {
                Console.WriteLine($"Key Pressed: {key}");
                File.AppendAllText("keylog.txt", $"{key} ");

                // 🧪 Simulated feature array until real extractor is built
                double[] testFeatures = new double[]
                {
            70, 100, 120, 80,
            150, 180, 120,
            250, 270, 200,
            160, 180, 140
                };

                await SendKeylogToModel(testFeatures);
            };

            // Audio saving timer
            audioSaveTimer = new System.Windows.Forms.Timer();
            audioSaveTimer.Interval = 10000; // Save audio every 10 seconds
            audioSaveTimer.Tick += AudioSaveTimer_Tick;
            audioSaveTimer.Start();
        }



        private void AudioSaveTimer_Tick(object sender, EventArgs e)
        {
            if (audioBuffer.Count > 0)
            {
                string path = $"audio/audio_{audioFileIndex++}.wav";

                using (var writer = new WaveFileWriter(path, new WaveFormat(16000, 1)))
                {
                    writer.Write(audioBuffer.ToArray(), 0, audioBuffer.Count);
                }

                audioBuffer.Clear(); // Clear buffer for next batch
                Console.WriteLine($"Audio saved to: {path}");


                SendAudioToModel(path);
            }
        }




















        private async Task SendKeylogToModel(double[] features)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var json = new { features = features };
                    var jsonString = JsonConvert.SerializeObject(json);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://127.0.0.1:5002/predict-keylog", content);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Keylog prediction: " + result);

                    // Optional alert logic
                    if (result.Contains("0.9")) // simple threshold check
                    {
                        MessageBox.Show("⚠️ Suspicious typing detected!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending keylog to model: " + ex.Message);
            }
        }




        private async Task SendFrameToVideoModel(string imagePath)
        {
            try
            {
                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(imagePath))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    form.Add(fileContent, "file", Path.GetFileName(imagePath));

                    var response = await client.PostAsync("http://127.0.0.1:5003/predict-video", form);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Video prediction: " + result);

                    if (result.Contains("\"cheating\": true"))
                    {
                        MessageBox.Show("🚨 Cheating behavior detected!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending frame to video model: " + ex.Message);
            }
        }



        private async Task SendFrameToHumanModel(string imagePath)
        {
            try
            {
                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(imagePath))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    form.Add(fileContent, "file", Path.GetFileName(imagePath));

                    var response = await client.PostAsync("http://127.0.0.1:5005/predict-humans", form);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Human detection: " + result);

                    if (result.Contains("\"cheating\": true"))
                    {
                        MessageBox.Show("⚠ Multiple humans detected! Possible cheating.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending frame to human detection model: " + ex.Message);
            }
        }




        private async Task SendAudioToModel(string audioPath)
        {
            try
            {
                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(audioPath))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                    form.Add(fileContent, "file", Path.GetFileName(audioPath));

                    var response = await client.PostAsync("http://127.0.0.1:5006/predict-audio", form);
                    var result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Audio prediction: " + result);

                    if (result.Contains("\"human_voice_detected\": true"))
                    {
                        MessageBox.Show("🗣️ Human voice detected during exam!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending audio to model: " + ex.Message);
            }
        }









    }
}
