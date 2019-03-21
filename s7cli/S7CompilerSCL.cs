/************************************************************************
 * S7CompilerSCL.cs - S7 Compiler SCL class                             *
 *                                                                      *
 * Copyright (C) 2013-2019 CERN                                         *
 *                                                                      *
 * This program is free software: you can redistribute it and/or modify *
 * it under the terms of the GNU General Public License as published by *
 * the Free Software Foundation, either version 3 of the License, or    *
 * (at your option) any later version.                                  *
 *                                                                      *
 * This program is distributed in the hope that it will be useful,      *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of       *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the        *
 * GNU General Public License for more details.                         *
 *                                                                      *
 * You should have received a copy of the GNU General Public License    *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.*
 ************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
//using System.Windows.Automation;
using System.Collections.Generic;

using SimaticLib;
using S7HCOM_XLib;

namespace S7_cli
{
    //////////////////////////////////////////////////////////////////////////
    /// class S7CompilerSCL
    /// <summary>
    /// A class for interacting with an opened SCL compiler and accessing
    /// the compilation status.
    /// </summary>
    public class S7CompilerSCL
    {
        [ DllImport( "user32.dll", SetLastError = true ) ]
        static extern IntPtr FindWindow( string IpClassName,
                                         string IpWindowName );

        [ DllImport ( "user32.dll", SetLastError = true ) ]
        static extern IntPtr FindWindowEx( IntPtr hwndParent,
                                           IntPtr hwndChildAfter,
                                           string IpszClass,
                                           string IpszWindow );

        [ DllImport( "user32.dll", SetLastError = true ) ]
        static extern int SendMessage( IntPtr hwnd,
                                       int    wMsg,
                                       IntPtr wParam,
                                       IntPtr lParam );

        [ DllImport( "user32.dll", SetLastError = true ) ]
        static extern uint GetWindowThreadProcessId( IntPtr   hwnd,
                                                     ref uint ProcessId );
        
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


        [ DllImport( "kernel32.dll", SetLastError = true ) ]
        public static extern IntPtr OpenProcess( uint processAccess,
                                                 bool bInheritHandle,
                                                 int  processId );

        [ DllImport( "kernel32.dll", SetLastError = true ) ]
        public static extern bool ReadProcessMemory( IntPtr     hProcess,
                                                     IntPtr     lpBaseAddress,
                                                     ref IntPtr lpBuffer,
                                                     int        dwSize,
                                                     ref int    lpnumberOfBytesRead );

        [ DllImport( "kernel32.dll", SetLastError = true ) ]
        public static extern bool ReadProcessMemory( IntPtr  hProcess,
                                                     IntPtr  lpBaseAddress,
                                                     byte [] buffer,
                                                     int     dwSize,
                                                     ref int lpnumberOfBytesRead );

        // System constants, more can be found on:
        // https://referencesource.microsoft.com/UIAutomationClientsideProviders/MS/Win32/NativeMethods.cs.html
        //
        const int WM_CLOSE       = 0x10;
        const int LB_ERR         = -1;
        const int LB_SETCURSEL   = 0x0186;
        const int LB_GETCURSEL   = 0x0188;

        const int LB_GETTEXT     = 0x0189;
        const int LB_GETTEXTLEN  = 0x018A;
        const int LB_GETCOUNT    = 0x018B;
        const int LB_GETITEMDATA = 0x0199;

        const uint DELETE       = 0x00010000;
        const uint READ_CONTROL = 0x00020000;
        const uint WRITE_DAC    = 0x00040000;
        const uint WRITE_OWNER  = 0x00080000;
        const uint SYNCHRONIZE  = 0x00100000;
        const uint END = 0xFFF; //if you have Windows XP or Windows Server 2003 you must change this to 0xFFFF
        const uint PROCESS_ALL_ACCESS = (DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER | SYNCHRONIZE | END);
        //const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        const int PROCESS_WM_READ = 0x0010;

        // the SCL compiler system data
        IntPtr handle,            // window handle
               nullptr;           // null pointer (required in many system calls)
        uint   pid;               // process ID
        IntPtr hProc;             // process handle

        // compiler status buffer
        List<string> statusBuffer;


        /// <summary>
        /// Constructor
        /// </summary>
        ///
        public S7CompilerSCL()
        {
            nullptr = new IntPtr(0);
            
            handle = FindWindow( "AfxMDIFrame42", null );
            if (handle.Equals(null))
            {
                Logger.log_error("The SCL compiler window not found.");
                return;
            }

            GetWindowThreadProcessId(handle, ref pid);

            //hProc = OpenProcess(PROCESS_ALL_ACCESS, false, (int) pid);
            hProc = OpenProcess(PROCESS_WM_READ, false, (int)pid);
            if ( hProc == null )
            {
                Logger.log_error( "OpenProcess() accessing the SCL compiler failed.");
            }

            statusBuffer = new List<string>();
        }


        /// <summary>
        /// Find the listbox (with compilation status info) in the SCL compiler window
        /// </summary>
        IntPtr getSclStatusListBox()
        {
            if (handle.Equals( null ) )
                return handle;

            IntPtr listboxControlBar = FindWindowEx( handle, nullptr,
                                                     "AfxControlBar42", "SCL: Errors and Warnings" );
	        //checkHandle(listboxControlBar, "AfxControlBar42 / SCL: Errors and Warnings");

            IntPtr listboxWnd42 = FindWindowEx( listboxControlBar, nullptr,
                                                "AfxWnd42", "SCL: Errors and Warnings" );
	        //checkHandle(listboxWnd42, "AfxWnd42 / SCL: Errors and Warnings");

            IntPtr listboxAfx_1 = FindWindowEx( listboxWnd42, nullptr, "Afx:400000:8", "" );
	        //checkHandle(listboxAfx_1, "Afx:400000:8");

            IntPtr listbox = FindWindowEx( listboxAfx_1, nullptr, "ListBox", "" );
	        //checkHandle(listbox, "ListBox");

	        return listbox;
        }


        /// <summary>
        /// Reads the compilation status buffer from the SCL compiler process
        /// </summary>
        void readSclStatusBuffer()
        {
            IntPtr listbox = getSclStatusListBox();

	        int itemCount = SendMessage(listbox, LB_GETCOUNT, nullptr, nullptr);

            byte[] bufferLine = new byte[128];

	        for (int i = 0; i < itemCount; i++)
	        {
		        int txtLength = SendMessage(listbox, LB_GETTEXTLEN, (IntPtr) i, nullptr);

                // clean-up line buffer
                for (int j = 0; j < bufferLine.Length; j++)
                    bufferLine[j] = 0;

		        int selected = SendMessage(listbox, LB_SETCURSEL, (IntPtr) i, nullptr );

		        selected = SendMessage(listbox, LB_GETCURSEL, nullptr, nullptr );
                int itemPtr = SendMessage(listbox, LB_GETITEMDATA, (IntPtr) selected, nullptr);

		        if ( itemPtr != LB_ERR )
		        {
			        int bytes_read = 0;
                    IntPtr ptr = new IntPtr(0);

                    bool result = ReadProcessMemory(
                        (IntPtr) hProc,
                        (IntPtr) ( itemPtr + 4 ),
                        ref ptr,
				        4, 
                        ref bytes_read );

                    unsafe
                    {
                        fixed (byte* p = bufferLine)
                        {
                            IntPtr ptr2 = (IntPtr) p;

                            result = ReadProcessMemory(
                                (IntPtr) hProc,
                                ptr,
                                bufferLine,
                                bufferLine.Length,
                                ref bytes_read );
                        }
                    }

                    int endStrIdx = 0;
                    for (int j = 0 ; j< bufferLine.Length ; j++)
                        if ( bufferLine[j] == 0 )
                        {
                            endStrIdx = j;
                            break;
                        }

                    string line = new string(
                        System.Text.Encoding.ASCII.GetString( bufferLine,0, endStrIdx ).ToCharArray() );
                    statusBuffer.Add( line );
		        }
		        else
		        {
			        Logger.log_error ( "Error getting: " + i + "\n");
		        }
	        }
        }


        /// <summary>
        /// Returns the summary line of compilation from the status buffer
        /// </summary>
        public string getSclStatusBuffer()
        {
            if ( statusBuffer.Count < 1 )
                readSclStatusBuffer();
            
            string buffer = "";
            for (int i = 0; i < statusBuffer.Count; i++)
                buffer += statusBuffer[i] + "\n";

            return buffer;
        }


        /// <summary>
        /// Returns the summary line of compilation from the status buffer
        /// </summary>
        public string getSclStatusLine()
        {
            if (statusBuffer.Count < 1)
                readSclStatusBuffer();
            return statusBuffer[ statusBuffer.Count - 1 ];
        }


        /// <summary>
        /// Returns the number of errors (from the summary in the status buffer)
        /// </summary>
        public int getErrorCount()
        {
            string [] statusLine = getSclStatusLine().Split(' ');
            return Int32.Parse(statusLine[1]);
        }


        /// <summary>
        /// Returns the number of warnings (from the summary in the status buffer)
        /// </summary>
        public int getWarningCount()
        {
            string[] statusLine = getSclStatusLine().Split(' ');
            return Int32.Parse(statusLine[3]);
        }


        /// <summary>
        /// Closes the SCL compiler application / window
        /// </summary>
        public void closeSclWindow()
        {
            SendMessage(handle, WM_CLOSE, new IntPtr(0), new IntPtr(0) );
        }
    }
}