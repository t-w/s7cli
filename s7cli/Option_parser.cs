/************************************************************************
 * Option_parser.cs - an option parser for s7cli                        *
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7_cli
{

    public class Option_parser
    {
        public static readonly string[] commands = { 
            "createProject",
            "createLib",
            "listProjects",
            "listPrograms",
            "listContainers",
            "listStations",
            "importConfig",
            "exportConfig",
            "importSymbols",
            "exportSymbols",
            "listSources",
            "listBlocks",
            "importLibSources",
            "importLibBlocks",
            "importSources",
            "importSourcesDir",
            "compileSources",
            "exportSources",
            "exportAllSources",
            "exportProgramStructure",
            "compileStation",
            "compileAllStations",
            "downloadSystemData",
            "downloadBlocks",
            "download",
            "startCPU",
            "stopCPU"
        };

        Dictionary<string, string> command_help;
        Dictionary<string, string[]> options;
        Dictionary<string, string[]> options_valid;
        Dictionary<string, string[]> options_required;

        // program actions
        public string programAction = "";

        Dictionary<string, string> options_parsed = new Dictionary<string, string>();
        bool options_ok = false;

        public Option_parser(string[] args)
        {
            options = new Dictionary<string, string[]>()
                { // { option,                { short option, help information } }
                    { "--projname",            new string[] { "", "project name" }},
                    { "--projdir",             new string[] { "", "project directory" }},
            
                    { "--project",             new string[] { "-p", "project name or path" }},
                    { "--program",             new string[] { "", "program name in project" }},

                    { "--config",              new string[] { "-c", "station (hardware) configuration file" }},
                    { "--station",              new string[] { "", "station name" }},
                    { "--srcdir",              new string[] { "",   "directory with source code files" }},
                    { "--library",             new string[] { "-l", "library name (do 'listProjects' if not sure)" }},
                    { "--libprg",              new string[] { "",   "program in library project" }},
                    { "--libdir",              new string[] { "", "library directory" }},
                    { "--libname",             new string[] { "", "library name" }},
                    { "--dest-proj-prog-name", new string[] { "",   "name of the destination project program" }}, // -> to remove (?) -> use program option

                    { "--symbols",             new string[] { "-s", "path to file with symbols" }},
                    { "--sources",             new string[] { "",   "list of files with source code (CSV)" }},
                    { "--force",               new string[] { "",   "force overwrite (replace) existing sources in project (y/n)" }},
                    { "--conflictok",          new string[] { "",   "do not treat conflicts as errors (y/n)" }},

                    { "--output",              new string[] { "-o", "output file" }},
                    { "--outputdir",           new string[] { "",   "output directory" }},

                    { "--help",                new string[] { "-h", "show help for command" }},
                    { "--debug",               new string[] { "-d", "debug level (0-3)" }}
                };

            command_help = new Dictionary<string, string>()
                { 
                    { "createProject",       "Create new, empty project in specified location" },
                    { "createLib",           "Create a new, empty library in specified location"},
                    { "listProjects",        "List available Simatic projects" },
                    { "listPrograms",        "List available programs in Simatic project/library" },
                    { "listStations",        "List available stations in Simatic project/library" },
                    { "listContainers",      "List available containers in Simatic project/library" },
                    { "importConfig",        "Import station configuration from a file" },
                    { "exportConfig",        "Export station configuration to a file" },
                    { "importSymbols",       "Import program symbols from a file" },
                    { "exportSymbols",       "Export program symbols to a file" },
                    { "listSources",         "List of source code modules in a program" },
                    { "listBlocks",          "List of blocks in specified program"},
                    { "importLibSources",    "Import all sources from a library to a project" },
                    { "importLibBlocks",     "Import all blocks from a library to a project" },
                    { "importSources",       "Import specified source code files" },
                    { "importSourcesDir",    "Import all source code files from specified directory (only valid ones: .SCL, .AWL, .INP, .GR7)" },
                    { "compileSources",      "Compile specified source code module(s)" },
                    { "exportSources",       "Export specified source code module(s)" },
                    { "exportAllSources",    "Export all source code module(s) from a program" },
                    { "exportProgramStructure", "Exports the block calling structure into a DIF-File (experimental, not tested!!!)" },
                    { "compileStation",      "Compiles station hardware and connections (experimental, don't use it!!!)" },
                    { "compileAllStations",  "Compiles all stations' hardware and connections " },
                    { "downloadBlocks",      "Downloads blocks (omits \"System data\") to the PLC" },
                    { "downloadSystemData",  "Downloads \"System data\" to the PLC" },
                    { "download",            "Downloads all blocks (including system data) for a given program to the PLC" },
                    { "startCPU",            "Starts (new start) PLC" },
                    { "stopCPU",             "Stops PLC" }
                };

            options_valid = new Dictionary<string, string[]>()
                { 
                    { "createProject",          new string[] { "--debug", "--projname", "--projdir" }},
                    { "createLib",              new string[] { "--debug", "--libdir", "--libname"}},
                    { "listProjects",           new string[] { "--debug", }},
                    { "listPrograms",           new string[] { "--debug", "--project" }},
                    { "listStations",           new string[] { "--debug", "--project" }},
                    { "listContainers",         new string[] { "--debug", "--project" }},
                    { "importConfig",           new string[] { "--debug", "--project", "--config" }},
                    { "exportConfig",           new string[] { "--debug", "--project", "--config", "--station" }},
                    { "importSymbols",          new string[] { "--debug", "--project", "--program", "--symbols", "--conflictok" }},
                    { "exportSymbols",          new string[] { "--debug", "--project", "--program", "--output", "--force" }},
                    { "listSources",            new string[] { "--debug", "--project", "--program" }},
                    { "listBlocks",             new string[] { "--debug", "--project", "--program" }},
                    { "importLibSources",       new string[] { "--debug", "--project", "--program", "--library", "--libprg", "--force" }},
                    { "importLibBlocks",        new string[] { "--debug", "--project", "--program", "--library", "--libprg", "--force" }}, 
                    { "importSources",          new string[] { "--debug", "--project", "--program", "--sources", "--force" }},
                    { "importSourcesDir",       new string[] { "--debug", "--project", "--program", "--srcdir",  "--force" }},
                    { "compileSources",         new string[] { "--debug", "--project", "--program", "--sources" }},
                    { "exportSources",          new string[] { "--debug", "--project", "--program", "--sources", "--outputdir" }},
                    { "exportAllSources",       new string[] { "--debug", "--project", "--program", "--outputdir" }},
                    { "exportProgramStructure", new string[] { "--debug", "--project", "--program", "--output" }},
                    { "compileStation",         new string[] { "--debug", "--project", "--station" }},
                    { "compileAllStations",     new string[] { "--debug", "--project" }},
                    { "downloadBlocks",         new string[] { "--debug", "--project", "--program", "--force" }},
                    { "downloadSystemData",     new string[] { "--debug", "--project", "--program", "--force" }},
                    { "download",               new string[] { "--debug", "--project", "--program" }},
                    { "startCPU",               new string[] { "--debug", "--project", "--program" }},
                    { "stopCPU",                new string[] { "--debug", "--project", "--program" }}
                };

            options_required = new Dictionary<string, string[]>()
                { 
                    { "createProject",       new string[] { "--projname", "--projdir" }},
                    { "createLib",           new string[] { "--libdir", "--libname"}},
                    { "listProjects",        new string[] { }},
                    { "listPrograms",        new string[] { "--project" }},
                    { "listStations",        new string[] { "--project" }},
                    { "listContainers",      new string[] { "--project" }},
                    { "importConfig",        new string[] { "--project", "--config" }},
                    { "exportConfig",        new string[] { "--project", "--config", "--station" }},
                    { "importSymbols",       new string[] { "--project", "--program", "--symbols" }},
                    { "exportSymbols",       new string[] { "--project", "--program", "--output" }},
                    { "listSources",         new string[] { "--project", "--program" }},
                    { "listBlocks",          new string[] { "--project", "--program" }},
                    { "importLibSources",    new string[] { "--project", "--program", "--library", "--libprg" }},
                    { "importLibBlocks",     new string[] { "--project", "--program", "--library", "--libprg" }}, 
                    { "importSources",       new string[] { "--project", "--program", "--sources" }},
                    { "importSourcesDir",    new string[] { "--project", "--program", "--srcdir" }},
                    { "compileSources",      new string[] { "--project", "--program", "--sources" }},
                    { "exportSources",          new string[] { "--project", "--program", "--sources", "--outputdir" }},
                    { "exportAllSources",       new string[] { "--project", "--program", "--outputdir" }},
                    { "exportProgramStructure", new string[] { "--project", "--program", "--output" }},
                    { "compileStation",         new string[] { "--project", "--station" }},
                    { "compileAllStations",     new string[] { "--project" }},
                    { "downloadBlocks",         new string[] { "--project", "--program" }},
                    { "downloadSystemData",     new string[] { "--project", "--program" }},
                    { "download",               new string[] { "--project", "--program" }},
                    { "startCPU",               new string[] { "--project", "--program" }},
                    { "stopCPU",                new string[] { "--project", "--program" }}
                };

            if (this.parseOptions(args))
                this.validateOptions();
        }

        /// <summary>
        /// Parse command line options, building options_command dictionary,
        /// return info if parsing correct
        /// </summary>
        /// <param name="args"></param>
        /// <returns>
        /// true  if options are parsed correctly,
        /// false otherwies
        /// </returns>
        private bool parseOptions(string [] args){

            if (args == null) return false;

            // nr of passed arguments to the program
            int nrOfArguments = args.Length;
            Logger.log_debug("\nparseOptions(): Number of arguments: " + nrOfArguments);

            // C# does not count program name as argument
            if (nrOfArguments != 0)  {
                Logger.log_debug("\nCommand: " + args[0] + "\n");
                foreach (string command in commands)
                    if (args[0].CompareTo(command) == 0)
                        this.programAction = command;
                
                if (this.programAction.CompareTo("") == 0)   // unknown command (?)
                    return false;
                
                for (int currentArgument = 1; currentArgument < nrOfArguments; currentArgument++)  {
                    Logger.log_debug("\nCurrent arg: " + args[currentArgument] + "\n");

                    // if help option set - do not continue checking other args (not needed)
                    if (args[currentArgument] == "--help" || args[currentArgument] == "-h") {
                        this.options_parsed.Add("--help", "");
                        return true;
                    }

                    /*
                    if (args[currentArgument] == "--debug" || args[currentArgument] == "-d") {
                        
                        int debug_level = -1;  // not set
                        if ((nrOfArguments > (currentArgument + 1)) &&
                             (int.TryParse(args[currentArgument + 1], out debug_level)))  {
                            this.options_parsed.Add("--debug", args[currentArgument + 1]);
                            return true;
                        }  else  {
                            Logger.log("\n\n*** Error: option --debug/-d needs an argument.\n");
                            return false;
                        }
                    }*/

                    foreach (KeyValuePair<string, string[]> option in options)  {
                        if (args[currentArgument].CompareTo(option.Key) == 0 ||     // long option
                             args[currentArgument].CompareTo(option.Value[0]) == 0)  // short option
                        {

                            if ( nrOfArguments > (currentArgument + 1) &&   // all options require an arg.
                                 args[currentArgument + 1] != "" )          // ... and an non-empty one
                            {
                                //this.projectConfigPath = args[currentArgument + 1];
                                this.options_parsed.Add(option.Key, args[currentArgument + 1]);
                                Logger.log_debug("Adding parsed arg: " + option.Key);
                            } else {
                                //Console.Write("Error: " + option.Value[0] + " is not correctly specified.\n");
                                Logger.log("\n\n*** Error: option " + option.Key + " needs an argument.\n");
                                //Environment.Exit(1);
                                return false;
                            }
                        }
                    }
                    Logger.log_debug(options_parsed.Count + " " + options_parsed.Keys);

                }
                this.options_ok = true;
                return this.options_ok;
            }
            return false;
        }

        public bool optionSet(string option)
        { /*
            try {
                string tmp = this.options_parsed[option];
            } catch (SystemException exc) {
                Console.Write(exc.ToString());
                return false;
            }
            return true; */
            if (options_parsed.ContainsKey(option))
                return true;
            else
                return false;
        }

        private void validateOptions(){
            this.options_ok = true;
            foreach (string opt in options_required[programAction])
                if (!this.optionSet(opt)) {
                    //Logger.log_debug("\n options not ok: " + opt);
                    this.options_ok = false;
                    return;
                }
        }

        public bool optionsOK()   {
            return this.options_ok;
        }

        public string getCommand()  {
            return this.programAction;
        }

        public string getOption(string option)  {
            Logger.log_debug("getOption('" + option + "')");
            if ( optionSet( option ) )
                return this.options_parsed[option];
            return "";
        }

        public bool needHelp(){
            if (this.optionSet("--help"))
                return true;
            else
                return false;
        }

        public string getOptionHelp(string option)  {
            return this.options[option][1];
        }

        public string getCommandHelp(string command)  {
            return command_help[command];
        }

        public string getCommandOptionsHelp(string command)
        {
            string helpInfo = "";
            foreach (string opt in options_valid[command])
                helpInfo = helpInfo + "\n\n    " + opt + (options[opt][0] != "" ? ", " + options[opt][0] : "" ) + 
                    "\n        " + options[opt][1];
            return helpInfo;
        }
    }
}
