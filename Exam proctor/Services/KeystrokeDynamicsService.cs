using Exam_proctor.APIClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.Services
{
    public class KeystrokeDynamicsService :IDisposable
    {

        private readonly GlobalKeyboardHook _hook;
        private readonly Dictionary<Keys, long> _keyDownTimes = new Dictionary<Keys, long>();
        private readonly List<long> _holdTimes = new List<long>();
        private readonly List<long> _interKeyLatencies = new List<long>();
        private readonly List<long> _digraphDurations = new List<long>();
        private long _lastKeyDownTime = -1;

        public event EventHandler<double[]> FeaturesReady;

        // 🔹 NEW: text capture state
        private readonly object _textLock = new object();
        private readonly StringBuilder _currentWord = new StringBuilder();
        private readonly List<string> _words = new List<string>();
        private const int WORD_BATCH_SIZE = 50;
        private bool _shiftDown = false;

        // 🔹 NEW: AI client for sending 30-word batches
        private readonly AccessDetectPlagiarsm _aiClient = new AccessDetectPlagiarsm();


        public KeystrokeDynamicsService()
        {
            _hook = new GlobalKeyboardHook();
            _hook.KeyDownTimestamp += OnKeyDown;
            _hook.KeyUpTimestamp += OnKeyUp;
        }

        private void OnKeyDown(object sender, (Keys key, long timestamp) e)
        {
            var key = e.key;
            var ts = e.timestamp;

            // 🔹 track shift state
            if (key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey)
                _shiftDown = true;


            _keyDownTimes[key] = ts;

            if (_lastKeyDownTime > 0)
                _interKeyLatencies.Add(ts - _lastKeyDownTime);

            _lastKeyDownTime = ts;
        }

        private async void OnKeyUp(object sender, (Keys key, long timestamp) e)
        {
            var key = e.key;
            var ts = e.timestamp;

            // 🔹 update shift state on release
            if (key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey)
            {
                _shiftDown = false;
            }



            if (_keyDownTimes.ContainsKey(key))
            {
                long hold = ts - _keyDownTimes[key];
                _holdTimes.Add(hold);
                _digraphDurations.Add(hold);
                _keyDownTimes.Remove(key);

                if (_holdTimes.Count >= 15)
                {
                    var features = ExtractFeatures(_holdTimes, _interKeyLatencies, _digraphDurations);
                    FeaturesReady?.Invoke(this, features);

                    await new AccessKeyStrokeModel().SendKeylogToModel(features);

                    _holdTimes.Clear();
                    _interKeyLatencies.Clear();
                    _digraphDurations.Clear();
                    _keyDownTimes.Clear();
                    _lastKeyDownTime = -1;
                }
            }



            // 🔹 TEXT CAPTURE LOGIC (letters, digits, punctuation, space, backspace)
            char? ch = TryTranslateKeyToChar(key, _shiftDown, Control.IsKeyLocked(Keys.CapsLock));
            if (key == Keys.Back)
            {
                lock (_textLock)
                {
                    if (_currentWord.Length > 0)
                    {
                        _currentWord.Length = _currentWord.Length - 1;
                    }
                    else if (_words.Count > 0)
                    {
                        // allow backspacing into the previous word (optional)
                        var last = _words[_words.Count - 1];
                        if (last.Length > 0)
                        {
                            _currentWord.Append(last.Substring(0, last.Length - 1));
                            _words.RemoveAt(_words.Count - 1);
                        }
                    }
                }
                return;
            }

            if (key == Keys.Space || key == Keys.Enter || key == Keys.Tab)
            {
                // word boundary
                await FinalizeWordIfAnyAndSendIfFull();
                return;
            }

            if (ch.HasValue)
            {
                lock (_textLock)
                {
                    _currentWord.Append(ch.Value);
                }
            }



        }



        // 🔹 finalize a word on whitespace and send batches of 30 words
        private async Task FinalizeWordIfAnyAndSendIfFull()
        {
            string toSend = null;

            lock (_textLock)
            {
                if (_currentWord.Length > 0)
                {
                    _words.Add(_currentWord.ToString());
                    _currentWord.Clear();
                }

                if (_words.Count >= WORD_BATCH_SIZE)
                {
                    toSend = string.Join(" ", _words.Take(WORD_BATCH_SIZE));
                    _words.RemoveRange(0, WORD_BATCH_SIZE);
                }
            }

            if (!string.IsNullOrWhiteSpace(toSend))
            {
                try
                {
                    await _aiClient.SendTextToModel(toSend);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending 30-word batch to AI model: " + ex.Message);
                }
            }
        }



        // 🔹 Map Keys -> char with Shift/Caps handling for common keys
        private static char? TryTranslateKeyToChar(Keys key, bool shiftDown, bool capsLock)
        {
            // Letters
            if (key >= Keys.A && key <= Keys.Z)
            {
                char baseChar = (char)('a' + (key - Keys.A));
                bool upper = capsLock ^ shiftDown; // XOR for letters
                return upper ? char.ToUpper(baseChar) : baseChar;
            }

            // Digits (top row)
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                int num = (int)(key - Keys.D0);
                if (!shiftDown) return (char)('0' + num);

                // Shifted symbols for 0–9
                switch (num)
                {
                    case 1: return '!';
                    case 2: return '@';
                    case 3: return '#';
                    case 4: return '$';
                    case 5: return '%';
                    case 6: return '^';
                    case 7: return '&';
                    case 8: return '*';
                    case 9: return '(';
                    case 0: return ')';
                    default: return (char?)null;
                }
            }

            // NumPad digits
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return (char)('0' + (key - Keys.NumPad0));

            // Common punctuation (US layout)
            switch (key)
            {
                case Keys.OemMinus: return shiftDown ? '_' : '-';
                case Keys.Oemplus: return shiftDown ? '+' : '=';
                case Keys.OemOpenBrackets: return shiftDown ? '{' : '[';
                case Keys.Oem6: return shiftDown ? '}' : ']'; // close bracket
                case Keys.Oem5: return shiftDown ? '|' : '\\'; // backslash/pipe
                case Keys.Oem1: return shiftDown ? ':' : ';';  // semicolon/colon
                case Keys.Oem7: return shiftDown ? '"' : '\''; // quote/double-quote
                case Keys.Oemcomma: return shiftDown ? '<' : ',';  // comma/less-than
                case Keys.OemPeriod: return shiftDown ? '>' : '.';  // period/greater-than
                case Keys.OemQuestion: return shiftDown ? '?' : '/';  // slash/question (aka Oem2)
                case Keys.Oemtilde: return shiftDown ? '~' : '`';  // backtick/tilde
                default: return (char?)null;
            }
        }






        private static double[] ExtractFeatures(List<long> hold, List<long> latency, List<long> digraph)
        {
            double typingSpeed = latency.Count >= 1 ? 60000.0 / latency.Average() : 0;
            int backspaces = File.Exists("keylog.txt")
                ? File.ReadAllText("keylog.txt").Count(c => c == '\b')
                : 0;

            return new double[]
            {
                typingSpeed,                 // 0
                hold.Average(),              // 1
                hold.Max(),                  // 2
                hold.Min(),                  // 3
                latency.Average(),           // 4
                latency.Max(),               // 5
                latency.Min(),               // 6
                digraph.Average(),           // 7
                digraph.Max(),               // 8
                digraph.Min(),               // 9
                GetInterReleaseAverage(),    // 10
                150,                         // 11 placeholder
                90                           // 12 placeholder
            };
        }

        private static double GetInterReleaseAverage()
        {
            return 120;
        }

        public void Dispose()
        {
            if (_hook != null)
                _hook.Dispose();
        }
    }
}
