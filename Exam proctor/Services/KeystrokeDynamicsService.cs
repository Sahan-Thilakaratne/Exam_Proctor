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

            _keyDownTimes[key] = ts;

            if (_lastKeyDownTime > 0)
                _interKeyLatencies.Add(ts - _lastKeyDownTime);

            _lastKeyDownTime = ts;
        }

        private async void OnKeyUp(object sender, (Keys key, long timestamp) e)
        {
            var key = e.key;
            var ts = e.timestamp;

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
