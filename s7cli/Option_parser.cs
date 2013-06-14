using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7_cli
{
    class CLI_parser
    {
        // program actions
        public int programAction;

        // program switches
        public string projectName = "";
        public string projectConfigPath = "";
        public string projectDirPath = "";
        public string symbolsPath = "";
        public string programName = "";
        public string libProjectName = "";
        public string libProjectProgramName = "";
        public string destinationProjectProgramName = "";

        public CLI_parser(string[] args)
        {
            // nr of passed arguments to the program
            int nrOfArguments = args.Length;

            // program actions
            this.programAction = 0;

            // C# does not count program name as argument
            if (nrOfArguments != 0)
            {
                if (args[0].CompareTo("createProject") == 0)
                {
                    this.programAction = 1;
                }
                else if (args[0].CompareTo("importProjectConfig") == 0)
                {
                    this.programAction = 2;
                }
                else if (args[0].CompareTo("importSymbols") == 0)
                {
                    this.programAction = 3;
                }
                else if (args[0].CompareTo("importLibSources") == 0)
                {
                    this.programAction = 4;
                }
                else if (args[0].CompareTo("importLibBlocks") == 0)
                {
                    this.programAction = 5;
                }

                for (int currentArgument = 1; currentArgument < nrOfArguments; currentArgument++)
                {
                    if (args[currentArgument].CompareTo("--config") == 0 || args[currentArgument].CompareTo("-c") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.projectConfigPath = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Path to config file is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }

                    if (args[currentArgument].CompareTo("--directory") == 0 || args[currentArgument].CompareTo("-d") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.projectDirPath = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Project directory is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }

                    if (args[currentArgument].CompareTo("--lib-proj-name") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.libProjectName = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Name of the library project is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }

                    if (args[currentArgument].CompareTo("--lib-proj-prog-name") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.libProjectProgramName = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Name of the library project program is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }

                    if (args[currentArgument].CompareTo("--dest-proj-prog-name") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.destinationProjectProgramName = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Name of the destination project program is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }

                    if (args[currentArgument].CompareTo("--project-name") == 0 || args[currentArgument].CompareTo("-n") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.projectName = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Name of the project is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }

                    if (args[currentArgument].CompareTo("--program-name") == 0 || args[currentArgument].CompareTo("-p") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.programName = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Program name is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }

                    if (args[currentArgument].CompareTo("--symbols") == 0 || args[currentArgument].CompareTo("-s") == 0)
                    {
                        if (nrOfArguments >= (currentArgument + 1))
                        {
                            this.symbolsPath = args[currentArgument + 1];
                        }
                        else
                        {
                            Console.Write("Error: Path to file with symbols is not correctly specified.\n");
                            Environment.Exit(1);
                        }
                    }
                }
            }
        }
    }
}
