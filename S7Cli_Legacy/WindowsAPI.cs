using System;
using System.Runtime.InteropServices;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace S7_cli
{
    //////////////////////////////////////////////////////////////////////////
    /// class S7CompilerSCL
    /// <summary>
    /// A class for interacting with an opened SCL compiler and accessing
    /// the compilation status.
    /// </summary>
    public class WindowsAPI
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string IpClassName,
                                         string IpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent,
                                           IntPtr hwndChildAfter,
                                           string IpszClass,
                                           string IpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hwnd,
                                       int wMsg,
                                       IntPtr wParam,
                                       IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hwnd,
                                                     ref uint ProcessId);

        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess,
                                                 bool bInheritHandle,
                                                 int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess,
                                                     IntPtr lpBaseAddress,
                                                     ref IntPtr lpBuffer,
                                                     int dwSize,
                                                     ref int lpnumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess,
                                                     IntPtr lpBaseAddress,
                                                     byte[] buffer,
                                                     int dwSize,
                                                     ref int lpnumberOfBytesRead);

        // System constants, more can be found on:
        // https://referencesource.microsoft.com/UIAutomationClientsideProviders/MS/Win32/NativeMethods.cs.html
        //
        public const int WM_CLOSE = 0x10;
        public const int LB_ERR = -1;
        public const int LB_SETCURSEL = 0x0186;
        public const int LB_GETCURSEL = 0x0188;

        public const int LB_GETTEXT = 0x0189;
        public const int LB_GETTEXTLEN = 0x018A;
        public const int LB_GETCOUNT = 0x018B;
        public const int LB_GETITEMDATA = 0x0199;

        public const uint DELETE = 0x00010000;
        public const uint READ_CONTROL = 0x00020000;
        public const uint WRITE_DAC = 0x00040000;
        public const uint WRITE_OWNER = 0x00080000;
        public const uint SYNCHRONIZE = 0x00100000;
        public const uint END = 0xFFF; //if you have Windows XP or Windows Server 2003 you must change this to 0xFFFF
        public const uint PROCESS_ALL_ACCESS = (DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER | SYNCHRONIZE | END);
        //const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        public const int PROCESS_WM_READ = 0x0010;
    }
}
