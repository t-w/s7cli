using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7_cli
{

    public static class S7Status
    {
        //public enum Result_code { success, failure, unknown };
        public const int success = 0;
        public const int failure = 1;
        public const int unknown = 2;

        static int status = -1;
        static string[] status_info = 
            {
                "Success",
                "Failure",
                "Unknown"
            };

        static string detailed_info = "";

        public static int get_status()
        {
            return status;
        }

        public static bool status_set()
        {
            return (status > -1);
        }

        public static void set_status(int new_status)
        {
            if (new_status < -1 || new_status > 2)
                throw new System.Exception("S7Status::set_status() - illegal value " + new_status + "!");
            status = new_status;
        }

        public static string get_info()
        {
            if (status_set())
                return status_info[status];
            else
                return "Status unset!";
        }

        public static void set_detailed_info(string info)
        {
            detailed_info = info;
        }

        public static string get_detailed_info()
        {
            return detailed_info;
        }
    }

    public class S7Command
    {
        S7Project s7project = null;
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
            S7Status.set_status(S7Status.success);
        }

        public bool createProject(string projectName, string projectDirectory)
        {
            s7project = new S7Project(projectName, projectDirectory);
            if (s7project.isProjectOpened())
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return s7project.isProjectOpened();
        }


        S7Project openProject(string projectPathOrName)
        {
            if (s7project == null) {
                Logger.log("Opening project: " + projectPathOrName);
                s7project = new S7Project(projectPathOrName);
            } else {
                Logger.log_debug("openProject(): a project is already opened:" +
                    s7project.getS7ProjectName() + ", " + s7project.getS7ProjectPath());
            }
            if (!s7project.isProjectOpened()) {
                S7Status.set_status(S7Status.failure);
                throw new S7ProjectNotOpenException("Project not opened!");
            }
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
                S7Status.set_status(S7Status.failure);
                return false;
            }
            this.openProject(projectPathOrName);
            bool cmd_status_ok = s7project.importConfig(projectConfigPath);
            if (cmd_status_ok)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return cmd_status_ok;
        }

        public bool exportConfig(string projectPathOrName, string stationName, string projectConfigPath)
        {
            this.openProject(projectPathOrName);
            bool cmd_status_ok = s7project.exportConfig(stationName, projectConfigPath);
            if (cmd_status_ok)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return cmd_status_ok;
        }

        public bool compileStation(string projectPathOrName, string stationName)
        {
            this.openProject(projectPathOrName);
            bool cmd_status_ok = s7project.compileStation(stationName);
            if (cmd_status_ok)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return cmd_status_ok;
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
            S7Status.set_status(S7Status.success);   // if project opened - should be ok...
        }

        private string getImportSymbolsReport()
        {
            // file found by accindent, seems not always named like this...
            const string reportFile = "c:\\ProgramData\\Siemens\\Automation\\Step7\\S7Tmp\\sym_imp.txt";
            if (File.Exists(reportFile))  {
                return File.ReadAllText(reportFile);
            } else {
                return "Import report file " + reportFile + " not found!";
            }

        }

        public int importSymbols(string projectPathOrName, string symbolsPath, string programName = "")
        {
            this.openProject(projectPathOrName);
            //Logger.log_debug("S7Command::ImportSymbols(): " + programName + "\n");
            Logger.log("Importing symbols ");
            Logger.log("    from: " + symbolsPath);
            Logger.log("    to: - project: " + s7project.getS7ProjectName() + ", " + s7project.getS7ProjectPath());
            Logger.log("        - program: " + programName);
            
            int symbolsImported;

            if (programName != "")
                symbolsImported = s7project.importSymbols(symbolsPath, programName);
            else
                symbolsImported = s7project.importSymbols(symbolsPath);
            Logger.log("Imported " + symbolsImported + " symbols.");
            Logger.log(@"*******************************
*** Report file contents ***:
" + this.getImportSymbolsReport() + "*******************************");
            if (symbolsImported > 0)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return symbolsImported;
        }

        public void exportSymbols(string projectPathOrName, string programName, 
                                  string symbolsOutputFile, bool force = false){
            this.openProject(projectPathOrName);

            string exportDirectory = Path.GetDirectoryName(symbolsOutputFile);
            if (!Directory.Exists(exportDirectory))  {
                Logger.log("Error: Cannot export symbols - destination directory '" + exportDirectory + "' does not exist!\n");
                S7Status.set_status(S7Status.failure);
                return;
            }

            if (Path.GetExtension(symbolsOutputFile) != ".sdf") {
                Logger.log("Error: Cannot export symbols - export file '" + symbolsOutputFile + 
                    "' has incorrect extension (it must be '.sdf'!).\n");
                S7Status.set_status(S7Status.failure);
                return;
            }

            if (File.Exists(symbolsOutputFile) && !force)
            {
                Logger.log("Error: Cannot export symbols - export file '" + symbolsOutputFile + "' already exists!\n");
                S7Status.set_status(S7Status.failure);
                return;
            }

            Logger.log("Exporting symbols ");
            Logger.log("    from: - project: " + s7project.getS7ProjectName() + ", " + s7project.getS7ProjectPath());
            Logger.log("          - program: " + programName);
            Logger.log("      to: " + symbolsOutputFile);

            int status = s7project.exportSymbols(symbolsOutputFile, programName);

            if (status == 0)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
        }

        public void listSources(string projectPathOrName, string programName = "")
        {
            this.openProject(projectPathOrName);
            Logger.log("List of sources in program '" + programName + "'\n");
            string [] sources = s7project.getSourcesList(programName);

            if (sources == null)  {
                S7Status.set_status(S7Status.failure);
                return;
            }
            foreach (string src in sources)
                Logger.log(src);
            Logger.log("\nSources found: " + sources.Length);
            S7Status.set_status(S7Status.success);
        }

        public void listBlocks(string projectPathOrName, string programName = "")
        {
            this.openProject(projectPathOrName);
            Logger.log("List of blocks in program '" + programName + "'\n");
            string[] blocks = s7project.getBlocksList(programName);

            if (blocks == null)
            {
                S7Status.set_status(S7Status.failure);
                return;
            }
            foreach (string block in blocks)
                Logger.log(block);
            Logger.log("\nBlocks found: " + blocks.Length);
            S7Status.set_status(S7Status.success);
        }

        public void downloadSystemData(string projectPathOrName, string projectProgramName, bool force = false)
        {
            this.openProject(projectPathOrName);
            string[] blocks;

            if (force)
            {
                blocks = s7project.downloadSystemData(projectProgramName, true);
            }
            else
            {
                blocks = s7project.downloadSystemData(projectProgramName);
            }

            Logger.log("List of blocks to be downloaded to the PLC:\n");

            if (blocks == null)
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

            foreach (string block in blocks)
                Logger.log(block);
            Logger.log("\nBlocks found: " + blocks.Length + "\n");

            if (blocks.Length == 0)
            {
                Logger.log_error("It seems, that there is no \"System data\" in the project!");
                S7Status.set_status(S7Status.failure);
                return;
            }

            S7Status.set_status(S7Status.success);
         }

        public void downloadAllBlocks(string projectPathOrName, string projectProgramName, bool force = false)
        {
            this.openProject(projectPathOrName);
            string[] blocks;

            if (force)
            {
                blocks = s7project.downloadAllBlocks(projectProgramName, true);
            }
            else
            {
                blocks = s7project.downloadAllBlocks(projectProgramName);
            }

            Logger.log("List of blocks to be downloaded to the PLC:\n");

            if(blocks == null)
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

            foreach (string block in blocks)
                Logger.log(block);
            Logger.log("\nBlocks found: " + blocks.Length);
            S7Status.set_status(S7Status.success);
        }

        public void startCPU(string projectPathOrName, string projectProgramName)
        {
            this.openProject(projectPathOrName);

            if (s7project.startCPU(projectProgramName))
            {
                S7Status.set_status(S7Status.success);
                return;
            }
            else
            {
                S7Status.set_status(S7Status.failure);
                return;
            }
        }

        public void stopCPU(string projectPathOrName, string projectProgramName)
        {
            this.openProject(projectPathOrName);

            if (s7project.stopCPU(projectProgramName))
            {
                S7Status.set_status(S7Status.success);
                return;
            }
            else
            {
                S7Status.set_status(S7Status.failure);
                return;
            }
        }

        public void importLibSources(string projectPathOrName, string libProjectName,
                                     string libProjectProgramName, string destinationProjectProgramName)
        {
            this.openProject(projectPathOrName);
            if (s7project.importLibSources(libProjectName, libProjectProgramName, 
                    destinationProjectProgramName))
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
                    
        }

        public void importLibBlocks(string projectPathOrName, string libProjectName,
                                    string libProjectProgramName, string destinationProjectProgramName)
        {
            this.openProject(projectPathOrName);
            if (s7project.importLibBlocks(libProjectName, libProjectProgramName, 
                    destinationProjectProgramName))
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
        }

        public void importSources(string projectPathOrName, string program, string[] sourceFiles, bool forceOverwrite = false)
        {
            this.openProject(projectPathOrName);

            Logger.log("\nImporting sources to program: " + program + "\n\n");
            bool failure = false;
            foreach (string srcfile in sourceFiles)   {
                Logger.log("\nImporting file: " + srcfile);

                // checking if file to import exists
                if (!File.Exists(srcfile)) {
                    Logger.log("Error: Cannot import - source file " + srcfile + " does not exist!\n");
                    failure = true;
                    continue;
                }

                if (s7project.addSource(program, srcfile, forceOverwrite) == null)
                    failure = true;
            }
            if (!failure)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
        }

        public void compileSources(string projectPathOrName, string programName, string[] sources)
        {
            this.openProject(projectPathOrName);

            Logger.log("\nBuilding source(s) in program: " + programName + "\n\n");
            foreach (string src in sources)  {
                Logger.log("\nCompiling source: " + src);
                s7project.compileSource(programName, src);
            }
            S7Status.set_status(S7Status.unknown);   // we cannot get any useful result from compile()...
        }


        public void exportSources(string projectPathOrName, string programName, string [] sources, string exportDirectory)
        {
            if (!Directory.Exists(exportDirectory))  {
                Logger.log("Error: Cannot export source(s) - destination directory '" + exportDirectory + "' does not exist!\n");
                S7Status.set_status(S7Status.failure);
                return;
            }

            this.openProject(projectPathOrName);

            Logger.log("\nExporting source(s) from program: " + programName + " to " + exportDirectory + ".\n\n");

            bool failed = false;
            foreach (string src in sources)  {
                string exportFilename = exportDirectory + "\\" + src;// +
                    //s7project.getSourceFileExtension(programName, src);

                string srcType = s7project.getSourceTypeString(programName, src);
                Logger.log("\nExporting source: " + src + " (" + srcType + ") ... ");

                if (s7project.exportSource(programName, src, exportFilename) == 0)  {
                    Logger.log("Done!");
                } else {
                    failed = true;
                    Logger.log("Failed!");
                }
            }
            if (!failed)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
        }


        public void exportAllSources(string projectPathOrName, string programName, string exportDirectory)
        {
            this.openProject(projectPathOrName);
            string[] sources = s7project.getSourcesList(programName);
            this.exportSources(projectPathOrName, programName, sources, exportDirectory);
        }


        public void exportProgramStructure(string projectPathOrName, string programName, string exportFileName)
        {
            this.openProject(projectPathOrName);

            Logger.log("\nExporting program structure of: " + programName + "\n\n");
            s7project.exportProgramStructure(programName, exportFileName);
            // TO ADD - setting status (not sure yet!)
        }

    }
}