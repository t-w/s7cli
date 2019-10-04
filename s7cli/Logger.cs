/************************************************************************
 * Logger.cs - the s7cli application logger                             *
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
//using System.Text;

namespace S7_cli
{
    /// <summary>
    /// Static class - common logger with several debug levels.
    /// </summary>
    public static class Logger
    {
        public const int min_debug_level = 0;
        public const int max_debug_level = 3;

        public const int level_none = 0;
        public const int level_error = 1;
        public const int level_warning = 2;
        public const int level_debug = 3;

        static int level = 1;       // default is error level

        public static void setLevel(int log_level)
        {
            level = log_level;
        }

        public static int getLevel()
        {
            return level;
        }

        public static void log(string info)
        {
            Console.Write(info + "\n");
        }

        public static void log_debug(string info)
        {
            // only console output
            if (level >= level_debug)
                log("Debug: " + info);
        }

        public static void log_warning(string info)
        {
            // only console output
            if (level >= level_warning)
                log("Warning: " + info);
        }

        public static void log_error(string info)
        {
            // only console output
            if (level >= level_error)
                //log ("Error: " + info + "\n");
                log("Error: " + info);
        }

        public static void log_result(string info)
        {
            log("Result: " + info);
        }
    }
}