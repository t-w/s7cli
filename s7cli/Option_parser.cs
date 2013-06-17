using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7_cli
{

    class Option_parser
    {
        public static readonly string[] commands = { 
            "createProject", "listProjects", "listPrograms", "importConfig", "importSymbols", 
            "listSources", "importLibSources", "importLibBlocks", "importSources", 
            "importSourcesDir", "compileSources"
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

                    { "--config",              new string[] { "-c", "project (hardware) configuration file" }},
                    { "--srcdir",              new string[] { "",   "directory with source code files" }},
                    { "--library",             new string[] { "-l", "library name (do 'listProjects' if not sure)" }},
                    { "--libprg",              new string[] { "",   "program in library project" }},
                    { "--dest-proj-prog-name", new string[] { "",   "name of the destination project program" }}, // -> to remove (?) -> use program option

                    { "--symbols",             new string[] { "-s", "path to file with symbols" }},
                    { "--sources",             new string[] { "",   "list of files with source code (CSV)" }},
                    { "--force",               new string[] { "",   "force overwrite (replace) existing sources in project (y/n)" }},

                    { "--help",                new string[] { "-h", "show help for command" }}
                };

            command_help = new Dictionary<string, string>()
                { 
                    { "createProject",       "Create new, empty project in specified location" },
                    { "listProjects",        "List available Simatic projects" },
                    { "listPrograms",        "List available programs in Simatic project/library" },
                    { "importConfig",        "Import project configuration from a file" },
                    { "importSymbols",       "Import program symbols from a file" },
                    { "listSources",         "List of source code modules in specified program" },
                    { "importLibSources",    "Import all sources from a library to project" },
                    { "importLibBlocks",     "Import all blocks from a library to project" },
                    { "importSources",       "Import specified source code files" },
                    { "importSourcesDir",    "Import all source code files from specified directory (only valid ones: .SCL, .AWL, .INP)" },
                    { "compileSources",      "Compile specified source code module(s)" }
                };

            options_valid = new Dictionary<string, string[]>()
                { 
                    { "createProject",       new string[] { "--projname", "--projdir" }},
                    { "listProjects",        new string[] { }},
                    { "listPrograms",        new string[] { "--project" }},
                    { "importConfig",        new string[] { "--project", "--config" }},
                    { "importSymbols",       new string[] { "--project", "--program", "--symbols" }},
                    { "listSources",         new string[] { "--project", "--program" }},
                    { "importLibSources",    new string[] { "--project", "--program", "--library", "--libprg" }},
                    { "importLibBlocks",     new string[] { "--project", "--program", "--library", "--libprg" }}, 
                    { "importSources",       new string[] { "--project", "--program", "--sources", "--force" }},
                    { "importSourcesDir",    new string[] { "--project", "--program", "--srcdir",  "--force" }},
                    { "compileSources",      new string[] { "--project", "--program", "--sources" }},
                };

            options_required = new Dictionary<string, string[]>()
                { 
                    { "createProject",       new string[] { "--projname", "--projdir" }},
                    { "listProjects",        new string[] { }},
                    { "listPrograms",        new string[] { "--project" }},
                    { "importConfig",        new string[] { "--project", "--config" }},
                    { "importSymbols",       new string[] { "--project", "--program", "--symbols" }},
                    { "listSources",         new string[] { "--project", "--program" }},
                    { "importLibSources",    new string[] { "--project", "--program", "--library", "--libprg" }},
                    { "importLibBlocks",     new string[] { "--project", "--program", "--library", "--libprg" }}, 
                    { "importSources",       new string[] { "--project", "--program", "--sources" }},
                    { "importSourcesDir",    new string[] { "--project", "--program", "--srcdir" }},
                    { "compileSources",      new string[] { "--project", "--program", "--sources" }},
                };

            if (this.parseOptions(args))
                this.validateOptions();
        }

        private bool parseOptions(string [] args){
            
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

                    foreach (KeyValuePair<string, string[]> option in options)  {
                        if (args[currentArgument].CompareTo(option.Key) == 0 ||     // long option
                             args[currentArgument].CompareTo(option.Value[0]) == 0)  // short option
                        {
                            if (nrOfArguments > (currentArgument + 1)) {
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
            return this.options_parsed[option];
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
