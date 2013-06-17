using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7_cli
{
    public class S7Command
    {
        S7Project s7project;
        string error_info = "";

        public string getErrorInfo()
        {
            return error_info;
        }

        /**********************************************************************************
         * Project commands
         **********************************************************************************/

        public void getListOfProjects()
        {
            Logger.log("List of available projects / standard libraries:\n");
            Logger.log(S7Project.getListOfAvailableProjects());
        }

        public bool createProject(string projectName, string projectDirectory)
        {
            s7project = new S7Project(projectName, projectDirectory);
            return s7project.isProjectOpened();
        }


        public S7Project openProject(string projectPathOrName)
        {
            Logger.log("Opening project: " + projectPathOrName);
            s7project = new S7Project(projectPathOrName);
            if ( !s7project.isProjectOpened())
                throw new S7ProjectNotOpenException("Project not opened!");
            return s7project;
        }

        public S7Project getProject()
        {
            if (s7project.isProjectOpened())
                return s7project;
            else return null;
        }

        public bool importConfig(string projectPathOrName, string projectConfigPath)
        {
            // checking if config file exists
            if (!File.Exists(projectConfigPath)) {
                Logger.log("Error: Cannot import project configuration because config file " + projectConfigPath + " does not exist!\n");
                return false;
            }
            this.openProject(projectPathOrName);
            return s7project.importConfig(projectConfigPath);
        }


        /**********************************************************************************
         * Program commands
         **********************************************************************************/


        public void getListOfPrograms(string projectPathOrName)
        {
            this.openProject(projectPathOrName);
            Logger.log("List of available programs:\n");

            string [] programs = s7project.getListOfAvailablePrograms();
            foreach (string program in programs)
                Logger.log(program);
        }

        public int importSymbols(string projectPathOrName, string symbolsPath, string programName = "")
        {
            this.openProject(projectPathOrName);
            //Logger.log_debug("S7Command::ImportSymbols(): " + programName + "\n");
            Logger.log("Importing symbols ");
            Logger.log("    from: " + symbolsPath);
            Logger.log("    to: - project: " + s7project.getS7ProjectName() + ", " + projectPathOrName );
            Logger.log("        - program: " + programName);
            
            int symbolsImported;

            if (programName != "")
                symbolsImported = s7project.importSymbols(symbolsPath, programName);
            else
                symbolsImported = s7project.importSymbols(symbolsPath);
            Logger.log("Imported " + symbolsImported + " symbols.");
            return symbolsImported;
        }

        public void listSources(string projectPathOrName, string programName = "")
        {
            this.openProject(projectPathOrName);
            Logger.log("List of sources in program '" + programName + "'\n");
            string [] sources = s7project.getSourcesList(programName);
            foreach (string src in sources)
                Logger.log(src);
            Logger.log("\nSources found: " + sources.Length);

        }

        public void importLibSources(string projectPathOrName, string libProjectName,
                                     string libProjectProgramName, string destinationProjectProgramName)
        {
            this.openProject(projectPathOrName);
            s7project.importLibSources(libProjectName, libProjectProgramName, destinationProjectProgramName);
        }

        public void importLibBlocks(string projectPathOrName, string libProjectName,
                                    string libProjectProgramName, string destinationProjectProgramName)
        {
            this.openProject(projectPathOrName);
            s7project.importLibBlocks(libProjectName, libProjectProgramName, destinationProjectProgramName);
        }

        public void importSources(string projectPathOrName, string program, string[] sourceFiles, bool forceOverwrite = false)
        {
            this.openProject(projectPathOrName);

            Logger.log("\nImporting sources to program: " + program + "\n\n");
            foreach (string srcfile in sourceFiles)   {
                Logger.log("\nImporting file: " + srcfile);
                s7project.addSource(program, srcfile, forceOverwrite);
            }
        }

        public void compileSources(string projectPathOrName, string programName, string[] sources)
        {
            this.openProject(projectPathOrName);

            Logger.log("\nBuilding source(s) in program: " + programName + "\n\n");
            foreach (string src in sources)  {
                Logger.log("\nCompiling source: " + src);
                s7project.compileSource(programName, src);
            }

        }

    }
}