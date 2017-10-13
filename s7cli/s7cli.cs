using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Runtime.InteropServices;
using System.Reflection;

using SimaticLib;

namespace S7_cli
{
    static class Logger
    {
        public const int min_debug_level = 0;
        public const int max_debug_level = 3;

        public const int level_none = 0;
        public const int level_error = 1;
        public const int level_warning = 2;
        public const int level_debug = 3;

        static int level = 1;       // default is error level

        public static void setLevel(int log_level)  {
            level = log_level;
        }

        public static int getLevel()  {
            return level;
        }

        public static void log(string info)
        {
            Console.Write(info + "\n");
        }

        public static void log_debug(string info)  {
            // only console output
            if (level >= level_debug)
                log ("Debug: " + info + "\n");
        }

        public static void log_warning(string info)   {
            // only console output
            if (level >= level_warning)
                log ("Warning: " + info + "\n");
        }

        public static void log_error(string info)  {
            // only console output
            if (level >= level_error)
                //log ("Error: " + info + "\n");
                log("Error: " + info);
        }

        public static void log_result(string info)  {
            log("Result: " + info);
        }
    }

    public static class S7cli_Status
    {

        //public static void show(Result_code result_code, string result_info = "")
        public static void show(int result_code, string result_info = "")
        {
            Logger.log("");
            Logger.log("Result: " + S7Status.get_info());
            string detailed_info = S7Status.get_detailed_info();

            if (detailed_info != "")
                Logger.log("Result info: " + detailed_info);

            if (result_info != "")
                Logger.log("Result info: " + result_info);
        }

        //public static void exit(Result_code result_code)
        public static void exit(int result_code)
        {
            Logger.log_debug("Exiting with status:" + result_code);
            Environment.Exit((int)result_code);
        }

        //public static void exit_with_info(Result_code result_code, string result_info = "")
        public static void exit_with_info(int result_code, string result_info = "")
        {
            show(result_code, result_info);
            exit(result_code);
        }

    }

    class s7cli
    {
        static Option_parser options;

        static public string get_version()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." +
                   Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
        }

        static public void show_logo()
        {
            string logo = @"
                                      
                  _|_|_|_|_|            _|  _|
          _|_|_|          _|    _|_|_|  _|    
        _|_|            _|    _|        _|  _|
            _|_|      _|      _|        _|  _|
        _|_|_|      _|          _|_|_|  _|  _|   " + get_version() + @"

        Command-line interface for Siemens SIMATIC Step7(tm)
        (C) 2013-2017 CERN, TE-CRG-CE

        Authors: Michal Dudek, Tomasz Wolak
";
            Console.Write(logo);
        }

        static public void show_available_commands()
        {
            Console.Write("\n\nAvailable commands:\n\n");
            foreach (string cmd in Option_parser.commands)
                Console.Write(String.Format("  {0:20}\n      - {1}\n\n", cmd, options.getCommandHelp(cmd)));
            Console.Write("\n\n");
        }

        static public void usage()
        {
            Console.Write("\n\nUsage: s7cli <command> [command args] [-h]\n");
        }

