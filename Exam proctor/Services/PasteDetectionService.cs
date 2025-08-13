using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.Services
{
    public class PasteDetectionService : IDisposable
    {

        private readonly GlobalKeyboardHook _hook;

        public PasteDetectionService()
        {
            _hook = new GlobalKeyboardHook();
            _hook.KeyDownTimestamp += OnKeyDown;
        }

        private async void OnKeyDown(object sender, (Keys key, long timestamp) e)
        {
            var (key, _) = e;

            // Detect Ctrl+V and Shift+Insert 
            bool isCtrlV = (Control.ModifierKeys & Keys.Control) == Keys.Control && key == Keys.V;
            bool isShiftInsert = (Control.ModifierKeys & Keys.Shift) == Keys.Shift && key == Keys.Insert;

            if (isCtrlV || isShiftInsert)
                await HandlePasteAction();
        }

        private static async Task HandlePasteAction()
        {
            try
            {
                string pastedText = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(pastedText))
                {
                    await new Exam_proctor.APIClient.AccessDetectAIModel().SendTextToModel(pastedText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to read clipboard: " + ex.Message);
            }
        }

        public void Dispose()
        {
            _hook?.Dispose();
        }
    }
}
