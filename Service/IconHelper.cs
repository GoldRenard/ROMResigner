// ======================================================================
// ROM RESIGNER
// Copyright (C) 2013 Ilya Egorov (goldrenard@gmail.com)

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
// ======================================================================

using System;
using System.Text;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using System.IO;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Threading;

namespace ROMResigner
{
    /// <summary>
    /// Класс работы с иконками утилиты и APK
    /// </summary>
    public static class IconHelper
    {
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
                   int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd, uint msg,
                   IntPtr wParam, IntPtr lParam);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_DLGMODALFRAME = 0x0001;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOZORDER = 0x0004;
        const int SWP_FRAMECHANGED = 0x0020;
        const uint WM_SETICON = 0x0080;

        /// <summary>
        /// Убирает икорку окна
        /// </summary>
        /// <param name="window">Класс окна</param>
        public static void RemoveIcon(Window window)
        {
            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            // Change the extended window style to not show a window icon
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);

            // Update the window's non-client area to reflect the changes
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE |
                  SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        /// <summary>
        /// Получение иконки APK-приложения
        /// </summary>
        /// <param name="filename">Путь до приложения</param>
        /// <returns>Икорка</returns>
        public static ImageSource GetApkIcon(string filename)
        {
            //Нужно создавать новые экземпляры, ибо BeginOutputReadLine() блухает
            Environment.aaptProc.StartInfo.Arguments = "dump badging \"" + filename + "\"";
            ProcessStartInfo pStartInfo = Environment.aaptProc.StartInfo;
            Environment.aaptProc = new Process();
            Environment.aaptProc.StartInfo = pStartInfo;

            StringBuilder output = new StringBuilder();
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            {
                Environment.aaptProc.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                        outputWaitHandle.Set();
                    else
                        output.AppendLine(e.Data);
                };

                Environment.aaptProc.Start();
                Environment.aaptProc.BeginOutputReadLine();
                if (Environment.aaptProc.WaitForExit(1000) && outputWaitHandle.WaitOne(1000))
                {
                    Regex regex = new Regex(@"^application:\slabel='(.*)'\sicon='(.*)'$");
                    string IconPath = string.Empty;
                    foreach (string line in output.ToString().Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (regex.IsMatch(line))
                            IconPath = regex.Match(line).Groups[2].Value;
                    }
                    if (!string.IsNullOrEmpty(IconPath))
                    {
                        ZipFile zip = new ZipFile(filename);
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = zip.GetInputStream(zip.FindEntry(IconPath, true));
                        bitmap.EndInit();
                        bitmap.Freeze();
                        zip.Close();
                        return bitmap;
                    }
                }
            }
            return null;
        }
    }
}
