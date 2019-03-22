/************************************************************************
 * S7CommandStatus.cs - a class with cli command execution status       *
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

namespace S7_cli
{
    /// <summary>
    /// Class to manage S7 command line execution status
    /// </summary>
    public static class S7CommandStatus
    {
        //public enum Result_code { success, failure, unknown };
        public const int success = 0;
        public const int failure = 1;
        public const int unknown = 2;

        static int status = -1;
        static string[] status_info = 
            {
                "Success",
                "Failure",
                "Unknown"
            };

        static string detailed_info = "";

        /// <summary>
        /// Returns execution status code.
        /// </summary>
        /// <returns>Execution status code</returns>
        public static int get_status()
        {
            return status;
        }

        /// <summary>
        /// Check if the command status is set.
        /// </summary>
        /// <returns>Command status, true on success, false on failure.</returns>
        public static bool status_set()
        {
            return (status > -1);
        }

        /// <summary>
        /// Set execution status
        /// It should be set only once - when a result (usually success or failure) is reached
        /// </summary>
        public static void set_status(int new_status)
        {
            if (new_status < -1 || new_status > 2)
                throw new System.Exception("S7Status::set_status() - illegal value " + new_status + "!");
            status = new_status;
        }

        /// <summary>
        /// Returns text information about execution status.
        /// </summary>
        /// <returns>Execution status info.</returns>
        public static string get_info()
        {
            if (status_set())
                return status_info[status];
            else
                return "Status unset!";
        }

        /// <summary>
        /// Sets detailed info about status to given text (string).
        /// </summary>
        /// <param name="info">Detailed status info (string)</param>
        public static void set_detailed_info(string info)
        {
            detailed_info = info;
        }

        /// <summary>
        /// Return detailed information about status.
        /// </summary>
        /// <returns>Detailed status information (string)</returns>
        public static string get_detailed_info()
        {
            return detailed_info;
        }
    }
}