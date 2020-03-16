/************************************************************************
 * s7cli.cs - the s7cli main program                                    *
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
using System.Reflection;


namespace S7_cli
{
    /// <summary>
    /// s7cli main program
    /// </summary>
    public class s7cli
    {
        /// <summary>
        /// Main program
        /// </summary>
        /// <param name="args">command-line arguments</param>
        /// <returns>0 on success, error code otherwise</returns>
        public static int Main(string[] args)
        {
            //Logger.setLevel(Logger.level_debug);   // switch on more debugging info
            show_logo();
            Console.Write("\n\n");

            S7CommandStatus.set_status(S7CommandStatus.success);
            if (OptionParser.parse(args) != 0)
                S7CommandStatus.set_status(S7CommandStatus.failure);

            int status = S7CommandStatus.get_status();
            S7cli_Status.show(status);
            return status;

            // TODO: Remove following code; possibly add usage examples, per verb


            //siemensPLCProject project = new siemensPLCProject("D:\\controls\\apps\\sector56\\plc\\mirror56");

            //System.Console.Write("\nsources LogPath: " + sources.LogPath + "\n");
            //S7SWItems src_modules = project.getSourceModules("ARC56_program");
            //System.Console.Write("\nsrouce modules count: " + src_modules.Count + "\n");

            //S7SWItem src_module = project.getSourceModule("ARC56_program", "4_Compilation_OB");
            //System.Console.Write(src_module.NameToString());
            //System.Console.Write(src_module.Name);
            //src_modules.Add("Test1", SimaticLib.S7SWObjType.S7Source ,"D:\\test1.scl");
            //project.addSourceModuleSCL("ARC56_program", "D:\\test1.scl");
        }

        
        /// <summary>
        /// Return program version (as string)
        /// </summary>
        /// <returns>program version (string)</returns>
        static public string get_version()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Revision}";
        }

        /// <summary>
        /// Shows program logo / info
        /// </summary>
        static public void show_logo()
        {
            string logo = @"

                  _|_|_|_|_|            _|  _|
          _|_|_|          _|    _|_|_|  _|
        _|_|            _|    _|        _|  _|
            _|_|      _|      _|        _|  _|
        _|_|_|      _|          _|_|_|  _|  _|   " + get_version() + @"

        Command-line interface for Siemens SIMATIC Step7(tm)
        (C) 2013-2019 CERN, TE-CRG-CE

        Authors: Michal Dudek, Tomasz Wolak
";
            Console.Write(logo);
        }
    }
}
