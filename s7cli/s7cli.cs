using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Runtime.InteropServices;

using SimaticLib;

namespace S7_cli
{
    static class Logger
    {
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
                log ("Error: " + info + "\n");
        }

        public static void log_result(string info)  {
            log("Result: " + info);
        }
    }

    class s7cli
    {
        static readonly string logo = @"
                                      
                  _|_|_|_|_|            _|  _|
          _|_|_|          _|    _|_|_|  _|    
        _|_|            _|    _|        _|  _|
            _|_|      _|      _|        _|  _|
        _|_|_|      _|          _|_|_|  _|  _|

        Simatic Step7 command-line interface, v0.1
        (C) 2013 CERN, TE-CRG-CE Controls

        Authors: Michal Dudek, Tomasz Wolak
";
        static Option_parser options;

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

            Console.Write(logo);

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
                return;
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
                                            options.getOption("--projdir"));

                else if (command == "importConfig")
                    s7command.importConfig(options.getOption("--project"),
                                           options.getOption("--config"));

                else if (command == "listPrograms")
                    s7command.getListOfPrograms(options.getOption("--project"));

                else if (command == "importSymbols")
                    s7command.importSymbols(options.getOption("--project"),
                                            options.getOption("--symbols"),
                                            options.getOption("--program"));

                else if (command == "listSources")
                    s7command.listSources(options.getOption("--project"),
                                          options.getOption("--program"));

                else if (command == "importLibSources")
                    s7command.importLibSources(options.getOption("--project"),
                                               options.getOption("--library"),
                                               options.getOption("--libprg"),
                                               options.getOption("--program"));

                else if (command == "importLibBlocks")
                    s7command.importLibBlocks(options.getOption("--project"),
                                               options.getOption("--library"),
                                               options.getOption("--libprg"),
                                               options.getOption("--program"));

                else if (command == "importSources")
                    if (options.optionSet("--force"))
                        s7command.importSources(options.getOption("--project"),
                                                options.getOption("--program"),
                                                options.getOption("--sources").Split(','),
                                                options.getOption("--force") == "y");
                    else
                        s7command.importSources(options.getOption("--project"),
                                                options.getOption("--program"),
                                                options.getOption("--sources").Split(','));


                else if (command == "importSourcesDir")     {
                    string srcdir = options.getOption("--srcdir");
                    /*Logger.log_debug("\nImporting source files\n\n");
                    Logger.log_debug("\nProject: " + projectDir + "\n");
                    Logger.log_debug("\nProgram: " + program + "\n");
                    Logger.log_debug("\ndirectory with sources to import: " + srcdir + "\n"); */
                    List<string> srcfileslist = new List<string>();
                    srcfileslist = new List<string>();
                    string[] ext2import = { "*.SCL", "*.AWL", "*.INP" };
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

                } else if (command == "compileSources")
                    s7command.compileSources(options.getOption("--project"),
                                             options.getOption("--program"),
                                             options.getOption("--sources").Split(','));

                else {
                    System.Console.WriteLine("Unknown command: " + command + "\n\n");
                    usage();
                    show_available_commands();
                }

            } catch (S7ProjectNotOpenException e) { }

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
