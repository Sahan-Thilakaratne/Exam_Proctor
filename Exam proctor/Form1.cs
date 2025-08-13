using Exam_proctor.APIClient;
using Exam_proctor.Services;
using NAudio.Wave;
using Newtonsoft.Json;
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

namespace Exam_proctor
{
    public partial class Form1 : Form
    {

        //Form design
        private Label lblWelcome;
        private Label lblEmail;
        private TextBox txtEmail;
        private Label lblPassword;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblStatus;


        /// <summary>
        /// ////////////////
        /// </summary>
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;

        private GlobalKeyboardHook _keyLogger;

        private HashSet<Keys> _keysPressed = new HashSet<Keys>();



        private WaveInEvent waveIn;
        private List<byte> audioBuffer = new List<byte>();

        private System.Windows.Forms.Timer audioSaveTimer;
        private int audioFileIndex = 0;

        private Dictionary<Keys, long> keyDownTimes = new Dictionary<Keys, long>();
        private List<long> holdTimes = new List<long>();
        private List<long> interKeyLatencies = new List<long>();
        private List<long> digraphDurations = new List<long>();
        private long lastKeyDownTime = -1;

        //Plagia
        private System.Windows.Forms.Timer plagiarismCheckTimer;
        private System.Windows.Forms.Timer paraphraseCheckTimer;
        private StringBuilder recentTypedBuffer = new StringBuilder();
        private object bufferLock = new object();



        private static readonly string AppDataRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExamProctor");
        private static readonly string FramesDir = Path.Combine(AppDataRoot, "frames");
        private static readonly string AudioDir = Path.Combine(AppDataRoot, "audio");


        //Video capture threads helpers
        private Thread webcamThread;
        private VideoCapture webcamCapture;
        private bool webcamRunning = false;


        private KeystrokeDynamicsService _keystrokeService;
        private PasteDetectionService _pasteService;

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

            //this.Load += Form1_Load; // Attach Form1_Load

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 30;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
            path.AddArc(0, Height - radius, radius, radius, 90, 90);
            this.Region = new Region(path);
        }


