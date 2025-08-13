using Exam_proctor.APIClient;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.Services
{
    public class KeyStrokeCapture
    {

        private GlobalKeyboardHook _keyLogger;
        private HashSet<Keys> _keysPressed = new HashSet<Keys>();
        private Dictionary<Keys, long> keyDownTimes = new Dictionary<Keys, long>();
        private List<long> holdTimes = new List<long>();
        private List<long> interKeyLatencies = new List<long>();
        private List<long> digraphDurations = new List<long>();
        private long lastKeyDownTime = -1;
        private WaveInEvent waveIn;
        private List<byte> audioBuffer = new List<byte>();
        private System.Windows.Forms.Timer plagiarismCheckTimer;
        private System.Windows.Forms.Timer paraphraseCheckTimer;
        private StringBuilder recentTypedBuffer = new StringBuilder();
        private object bufferLock = new object();

        public void StartKeyStrokeCapture()
        {
            _keyLogger = new GlobalKeyboardHook();

            _keyLogger.KeyDownTimestamp += (s, evt) =>
            {


                var (key, timestamp) = evt;

                // Track keys currently pressed
                _keysPressed.Add(key);

                // Detect Ctrl + V
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control && key == Keys.V)
                {
                    HandlePasteAction();
                }
                // Detect Shift + Insert
                else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift && key == Keys.Insert)
                {
                    HandlePasteAction();
                }

                keyDownTimes[key] = timestamp;

                if (lastKeyDownTime > 0)
                    interKeyLatencies.Add(timestamp - lastKeyDownTime);

                lastKeyDownTime = timestamp;
            };

            _keyLogger.KeyUpTimestamp += async (s, evt) =>
            {
                var (key, timestamp) = evt;

                _keysPressed.Remove(key); // clear released keys

                if (keyDownTimes.ContainsKey(key))
                {
                    long holdTime = timestamp - keyDownTimes[key];
                    holdTimes.Add(holdTime);
                    digraphDurations.Add(holdTime);


                    //  Append typed character to buffer
                    lock (bufferLock)
                    {
                        if (key >= Keys.A && key <= Keys.Z)
                            recentTypedBuffer.Append(key.ToString().ToLower());
                        else if (key == Keys.Space)
                            recentTypedBuffer.Append(" ");
                        else if (key == Keys.OemPeriod)
                            recentTypedBuffer.Append(".");


                    }


                    if (holdTimes.Count >= 15)
                    {
                        double[] features = ExtractFeatures(holdTimes, interKeyLatencies, digraphDurations);
                        //await SendKeylogToModel(features);
                        await new AccessKeyStrokeModel().SendKeylogToModel(features);

                        holdTimes.Clear();
                        interKeyLatencies.Clear();
                        digraphDurations.Clear();
                        keyDownTimes.Clear();
                        lastKeyDownTime = -1;
                    }
                }
            };
        }


        public async void HandlePasteAction()
        {
            try
            {
                string pastedText = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(pastedText))
                {
                    //MessageBox.Show("Copy paste detected: ", pastedText);
                    await new SendCopyPasteText().SendToBackend(pastedText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to read clipboard: " + ex.Message);
            }
        }




        public double[] ExtractFeatures(List<long> hold, List<long> latency, List<long> digraph)
        {
            double typingSpeed = latency.Count >= 1 ? 60000.0 / latency.Average() : 0;
            int backspaces = File.Exists("keylog.txt") ? File.ReadAllText("keylog.txt").Count(c => c == '\b') : 0;

            return new double[]
            {
        typingSpeed,                     // 0
        hold.Average(),                  // 1
        hold.Max(),                      // 2
        hold.Min(),                      // 3
        latency.Average(),               // 4
        latency.Max(),                   // 5
        latency.Min(),                   // 6
        digraph.Average(),               // 7
        digraph.Max(),                   // 8
        digraph.Min(),                   // 9
        GetInterReleaseAverage(),        // 10
        150,                             // 11: max inter-release (placeholder)
        90                               // 12: min inter-release (placeholder)
            };
        }

        private double GetInterReleaseAverage()
        {

            return 120;
        }
    }
}
