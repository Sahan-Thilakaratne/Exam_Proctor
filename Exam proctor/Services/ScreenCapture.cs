using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_proctor.Services
{
    public static class ScreenCapture
    {
        /// <summary>
        /// Capture the entire virtual desktop (all monitors) as PNG bytes.
        /// </summary>
        public static byte[] CaptureAllScreensToPng()
        {
            Rectangle union = Screen.AllScreens
                                    .Select(s => s.Bounds)
                                    .Aggregate(Rectangle.Union);

            using (var bmp = new Bitmap(union.Width, union.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(union.Location, Point.Empty, union.Size);

                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Capture only the current foreground (active) top-level window as PNG bytes.
        /// Returns null if the window can't be determined.
        /// </summary>
        public static byte[] CaptureActiveWindowToPng()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return null;

            RECT r;
            if (!GetWindowRect(hWnd, out r))
                return null;

            int width = r.Right - r.Left;
            int height = r.Bottom - r.Top;
            if (width <= 0 || height <= 0)
                return null;

            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(new Point(r.Left, r.Top), Point.Empty, new Size(width, height));

                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left; public int Top; public int Right; public int Bottom;
        }
    }
}