        public void TriggerLoadManually()
        {
            
            MessageBox.Show("Proctoring components are now starting...");

                
            Form1_Load(this, EventArgs.Empty);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(AppDataRoot);
            Directory.CreateDirectory(FramesDir);
            Directory.CreateDirectory(AudioDir);

            StartWebcamCapture();
            StartAudioCapture();

            audioSaveTimer = new System.Windows.Forms.Timer();
            audioSaveTimer.Interval = 10000;
            audioSaveTimer.Tick += AudioSaveTimer_Tick;
            audioSaveTimer.Start();


           
            _keystrokeService = new KeystrokeDynamicsService();
            _pasteService = new PasteDetectionService();


            //Plagiarism Timers
            //System.Windows.Forms.Timer textCheckTimer = new System.Windows.Forms.Timer();
            //textCheckTimer.Interval = 20000; // 20 seconds
            //textCheckTimer.Tick += TextCheckTimer_Tick;
            //textCheckTimer.Start();
           

            
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




        private async void btnLogin1_Click(object sender, EventArgs e)
        {
            Auth auth = new Auth();
            await auth.LoginAsync(email.Text, password.Text, this);
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        public void StartWebcamCapture()
        {
            webcamCapture = new VideoCapture(0);
            webcamRunning = true;

            webcamThread = new Thread(async () =>
            {
                var mat = new Mat();
                while (webcamRunning)
                {
                    webcamCapture.Read(mat);
                    if (!mat.Empty())
                    {
                        var fileName = $"frame_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
                        var path = Path.Combine(FramesDir, fileName);
                        Cv2.ImWrite(path, mat);

                        //_ = new AccessCheatingObjectsModel().SendFrameToVideoModel(path);
                        //_ = new AccessHumanModel().SendFrameToHumanModel(path);
                        //_ = new AccessFacePoseModel().SendFrameToFacePoseModel(path);

                        await new AccessCheatingObjectsModel().SendFrameToVideoModel(path);
                        await new AccessHumanModel().SendFrameToHumanModel(path);
                        await new AccessFacePoseModel().SendFrameToFacePoseModel(path);
                    }

                    Thread.Sleep(2000); // 2 seconds
                }

                // Cleanup 
                webcamCapture.Release();
                webcamCapture.Dispose();
                mat.Dispose();
            });

            webcamThread.IsBackground = true;
            webcamThread.Start();
        }




        public void StartAudioCapture()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            waveIn.DataAvailable += (s, a) =>
            {
                lock (audioBuffer)
                {
                    audioBuffer.AddRange(a.Buffer.Take(a.BytesRecorded));
                }
            };

            waveIn.StartRecording();
        }

     


        private void AudioSaveTimer_Tick(object sender, EventArgs e)
        {
            byte[] bufferToWrite;
            lock (audioBuffer)
            {
                bufferToWrite = audioBuffer.ToArray();
                audioBuffer.Clear();
            }

            if (bufferToWrite.Length > 0)
            {
                var fileName = $"audio_{audioFileIndex++}.wav";
                var path = Path.Combine(AudioDir, fileName);

                using (var writer = new WaveFileWriter(path, new WaveFormat(16000, 1)))
                {
                    writer.Write(bufferToWrite, 0, bufferToWrite.Length);
                }

                Console.WriteLine($"Audio processed: {path}");
                new AccessAudioModel().SendAudioToModel(path);
            }
        }







        //Plagiarism
        //private async void TextCheckTimer_Tick(object sender, EventArgs e)
        //{
        //    string studentText = "";

        //    lock (bufferLock)
        //    {
        //        studentText = recentTypedBuffer.ToString().Trim();
        //        recentTypedBuffer.Clear(); // Clear after sending
        //    }

        //    if (!string.IsNullOrWhiteSpace(studentText))
        //    {
        //        Console.WriteLine("📝 Sending typed content:\n" + studentText);

        //        //await CheckPlagiarism(studentText);

        //        // Optional: Provide a known reference answer
        //        string referenceText = "The quick brown fox jumps over the lazy dog."; // or load from file/db
        //        //await CheckParaphrase(studentText, referenceText);
        //    }
        //}


        //private async Task CheckParaphrase(string text1, string text2)
        //{
        //    try
        //    {
        //        var client = new HttpClient();
        //        var data = new
        //        {
        //            text1 = text1,
        //            text2 = text2
        //        };
        //        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        //        var response = await client.PostAsync("http://127.0.0.1:5000/check-paraphrase", content);
        //        var result = await response.Content.ReadAsStringAsync();

        //        Console.WriteLine("Paraphrase check: " + result);
        //        // You can also parse and display in MessageBox
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error: " + ex.Message);
        //    }
        //}




        //private async Task CheckPlagiarism(string text)
        //{
        //    try
        //    {
        //        var client = new HttpClient();
        //        var data = new { text = text };
        //        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        //        var response = await client.PostAsync("http://127.0.0.1:5000/check-plagiarism", content);
        //        var result = await response.Content.ReadAsStringAsync();

        //        Console.WriteLine("Plagiarism check: " + result);
        //        // Show alert if result contains "AI"
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error: " + ex.Message);
        //    }
        //}






        //Clean Shutdown
        public void CleanShutdown()
        {
            try
            {
                audioSaveTimer?.Stop();
                audioSaveTimer?.Dispose();

                plagiarismCheckTimer?.Stop();
                plagiarismCheckTimer?.Dispose();

                paraphraseCheckTimer?.Stop();
                paraphraseCheckTimer?.Dispose();

                waveIn?.StopRecording();
                waveIn?.Dispose();

                
                _pasteService?.Dispose();
                _keystrokeService?.Dispose();

                // Stop webcam
                webcamRunning = false;
                if (webcamThread != null && webcamThread.IsAlive)
                {
                    webcamThread.Join(3000);
                }

                //_keyLogger?.Dispose();

                trayIcon.Visible = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during shutdown: " + ex.Message);
            }
        }





    }
}
