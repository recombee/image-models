using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepRecommender
{
    static class Presenter
    {
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        public static void DisplayItems(IEnumerable<string> items, string directory, int rows = 3, int columns = 4)
        {
            var existingItems = Directory.GetFiles(directory);
            var itemFileMap = existingItems.ToDictionary(Path.GetFileNameWithoutExtension, x => x);

            var N = 0;
            var windows = new List<IntPtr>();
            var windowHandlers = new List<Action>();

            var sreenW = Screen.PrimaryScreen.Bounds.Width;
            var sreenH = Screen.PrimaryScreen.Bounds.Height;

            foreach(var item in items.Take(rows * columns))
            {
                if(itemFileMap.TryGetValue(item, out var itemPath) == false)
                {
                    Console.WriteLine($"Not found: {item}");
                    continue;
                }

                try
                {
                    Process.Start(itemPath);

                    var i = N;
                    windowHandlers.Add(() =>
                    {
                        var title = Path.GetFileName(itemPath) + " - Windows Photo Viewer";

                        var X = (sreenW / columns) * (i % columns);
                        var Y = (sreenH / rows) * (i / columns);

                        IntPtr windowHandle;
                        while(true)
                        {
                            windowHandle = FindWindowByCaption(IntPtr.Zero, title);
                            if(windowHandle != IntPtr.Zero)
                            {
                                break;
                            }

                            //Retry till the windows is spawned
                            Thread.Sleep(TimeSpan.FromMilliseconds(50));
                        }

                        SetWindowPos(windowHandle, IntPtr.Zero, X, Y, 100, 100, 0);
                        windows.Add(windowHandle);
                    });
                }
                catch(Exception)
                {
                    Console.WriteLine($"Failed {item}");
                }

                N++;
            }

            foreach(var windowHandler in windowHandlers)
            {
                windowHandler.Invoke();
            }

            Console.WriteLine("Press any key to close the previews...");
            Console.ReadLine();

            foreach(var window in windows)
            {
                const UInt32 WM_CLOSE = 0x0010;
                SendMessage(window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }

}
