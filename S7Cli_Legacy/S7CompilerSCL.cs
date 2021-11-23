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
using System.Diagnostics;
using System.Collections.Generic;


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
            
            handle = WindowsAPI.FindWindow( "AfxMDIFrame42", null );
            if (handle.Equals(null))
            {
                Logger.log_error("The SCL compiler window not found.");
                return;
            }

            WindowsAPI.GetWindowThreadProcessId( handle, ref pid );

            //hProc = OpenProcess(PROCESS_ALL_ACCESS, false, (int) pid);
            hProc = WindowsAPI.OpenProcess( WindowsAPI.PROCESS_WM_READ, false, (int) pid );
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

            IntPtr listboxControlBar = WindowsAPI.FindWindowEx( handle, nullptr,
                                                     "AfxControlBar42", "SCL: Errors and Warnings" );
	        //checkHandle(listboxControlBar, "AfxControlBar42 / SCL: Errors and Warnings");

            IntPtr listboxWnd42 = WindowsAPI.FindWindowEx(listboxControlBar, nullptr,
                                                "AfxWnd42", "SCL: Errors and Warnings" );
	        //checkHandle(listboxWnd42, "AfxWnd42 / SCL: Errors and Warnings");

            IntPtr listboxAfx_1 = WindowsAPI.FindWindowEx(listboxWnd42, nullptr, "Afx:400000:8", "");
	        //checkHandle(listboxAfx_1, "Afx:400000:8");

            IntPtr listbox = WindowsAPI.FindWindowEx(listboxAfx_1, nullptr, "ListBox", "");
	        //checkHandle(listbox, "ListBox");

	        return listbox;
        }


        /// <summary>
        /// Reads the compilation status buffer from the SCL compiler process
        /// </summary>
        void readSclStatusBuffer()
        {
            IntPtr listbox = getSclStatusListBox();

            int itemCount = WindowsAPI.SendMessage(listbox, WindowsAPI.LB_GETCOUNT, nullptr, nullptr);

            byte[] bufferLine = new byte[128];

	        for (int i = 0; i < itemCount; i++)
	        {
                int txtLength = WindowsAPI.SendMessage(listbox, WindowsAPI.LB_GETTEXTLEN, (IntPtr)i, nullptr);

                // clean-up line buffer
                for (int j = 0; j < bufferLine.Length; j++)
                    bufferLine[j] = 0;

                int selected = WindowsAPI.SendMessage(listbox, WindowsAPI.LB_SETCURSEL, (IntPtr)i, nullptr);

                selected = WindowsAPI.SendMessage(listbox, WindowsAPI.LB_GETCURSEL, nullptr, nullptr);
                int itemPtr = WindowsAPI.SendMessage( listbox,
                                                      WindowsAPI.LB_GETITEMDATA,
                                                      (IntPtr)selected,
                                                      nullptr );

                if ( itemPtr != WindowsAPI.LB_ERR )
		        {
			        int bytes_read = 0;
                    IntPtr ptr = new IntPtr(0);

                    bool result = WindowsAPI.ReadProcessMemory(
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

                            result = WindowsAPI.ReadProcessMemory(
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
            WindowsAPI.SendMessage(handle, WindowsAPI.WM_CLOSE, new IntPtr(0), new IntPtr(0));

            // wait until the SCL compiler process disappears
            while ( Array.Exists< Process >( Process.GetProcesses(),
                                             s => s.Id == pid ) )
            {
                System.Threading.Thread.Sleep( 1000 );
            }

            // unset SCL compiler "handles"
            handle = new IntPtr(0);
            pid    = 0;
            hProc  = new IntPtr(0);
        }
    }
}