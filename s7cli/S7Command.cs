using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7_cli
{
    public class CLIexec
    {
        public CLIexec(string[] args)
        {
            CLI_parser commandParser = new CLI_parser(args);

            switch (commandParser.programAction)
            {
                case 0:
                    this.displayHelp();
                    Environment.Exit(1);
                    break;

                case 1:
                    this.createProject(commandParser.projectName, commandParser.projectDirPath);
                    break;

                case 2:
                    this.importProjectConfig(commandParser.projectDirPath, commandParser.projectConfigPath);
                    break;

                case 3:
                    this.importSymbols(commandParser.projectDirPath, commandParser.symbolsPath, commandParser.programName);
                    break;

                case 4:
                    this.importLibSources(commandParser.projectDirPath, commandParser.libProjectName, commandParser.libProjectProgramName, commandParser.destinationProjectProgramName);
                    break;

                case 5:
                    this.importLibBlocks(commandParser.projectDirPath, commandParser.libProjectName, commandParser.libProjectProgramName, commandParser.destinationProjectProgramName);
                    break;
            }
        }

        public void createProject(string projectName, string projectDirPath)
        {
            CryoAutomation.S7Project S7CLI = new CryoAutomation.S7Project(projectName, projectDirPath);
        }

        public void importProjectConfig(string projectDirPath, string projectConfigPath)
        {
            CryoAutomation.S7Project S7CLI = new CryoAutomation.S7Project(projectDirPath);
            S7CLI.importConfig(projectConfigPath);
        }

        public void importSymbols(string projectDirPath, string symbolsPath, string programName)
        {
            Console.Write(programName + "\n");

            CryoAutomation.S7Project S7CLI = new CryoAutomation.S7Project(projectDirPath);
            
            if(programName != "")
            {
                S7CLI.importSymbols(symbolsPath, programName);
            }
            else
            {
                S7CLI.importSymbols(symbolsPath);
            }
        }

        public void importLibSources(string projectDirPath, string libProjectName, string libProjectProgramName, string destinationProjectProgramName)
        {
            CryoAutomation.S7Project S7CLI = new CryoAutomation.S7Project(projectDirPath);
            S7CLI.importSources(libProjectName, libProjectProgramName, destinationProjectProgramName);
        }

        public void importLibBlocks(string projectDirPath, string libProjectName, string libProjectProgramName, string destinationProjectProgramName)
        {
            CryoAutomation.S7Project S7CLI = new CryoAutomation.S7Project(projectDirPath);
            S7CLI.importBlocks(libProjectName, libProjectProgramName, destinationProjectProgramName);
        }

        public void displayHelp()
        {
            Console.Write("Allowed options:\n");
        }
    }
}
