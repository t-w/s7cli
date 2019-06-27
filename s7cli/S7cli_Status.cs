/************************************************************************
 * S7cli_status.cs - the S7cli_status class                             *
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

namespace S7_cli
{
    /// <summary>
    /// s7cli status class
    /// </summary>
    public static class S7cli_Status
    {
        /// <summary>
        /// Show s7cli execution status
        /// </summary>
        /// <param name="result_code"></param>
        /// <param name="result_info"></param>
        public static void show(int result_code,
                                 string result_info = "")
        {
            Logger.log("");
            Logger.log("Result: " + S7CommandStatus.get_info());
            string detailed_info = S7CommandStatus.get_detailed_info();

            if (detailed_info != "")
                Logger.log("Result info: " + detailed_info);

            if (result_info != "")
                Logger.log("Result info: " + result_info);
        }

        //public static void exit(Result_code result_code)
        public static void exit(int result_code)
        {
            Logger.log_debug("Exiting with status:" + result_code);
            System.Environment.Exit((int)result_code);
        }

        //public static void exit_with_info(Result_code result_code, string result_info = "")
        public static void exit_with_info(int result_code, string result_info = "")
        {
            show(result_code, result_info);
            exit(result_code);
        }
    }
}