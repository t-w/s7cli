/************************************************************************
 * S7Command.cs - a class with high-level operations on S7 projects     *
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimaticLib;
using S7HCOM_XLib;

namespace S7_cli
{
    /// <summary>
    /// Class to manage S7 command line execution status
    /// </summary>
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

        public static void set_status( int new_status )
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

        public static void set_detailed_info( string info )
        {
            detailed_info = info;
        }

        public static string get_detailed_info()
        {
            return detailed_info;
        }
    }


    /// <summary>
    /// Class with the s7cli commands, allows to execute s7cli command, check status,
    /// informing about errors etc.
    /// </summary>
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
            SimaticAPI simatic = SimaticAPI.Instance;
            Logger.log("List of available projects / standard libraries:\n");
            //Logger.log(S7Project.getListOfAvailableProjects());
            Logger.log( simatic.getListOfAvailableProjectsAsString() );
            S7Status.set_status(S7Status.success);
        }

        /// <summary>
        /// Create a new (empty) SIMATIC Step 7 project
        /// </summary>
        /// <param name="name">project name</param>
        /// <param name="dir">the path in which the directory with the new project will be created</param>
        /// <param name="type">project type (</param>
        /// <returns></returns>
        public bool createProject( string        name,
                                   string        dir,
                                   S7ProjectType type )
        {
            s7project = new S7Project(name, dir, type);
            if (s7project.isProjectOpened())
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return s7project.isProjectOpened();
        }

        public bool createProject( string name,
                                   string dir )
        {
            return createProject(name, dir, S7ProjectType.S7Project);
        }

        public bool createLibrary( string name,
                                   string dir )
        {
            return createProject(name, dir, S7ProjectType.S7Library);
        }


        /// <summary>
        /// Opens an existing SIMATIC Step7 project
        /// </summary>
        /// <param name="projectPathOrName">path to the project directory</param>
        /// <returns>S7Project object</returns>
        S7Project openProject( string projectPathOrName )
        {
            if ( s7project != null )
            {
                Logger.log_debug( "openProject(): a project is already opened:" +
                s7project.getS7ProjectName() + ", " + s7project.getS7ProjectPath() );
                return s7project;
            }

            Logger.log( "Opening the project: " + projectPathOrName );
            s7project = new S7Project( projectPathOrName );

            if ( !s7project.isProjectOpened() )
            {
                s7project = null;
                S7Status.set_status( S7Status.failure );
                //throw new S7ProjectNotOpenException("Project not opened!");
                Logger.log_error("Project not opened.");
            }

            return s7project;
        }


        /// <summary>
        /// Import hardware (station) configuration from a file
        /// </summary>
        /// <param name="projectPathOrName">Path or name of the project</param>
        /// <param name="projectConfigPath">Path of configuration file to import</param>
        /// <returns></returns>
        public bool importConfig( string projectPathOrName,
                                  string projectConfigPath )
        {
            // checking if config file exists
            if (!File.Exists(projectConfigPath)) {
                Logger.log("Error: Cannot import project configuration because config file " + projectConfigPath + " does not exist!\n");
                S7Status.set_status(S7Status.failure);
                return false;
            }

            if ( this.openProject( projectPathOrName ) == null )
                return false;

            bool cmd_status_ok = s7project.importConfig(projectConfigPath);
            if (cmd_status_ok)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return cmd_status_ok;
        }


        public bool exportConfig( string projectPathOrName,
                                  string stationName,
                                  string projectConfigPath )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return false;

            Logger.log("\n\nExporting hardware config of " + stationName + " to " + projectConfigPath + "\n");
            bool cmd_status_ok = s7project.exportConfig(stationName, projectConfigPath);
            if (cmd_status_ok)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
            return cmd_status_ok;
        }

        public bool compileStation( string projectPathOrName,
                                    string stationName )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return false;

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


        public void getListOfPrograms( string projectPathOrName )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            Logger.log("\nThe S7 programs found:\n");

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

        public int importSymbols( string projectPathOrName,
                                  string symbolsPath,
                                  string programName = "")
        {
            if ( this.openProject( projectPathOrName ) == null )
                return 0;

            //Logger.log_debug("S7Command::ImportSymbols(): " + programName + "\n");
            Logger.log("Importing symbols ");
            Logger.log("    from: " + symbolsPath);
            Logger.log("    to: - project: " + s7project.getS7ProjectName() + ", " + s7project.getS7ProjectPath());
            Logger.log("        - program: " + programName);


            if ( ! s7project.programExists( programName ) )
            {
                S7Status.set_status( S7Status.failure );
                return 0;
            }

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

        public void exportSymbols( string projectPathOrName,
                                   string programName,
                                   string symbolsOutputFile,
                                   bool   force = false )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(programName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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

        public void listSources( string projectPathOrName,
                                 string programName = "" )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(programName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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

        public void listBlocks( string projectPathOrName,
                                string programName = "" )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(programName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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

        public void downloadSystemData( string projectPathOrName,
                                        string projectProgramName,
                                        bool   force = false )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(projectProgramName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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


        public void downloadAllBlocks( string projectPathOrName,
                                       string projectProgramName,
                                       bool force = false )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(projectProgramName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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


        public void startCPU( string projectPathOrName,
                              string projectProgramName )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(projectProgramName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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

        public void stopCPU( string projectPathOrName,
                             string projectProgramName )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(projectProgramName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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


        public void importLibSources( string projectPathOrName,
                                      string libProjectName,
                                      string libProjectProgramName,
                                      string destinationProjectProgramName,
                                      bool   forceOverwrite = false )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(destinationProjectProgramName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

            if (s7project.importLibSources(libProjectName, libProjectProgramName,
                    destinationProjectProgramName, forceOverwrite))
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
                    
        }


        public void importLibBlocks( string projectPathOrName,
                                     string libProjectName,
                                     string libProjectProgramName,
                                     string destinationProjectProgramName,
                                     bool   forceOverwrite = false )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(destinationProjectProgramName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

            if (s7project.importLibBlocks(libProjectName, libProjectProgramName,
                    destinationProjectProgramName, forceOverwrite))
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
        }


        public void importSources( string   projectPathOrName,
                                   string   program,
                                   string[] sourceFiles,
                                   bool     forceOverwrite = false )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(program, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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

                if (s7project.importSource(program, srcfile, forceOverwrite) == null)
                    failure = true;
            }
            if (!failure)
                S7Status.set_status(S7Status.success);
            else
                S7Status.set_status(S7Status.failure);
        }


        public void compileSources( string   projectPathOrName,
                                    string   programName,
                                    string[] sources )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(programName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

            Logger.log("\nBuilding source(s) in the program: " + programName + "\n\n");
            foreach (string src in sources)  {
                Logger.log("\nCompiling the source: " + src);
                int result = s7project.compileSource(programName, src);
                 
                if (result == -10)
                {
                    // Result: unknown
                    //Logger.log("Result: " + s7project.getCompilationStatusInfo(result));
                    S7Status.set_status( S7Status.unknown );
                }
                else if (result < 0 && result > -10)
                {
                    // Result: error
                    Logger.log_error( s7project.getCompilationStatusInfo( result ) );
                    S7Status.set_status( S7Status.failure );
                }
                else if (result >= 0)
                {
                    // just warnings or no problems -> set success
                    S7Status.set_status( S7Status.success );
                }
            }

            if (! S7Status.status_set() )
                S7Status.set_status(S7Status.unknown);   // we cannot get any useful result from compile()...
        }


        public void exportSources( string    projectPathOrName,
                                   string    programName,
                                   string [] sources,
                                   string    exportDirectory )
        {
            if (!Directory.Exists(exportDirectory))  {
                Logger.log("Error: Cannot export source(s) - destination directory '" + exportDirectory + "' does not exist!\n");
                S7Status.set_status(S7Status.failure);
                return;
            }

            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(programName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

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


        public void exportAllSources( string projectPathOrName,
                                      string programName,
                                      string exportDirectory )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(programName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

            string[] sources = s7project.getSourcesList(programName);
            this.exportSources(projectPathOrName, programName, sources, exportDirectory);
        }


        public void exportProgramStructure( string projectPathOrName,
                                            string programName,
                                            string exportFileName )
        {
            if ( this.openProject( projectPathOrName ) == null )
                return;

            if (!s7project.programExists(programName, true))
            {
                S7Status.set_status(S7Status.failure);
                return;
            }

            Logger.log("\nExporting program structure of: " + programName + "\n\n");
            s7project.exportProgramStructure(programName, exportFileName);
            // TO ADD - setting status (not sure yet!)
        }

    }
}