        static void Main(string[] args)
        {
            //Logger.setLevel(Logger.level_debug);   // switch on more debugging info
            show_logo();

            options = new Option_parser(args);

            if ( ( ! options.optionsOK() )  || options.needHelp())  {
                usage();
                if (options.needHelp()) {
                    Console.Write("\nCommand line options for '" + options.getCommand() + "':" +
                        options.getCommandOptionsHelp(options.getCommand()) + "\n");
                } else {
                    Console.Write("\nOption -h displays help for specified command.\n");
                    show_available_commands();
                }
                if ( ! options.optionsOK())
                    S7cli_Status.exit(S7Status.failure);
                S7cli_Status.exit(S7Status.success);
            }

            if (options.optionSet("--debug"))
            {                                         // set debug level from cmd line option
                int debug_level;
                int.TryParse(options.getOption("--debug"), out debug_level);
                if (debug_level >= Logger.min_debug_level && debug_level <= Logger.max_debug_level)  {
                    Logger.log_debug("Setting debug level to " + debug_level + ".");
                    Logger.setLevel(debug_level);
                } else {
                    Logger.log("Specified bug level is out of range (" + Logger.min_debug_level +
                        ", " + Logger.max_debug_level + ").\n");
                    S7cli_Status.exit(S7Status.failure);
                }
            }

            string command = options.getCommand();

            //System.Console.Write("\ncommand: " + command + "\n");
            //WinAPI winAPI = new WinAPI();
            //winAPI.test();
            //return;
            

            Console.Write("\n\n");

            S7Command s7command = new S7Command();

            try   {

                if (command == "listProjects")
                    s7command.getListOfProjects();

                else if (command == "createProject")
                    s7command.createProject(options.getOption("--projname"),
                                            options.getOption("--projdir"),
                                            S7ProjectType.S7Project);

                else if (command == "importConfig")
                    s7command.importConfig(options.getOption("--project"),
                                           options.getOption("--config"));

                else if (command == "createLib")
                    s7command.createProject(options.getOption("--libname"),
                                            options.getOption("--libdir"),
                                            S7ProjectType.S7Library);

                else if (command == "exportConfig")
                    s7command.exportConfig(options.getOption("--project"),
                                           options.getOption("--station"),
                                           options.getOption("--config"));

                else if (command == "listPrograms")
                    s7command.getListOfPrograms(options.getOption("--project"));

                else if (command == "importSymbols")
                    s7command.importSymbols(options.getOption("--project"),
                                            options.getOption("--symbols"),
                                            options.getOption("--program"));

                else if (command == "exportSymbols")  {
                    if (options.optionSet("--force"))
                        s7command.exportSymbols(options.getOption("--project"),
                                                options.getOption("--program"),
                                                options.getOption("--output"),
                                                options.getOption("--force") == "y");
                    else
                        s7command.exportSymbols(options.getOption("--project"),
                                                options.getOption("--program"),
                                                options.getOption("--output"));
                }

                else if (command == "listSources")
                    s7command.listSources(options.getOption("--project"),
                                          options.getOption("--program"));
                
                else if (command == "listBlocks")
                    s7command.listBlocks(options.getOption("--project"),
                                         options.getOption("--program"));

                else if (command == "importLibSources") {
                    if (options.optionSet("--force"))
                        s7command.importLibSources(options.getOption("--project"),
                                                   options.getOption("--library"),
                                                   options.getOption("--libprg"),
                                                   options.getOption("--program"),
                                                   options.getOption("--force") == "y");
                    else
                        s7command.importLibSources(options.getOption("--project"),
                                                   options.getOption("--library"),
                                                   options.getOption("--libprg"),
                                                   options.getOption("--program"));
                }

                else if (command == "importLibBlocks") {
                    if (options.optionSet("--force"))
                        s7command.importLibBlocks(options.getOption("--project"),
                                                  options.getOption("--library"),
                                                  options.getOption("--libprg"),
                                                  options.getOption("--program"),
                                                  options.getOption("--force") == "y");
                    else
                        s7command.importLibBlocks(options.getOption("--project"),
                                                  options.getOption("--library"),
                                                  options.getOption("--libprg"),
                                                  options.getOption("--program"));
                }

                else if (command == "importSources") {
                    if (options.optionSet("--force"))
                        s7command.importSources(options.getOption("--project"),
                                                options.getOption("--program"),
                                                options.getOption("--sources").Split(','),
                                                options.getOption("--force") == "y");
                    else
                        s7command.importSources(options.getOption("--project"),
                                                options.getOption("--program"),
                                                options.getOption("--sources").Split(','));
                }

                else if (command == "importSourcesDir") {
                    string srcdir = options.getOption("--srcdir");
                    /*Logger.log_debug("\nImporting source files\n\n");
                    Logger.log_debug("\nProject: " + projectDir + "\n");
                    Logger.log_debug("\nProgram: " + program + "\n");
                    Logger.log_debug("\ndirectory with sources to import: " + srcdir + "\n"); */
                    List<string> srcfileslist = new List<string>();
                    srcfileslist = new List<string>();
                    string[] ext2import = { "*.SCL", "*.AWL", "*.INP", "*.GR7" };
                    foreach (string ext in ext2import)
                        srcfileslist.AddRange(
                            System.IO.Directory.GetFiles(srcdir, ext,
                                System.IO.SearchOption.TopDirectoryOnly));
                    string[] srcfiles = srcfileslist.ToArray();
                    if (options.optionSet("--force"))
                        s7command.importSources(options.getOption("--project"),
                                                options.getOption("--program"),
                                                srcfiles,
                                                options.getOption("--force") == "y");
                    else
                        s7command.importSources(options.getOption("--project"),
                                                options.getOption("--program"),
                                                srcfiles);

                }

                else if (command == "compileSources")
                    s7command.compileSources(options.getOption("--project"),
                                             options.getOption("--program"),
                                             options.getOption("--sources").Split(','));

                else if (command == "exportSources")
                    s7command.exportSources(options.getOption("--project"),
                                            options.getOption("--program"),
                                            options.getOption("--sources").Split(','),
                                            options.getOption("--outputdir"));

                else if (command == "exportAllSources")
                    s7command.exportAllSources(options.getOption("--project"),
                                               options.getOption("--program"),
                                               options.getOption("--outputdir"));

                else if (command == "exportProgramStructure")
                    s7command.exportProgramStructure(options.getOption("--project"),
                                                     options.getOption("--program"),
                                                     options.getOption("--output"));

                else if (command == "compileStation")
                    s7command.compileStation(options.getOption("--project"),
                                             options.getOption("--station"));

                else if (command == "downloadSystemData") {
                    if (options.optionSet("--force"))
                        s7command.downloadSystemData(options.getOption("--project"),
                                                     options.getOption("--program"),
                                                     options.getOption("--force") == "y");
                    else
                        s7command.downloadSystemData(options.getOption("--project"),
                                                     options.getOption("--program"));
                }

                else if (command == "downloadAllBlocks") {
                    if (options.optionSet("--force"))
                        s7command.downloadAllBlocks(options.getOption("--project"),
                                                    options.getOption("--program"),
                                                    options.getOption("--force") == "y");
                    else
                        s7command.downloadAllBlocks(options.getOption("--project"),
                                                    options.getOption("--program"));
                }

                else if (command == "startCPU")
                    s7command.startCPU(options.getOption("--project"),
                                       options.getOption("--program"));

                else if (command == "stopCPU")
                    s7command.stopCPU(options.getOption("--project"),
                                      options.getOption("--program"));

                else  {
                    System.Console.WriteLine("Unknown command: " + command + "\n\n");
                    usage();
                    show_available_commands();
                    S7cli_Status.exit(S7Status.failure);
                }

            } catch (S7ProjectNotOpenException e) {
                Logger.log("Error: exception: project not opened with info:\n" + e.ToString() + ", " + e.Message + "\n");
                S7cli_Status.exit_with_info(S7Status.failure);
            }

            S7cli_Status.exit_with_info(S7Status.get_status());
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
    }
}
