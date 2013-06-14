using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using System.Runtime.InteropServices;
using System.Windows.Automation;
//using System.Windows.Forms;




/*
// For Windows Mobile, replace user32.dll with coredll.dll
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

// Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

[DllImport("user32.dll", EntryPoint="FindWindow", SetLastError = true)]
static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

// You can also call FindWindow(default(string), lpWindowName) or FindWindow((string)null, lpWindowName)
*/

namespace CryoAutomation
{
/*
[DllImport("user32", SetLastError=true)]
[return: MarshalAs(UnmanagedType.Bool)]
private extern static bool EnumThreadWindows(int threadId, EnumWindowsProc callback, IntPtr lParam);

[DllImport("user32", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

[DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
private extern static int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);


    */

    public class MoveToForeground
    {
        [DllImportAttribute("User32.dll")]
        private static extern int FindWindow(String ClassName, String WindowName);

        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_SHOWWINDOW = 0x0040;
        const int SWP_NOACTIVATE = 0x0010;
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        public static void DoOnProcess(string processName)
        {
            var allProcs = Process.GetProcessesByName(processName);
            if (allProcs.Length > 0)
            {
                Process proc = allProcs[0];
                int hWnd = FindWindow(null, proc.MainWindowTitle.ToString());
                // Change behavior by settings the wFlags params. See http://msdn.microsoft.com/en-us/library/ms633545(VS.85).aspx
                SetWindowPos(new IntPtr(hWnd), 0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
            }
        }
    }

    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        public IntPtr Handle
        {
            get { return _hwnd; }
        }

        private IntPtr _hwnd;
    }


    class WinAPI
    {
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImportAttribute("User32.dll")]
        private static extern int FindWindow(String ClassName, String WindowName);

        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_SHOWWINDOW = 0x0040;
        const int SWP_NOACTIVATE = 0x0010;
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);



        /*
        private static IntPtr FindWindowInThread(int threadId, Func<string, bool> compareTitle)
        {
          IntPtr windowHandle = IntPtr.Zero;
          EnumThreadWindows(threadId, (hWnd, lParam) =>
          {
            StringBuilder text = new StringBuilder(200);
            GetWindowText(hWnd, text, 200);
            if (compareTitle(text.ToString()))
            {
              windowHandle = hWnd;
              return false;
            }
            return true;
          }, IntPtr.Zero);

          return windowHandle;
        }


        public static IntPtr FindWindowInProcess(Process process, Func<string, bool> compareTitle)
        {
            IntPtr windowHandle = IntPtr.Zero;

            foreach (ProcessThread t in process.Threads)
            {
                windowHandle = FindWindowInThread(t.Id, compareTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    break;
                }
            }

          return windowHandle;
        }
        */

        public IntPtr getMainWindowHandleByName(string process_name)
        {
            Process[] processes = Process.GetProcessesByName(process_name);
            foreach (Process p in processes)
            {
                IntPtr pFoundWindow = p.MainWindowHandle;
                // Do something with the handle...
                //
                return pFoundWindow;
            }

            return IntPtr.Zero;
            /*
            foreach (Process p in Process.GetProcesses())
            {
              if (p.MainModule.FileName.ToLower().EndsWith("foo.exe"))
                 FindChildWindowWithText(p); //do work
            }*/
        }

        public IntPtr getSCLWindowHandle()
        {
            return getMainWindowHandleByName("s7sclapx");
        }

        public void test()
        {
            IntPtr wPtr = getSCLWindowHandle();
            if (wPtr != IntPtr.Zero)
                Console.Write("Found\n");
            else
                Console.Write("Not found\n");
            SetForegroundWindow(wPtr);
            SetWindowPos(wPtr, 0, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
            WindowWrapper w = new WindowWrapper(wPtr);
            //System.Windows.Forms.MessageBox.Show(w.Show(), "Test");
            //System.Windows.Forms.
            //ShowWindow
            //System.Windows.Forms.SendKeys.SendWait("^{F4}");
            System.Windows.Forms.SendKeys.SendWait("%{F4}");
        }

        [DllImport("user32.dll", EntryPoint="PostMessage", CharSet=CharSet.Auto)]
        static extern bool PostMessage1(IntPtr hWnd, uint Msg, int wParam, int lParam);

        static void ClickOn(IntPtr hControl)
        {
            uint WM_LBUTTONDOWN = 0x0201;
            uint WM_LBUTTONUP   = 0x0202;
            PostMessage1(hControl, WM_LBUTTONDOWN, 0, 0);
            PostMessage1(hControl, WM_LBUTTONUP,   0, 0);
        }
        
    }
}
