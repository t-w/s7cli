/************************************************************************
 * S7Project.cs - class with S7project and interface with SIMATIC       *
 *                                                                      *
 * Copyright (C) 2013-2018 CERN                                         *
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
//using System.Runtime.InteropServices;
//using System.Windows.Automation;
using System.Collections.Generic;

using SimaticLib;
using S7HCOM_XLib;

namespace S7_cli
{
    /// class SimaticAPI
    /// <summary>
    /// main interface for accessing Simatic Step7 environment
    /// </summary>
    ///
    public class SimaticAPI
    {
        private static Simatic simatic = null;

        /*
         * Constructor
         */
        public SimaticAPI()
        {
            simatic = new Simatic();

            if (simatic == null)  {
                Logger.log_error("SimaticAPI(): cannot initialize Simatic");
            } 

            Logger.log_debug("AutomaticSave: " + simatic.AutomaticSave.ToString());

            // force server mode
            enableUnattendedServerMode();
        }


        public void enableUnattendedServerMode()
        {
            if (simatic != null) {
                simatic.UnattendedServerMode = true;
                Logger.log_debug("UnattendedServerMode: " + simatic.UnattendedServerMode.ToString());
            } else {
                Logger.log_error("Cannot set \"UnattendedServerMode\" to true! Simatic variable is null!");
            }
        }

        public void disableUnattendedServerMode()
        {
            if (simatic != null) {
                simatic.UnattendedServerMode = false;
                Logger.log_debug("UnattendedServerMode: " + simatic.UnattendedServerMode.ToString());
            } else {
                Logger.log_error("Cannot set \"UnattendedServerMode\" to false! Simatic variable is null!");
            }
        }

        public string getListOfAvailableProjects()
        {
            string availableProjects = "";
            foreach (IS7Project project in simatic.Projects)  {
                availableProjects += ("- " + project.Name + ", " + project.LogPath + "\n");
            }
            return availableProjects;
        }

        public Simatic getSimatic()
        {
            return simatic;
        }


        /*
         * Destructor
         */
        ~SimaticAPI()
        {
            //Logger.log_debug("AutomaticSave: " + simatic.AutomaticSave.ToString());
            //Logger.log_debug("UnattendedServerMode: " + simatic.UnattendedServerMode.ToString());

            // make sure of saving all changes
            if (simatic != null){
                Logger.log_debug("Saving changes.");
                simatic.Save();
            }
        }
    
    }



    /// class S7ProjectNotOpenException
    /// <summary>
    /// S7 project not open excetion
    /// </summary>
    ///
    public class S7ProjectNotOpenException : System.ApplicationException
    {
        public S7ProjectNotOpenException() { }
        public S7ProjectNotOpenException(string message) { }
        public S7ProjectNotOpenException(string message, System.Exception inner) { }

        // Constructor needed for serialization 
        // when exception propagates from a remoting server to the client.
        protected S7ProjectNotOpenException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }


    public class SimaticProgram
    {
        private string name;
        private S7Program s7program;

        public SimaticProgram(string programName, S7Program program)
        {
            name = programName;
            s7program = program;
        }

        public string getName()
        {
            return this.name;
        }

        public IS7Program getProgram()
        {
            return this.s7program;
        }


        public IS7SymbolTable getSymbolTable()
        {
            try
            {
                return this.s7program.SymbolTable;
            }
            catch (System.Exception exc)
            {
                Logger.log_error("\n** getSymbolTable(): Error accessing the symbol table for the program: '" +
                                 name + "':\n" + exc.Message + "\n");
                return null;
            }
        }

        public int importSymbols(string symbolsPath)
        {
            if (!File.Exists(symbolsPath))
            {
                Logger.log_error("Error: File " + symbolsPath + " does not exist! Aborting import!\n");
                return 0;
            }

            int nrOfSymbols = 0;

            try
            {
                S7SymbolTable symbolTable = (S7SymbolTable)getSymbolTable();
                nrOfSymbols = symbolTable.Import(symbolsPath);
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                // return -1; / return error here???
            }
            return nrOfSymbols;
        }

        /* returned value
         * 0    - success
         * != 0 - failure
         */
        public int exportSymbols(string symbolsOutputFile)
        {
            IS7SymbolTable symbol_table = this.getSymbolTable();
            if (symbol_table == null)
                return -1;
            try
            {
                symbol_table.Export(symbolsOutputFile);
            }
            catch (SystemException e)
            {
                Logger.log_debug("\n** exportSymbols(): Error exporting the symbol table for the program: '" +
                     this.name + "':\n" + e.Message + "\n");
                return -1;
            }
            return 0;
        }


        public S7SWItem getSoftwareItem(string itemName)
        {
            S7SWItems items = this.s7program.Next;
            //Logger.log_debug("\nSoftware items count: " + items.Count + "\n");
            foreach (S7SWItem item in items)
            {
                //Logger.log_debug("\nitem: " + item.Name + "\n");
                if (item.Name == itemName)
                    return item;
            }
            /*
            S7SWItem item;
            try
            {
                item = items.Next[itemName];
            }
            catch (System.Exception exc)
            {
            }*/
            return null;
        }

        public S7SWItem getSources()
        {
            return this.getSoftwareItem("Sources");
        }

        public S7SWItem getBlocks()
        {
            return this.getSoftwareItem("Blocks");
        }

        private S7SWItems getSourceModules()
        {
            S7SWItem sources = this.getSources();
            //Logger.log_debug("\nsources LogPath: " + sources.LogPath + "\n");
            if (sources == null)
                return null;

            S7SWItems src_modules = sources.Next;
            /*Logger.log_debug("\nitems2 count: " + src_modules.Count + "\n");
            foreach (S7SWItem src_module in src_modules)
            {
                Logger.log_debug("\nsrc name: " + src_module.Name + "\n");
            }*/
            return src_modules;
        }


        public string[] getSourcesList()
        {
            List<string> srcs = new List<string>();
            S7SWItems src_modules = this.getSourceModules();
            if (src_modules == null)
                return null;   // failure
            foreach (S7SWItem src_module in src_modules)
            {
                //Logger.log_debug("\nsrc name: " + src_module.Name + "\n");
                srcs.Add(src_module.Name);
            }
            return srcs.ToArray();
        }


        private IS7SWItem getSourceModule(string sourceName)
        {
            /*
             foreach (S7SWItem src_module in getSourceModules(programName))   {
                //Logger.log_debug("\nsrc name: " + src_module.Name + "\n");
                if (src_module.Name == sourceName)
                    return src_module;
            }
             return null;*/
            try
            {
                IS7SWItem src_module = this.getSourceModules()[sourceName];
                return src_module;
            }
            catch (System.Exception exc)
            {
                Logger.log_debug("\n** getSourceModule(): Error getting the source '" + sourceName + "':\n" + exc.Message + "\n");
                return null;
            }
        }


        public bool sourceExists(string sourceName)
        {
            if (this.getSourceModule(sourceName) != null)
                return true;
            else
                return false;
        }


        private IS7SWItem addSourceWithType(S7SWObjType sourceType, string filename, bool forceOverwrite = false)
        {
            S7SWItems src_modules = this.getSourceModules();
            IS7SWItem item;

            string sourceName = System.IO.Path.GetFileNameWithoutExtension(filename);

            if (forceOverwrite && this.sourceExists(sourceName))
            {
                Logger.log("The source '" + sourceName + "' already exists - removing it (overwriting forced!)...");
                src_modules.Remove(sourceName);
                Logger.log("... and importing the new one.");
            }
            try
            {
                item = src_modules.Add(sourceName, sourceType, filename);
            }
            catch (System.Exception exc)
            {
                Logger.log("\n** Error importing '" + filename + "':\n" + exc.Message + "\n");
                item = null;
            }
            return item;
        }


        public IS7SWItem addSource(string filenameFullPath, bool forceOverwrite = false)
        {
            string filename = System.IO.Path.GetFileName(filenameFullPath);
            string extension = System.IO.Path.GetExtension(filename);
            if (extension.ToLower() == ".scl" ||
                extension.ToLower() == ".awl" ||
                extension.ToLower() == ".inp" ||
                extension.ToLower() == ".gr7")
            //if (Array.IndexOf ( string [] Array = {".scl", ".awl"}, extension.ToLower() > -1 )
                return this.addSourceWithType(S7SWObjType.S7Source, filenameFullPath, forceOverwrite);
            else {
                Logger.log("addSource(): Error - unknown source extension '" + extension + "' (file: " + filename + ")\n");
                return null;
            }
        }


        public void removeSource(string sourceName)
        {
            IS7SWItem src = this.getSourceModule(sourceName);
            try
            {
                src.Remove();
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: removeBlock()" + exc.Message + "\n");
                //return false;
            }
        }




        public S7Block getBlock(string blockName)
        {
            foreach (S7Block block in s7program.Next["Blocks"].Next)
            {
                if (block.Name == blockName)
                {
                    Logger.log_debug("getBlock(): found block: " + blockName);
                    return block;
                }
            }
            Logger.log_debug("getBlock(): the block " + blockName + " not found");
            return null;
        }

        public bool blockExists(string blockName)
        {
            if (this.getBlock(blockName) != null)
            {
                Logger.log_debug("blockExists(): found block: " + blockName);
                return true;
            }

            Logger.log_debug("blockExists(): the block " + blockName + " not found");
            return false;
        }

        public void removeBlock(string blockName)
        {
            S7Block block = this.getBlock(blockName);
            try
            {
                block.Remove();
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: removeBlock()" + exc.Message + "\n");
                //return false;
            }
        }




        public S7Source getS7SourceModule(string moduleName)
        {
            //S7SWItem item = getSourceModule(programName, moduleName);
            //S7Source source = (S7Source) item.Program.;

            //IS7Source src = getSources("ARC56_program").get_Child("test11"); //get_Collection("test11");
            S7Source src;
            try
            {
                src = (S7Source) this.s7program.Next["Sources"].Next[moduleName];
            }
            catch (Exception exc)
            {
                Logger.log_error("Warning: " + exc.Message + "\n");
                src = null;
            }

            return src;
        }

        /* compileSource()
         * 
         * return values:
         *    0 success
         *   <0 error
         *   -1 source not found
         *   -2 exception during compilation
         *    1 unknown
         */
        public int compileSource(string sourceName)
        {
            S7Source src = getS7SourceModule(sourceName);
            if (src == null)
                return -1;
            try
            {
                IS7SWItems items = src.Compile();  // this thing returns nothing useful -> bug in SimaticLib(!)
            }
            catch (System.Exception exc)
            {
                Logger.log("\n** compileSource(): Error compiling '" + sourceName + "':\n" + exc.Message + "\n");
                return -2;
            }
            //IntPtr ptr = new IntPtr((Void *) items. );
            /*Console.Write("\n" + items. ToString() + "\n" + items.Count + "\n");
            Type itype = items.GetType();
            Console.Write("\n" + itype + "\n");
            
            int i=0;
            foreach (S7SWItem item in items)
            {
                i++;
            }
            Console.Write("\ni = " + i + "\n");

            int a = src.AppWindowHandle; */
            //Console.Write("\n" + src.AppWindowHandle + "\n");
            //src.AppWindowHandle
            //src.AppWindowHandle
            //try{
            //  AutomationElement s7scl = AutomationElement.FromHandle(new IntPtr(src.AppWindowHandle));
            //}
            //catch (System.Exception e)
            //{
            //    e.HResult.get();
            //}
            //System.HR
            //s7scl.SetFocus();
            return 1;
        }

        private S7SourceType getSourceType(string sourceName)
        {
            S7Source src = getS7SourceModule(sourceName);
            Logger.log_debug("getSourceType(" + this.name + ", " + sourceName + ")\n\n");
            Logger.log_debug("returns: " + src.ConcreteType.GetType() + "\n\n");
            Logger.log_debug("returns: " + src.ConcreteType + "\n\n");
            return //(S7SourceType)
                src.ConcreteType;
        }

        public string getSourceTypeString(string sourceName)
        {
            /*S7SourceType srcType = this.getSourceType(programName, sourceName);
            if      (srcType == S7SourceType.S7AWL)     return "S7AWL";
            else if (srcType == S7SourceType.S7GG)      return "S7GG";
            else if (srcType == S7SourceType.S7GR7)     return "S7GR7";
            else if (srcType == S7SourceType.S7NET)     return "S7NET";
            else if (srcType == S7SourceType.S7SCL)     return "S7SCL";
            else if (srcType == S7SourceType.S7SCLMake) return "S7SCLMake";
            else if (srcType == S7SourceType.S7ZG)      return "S7ZG";
            else                                        return "Unknown"; */
            S7Source src = getS7SourceModule(sourceName);
            Logger.log_debug("getSourceType(" + this.name + ", " + sourceName + ")\n\n");
            if (src != null)
            {
                Logger.log_debug("returned: " + src.ConcreteType.GetType() + "\n\n");
                Logger.log_debug("returned: " + src.ConcreteType + "\n\n");
                return //(S7SourceType)
                    src.ConcreteType.ToString();
            }
            else
            {
                Logger.log_debug("returned: null(!)\n\n");
                return null;
            }

        }

        //public string getSourceFileExtension(string programName, string sourceName)
        //{
        //S7SourceType srcType = this.getSourceType(programName, sourceName);
        /*if (srcType == S7SourceType.S7AWL)          return ".AWL";
        else if (srcType == S7SourceType.S7GG)      return ".S7GG";    // to check
        else if (srcType == S7SourceType.S7GR7)     return ".S7GR7";   // to check
        else if (srcType == S7SourceType.S7NET)     return ".S7NET";   // to check
        //else if (srcType == S7SourceType.S7SCL)     return ".SCL";
        else if ((string)srcType == "S7SCL") return ".SCL";
        else if (srcType == S7SourceType.S7SCLMake) return ".INP";
        else if (srcType == S7SourceType.S7ZG)      return ".S7ZG";    // to check
        else return ".UnknownSourceType";*/
        //    Logger.log_debug("getSourceFileExtension() returns: " + this.getSourceTypeString(programName, sourceName) + "\n\n");
        //    return this.getSourceTypeString(programName, sourceName);
        //}

        /*
         * returned value:
         * 0 - success
         * !0 - error
         */
        public int exportSource(string sourceName, string ExportFileName)
        {
            S7Source src = getS7SourceModule(sourceName);
            if (src == null)
                return -1;
            try
            {
                //Logger.log_debug("Nous sommes la");
                src.Export(ExportFileName);
            }
            catch (System.Exception exc)
            {
                Logger.log("\n** Error exporting '" + sourceName + "':\n" + exc.Message + "\n");
                return -1;
            }
            return 0;
        }

        public int exportProgramStructure( string ExportFileName,
                                           bool   ExportDuplicateCalls = true,
                                           int    ColumnFlags = 0)
        {            // ExportProgramStructure(ByVal ExportFileName As String, ByVal ExportDuplicateCalls As Boolean, ByVal ColumnFlags As Long)
            /*if (simaticProject == null)
            {
                Logger.log_debug("exportProgramStructure(): Error: The project is not opened! Aborting operation!\n");
                return 1;
            }*/

            //S7Program program = (S7Program) this.getProgram(programName);
            //S7Program program = (S7Program)simaticProject.Programs[programName];
            this.s7program.ExportProgramStructure(ExportFileName, ExportDuplicateCalls, ColumnFlags);
            return 0;
        }

    }



    /// class S7Project
    /// <summary>
    /// open and work with a single S7 project
    /// </summary>
    ///
    public class S7Project
    {
        
        private SimaticAPI simaticapi = null;
        private IS7Project simaticProject = null;

        //private IDictionary<string, IS7Program> programs = null;
        private IDictionary<string, SimaticProgram> programs = null;

        /*
         * Constructors
         */

        //public plcProject(string Name)
        public S7Project(string pathOrName)
        {
            simaticapi = new SimaticAPI();
            Simatic simatic = simaticapi.getSimatic();

            foreach (IS7Project project in simatic.Projects)  {
                //System.Console.Write("Project creator: \n" + project.Creator simatic.Projects.Count + "\n");
                /*System.Console.Write("Project name: " + project.Name + "\n");
                System.Console.Write("Project creator: " + project.Creator + "\n");
                System.Console.Write("Project comment: " + project.Comment + "\n");
                System.Console.Write("Project LogPath: " + project.LogPath + "\n");
                System.Console.Write("stations count: " + project.Stations.Count + "\n");
                System.Console.Write("path from command: " + Path + "\n");*/

                if ( //project.Name == "ARC_LSS" &&
                    //project.LogPath == "D:\\controls\\apps\\sector56\\plc\\mirror56")
                    //project.LogPath == Path)
                    project.LogPath.ToLower() == pathOrName.ToLower() ||
                    project.Name.ToLower() == pathOrName.ToLower() )
                {
                    Logger.log_debug("S7Project(): Found project: " + project.Name + ", " + project.LogPath);
                    simaticProject = project;
                    break;
                }
            }

            if (simaticProject == null) {
                Logger.log_error("Project with the path or name: " + pathOrName +
                    " not found on the list of available projects!!!\n\nAvailable projects:\n");
                Logger.log_error(simaticapi.getListOfAvailableProjects());
                return;
            }

            this.updatePrograms();
        }


        public S7Project(string projectName, string projectDirPath, S7ProjectType projectType)
        {
            simaticapi = new SimaticAPI();
            Simatic simatic = simaticapi.getSimatic();

            // checking if the project dir path ends with "\"
            if (!projectDirPath.EndsWith("\\"))
            {
                projectDirPath = projectDirPath + "\\";
            }

            // concatenating real project dir path
            string projectPath;

            if (projectName.Length > 8)
            {
                projectPath = projectDirPath + projectName.Substring(0, 8);
            }
            else
            {
                projectPath = projectDirPath + projectName;
            }

            // checking if directory path is not taken
            if (Directory.Exists(projectPath))
            {
                Logger.log_error("Error: Cannot create the project because the folder " + projectPath + " already exists!\n");
                Logger.log_error("Error: The project not created! Exiting program!\n");
                Environment.Exit(1);
            }
            else
            {
                try
                {
                    simaticProject = simatic.Projects.Add(projectName, projectDirPath, projectType);
                }
                catch (SystemException exc)
                {
                    Logger.log_error("Error in S7Project(): " + exc.Message + "\n");
                    // should we set simaticProject to null here?
                    return;
                }
            }

            this.updatePrograms();
        }


        /*
         * Project methods
         */

        public bool isProjectOpened()
        {
            if (this.simaticProject != null)
                return true;
            else
                return false;
        }

        public bool checkProjectOpened()
        {
            if (!this.isProjectOpened())
            {
                Logger.log_error("Error: Project variable \"simaticProject\" not initialized!\n");
                return false;
            }
            return true;
        }

        /*
        private void addProject(string Name, string Path)
        {
            //IS7Project project = simatic.Projects.Add("ARC_LSS", "D:\\controls\\apps\\sector56\\plc\\mirror56");
            IS7Project project = simatic.Projects.Add(Name, Path);
        }*/

        public IS7Project getS7Project()
        {
            return simaticProject;
        }


        public string getS7ProjectName()
        {
            return simaticProject.Name;
        }

        public string getS7ProjectPath()
        {
            return simaticProject.LogPath;
        }

        /**********************************************************************************
         * Program methods
         **********************************************************************************/

        public bool importConfig(string projectConfigPath)
        {
            if ( ! checkProjectOpened() )  {
                Logger.log_error("Error: Project not opened - aborting import!\n");
                return false;
            }
            try  {
                simaticProject.Stations.Import(projectConfigPath);
            } catch (SystemException exc) {
                Logger.log_error("Error: " + exc.Message + "\n");
                return false;
            }
            return true;
        }

        public bool exportConfig(string stationName, string projectConfigPath)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting export!\n");
                return false;
            }

            try
            {
                S7Station station = simaticProject.Stations[stationName];
                station.Export(projectConfigPath);
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                return false;
            }

            return true;
        }

        public bool compileStation(string stationName)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting station compilation!\n");
                return false;
            }

            try
            {
                S7Station station = simaticProject.Stations[stationName];
                station.Compile();
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                return false;
            }

            return true;
        }

        public string[] getBlocksList(string projectProgramName)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting!\n");
                return null;
            }

            List<string> blocks = new List<string>();

            try
            {
                foreach (S7Block block in simaticProject.Programs[projectProgramName].Next["Blocks"].Next)
                {
                    //Logger.log_debug(block.Name);
                    blocks.Add(block.Name);
                }
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                return null;
            }
            return blocks.ToArray();
        }


        private void downloadBlock(S7Block block, bool force)
        {
            Logger.log_debug("Downloading the block: " + block.Name);
            block.Download(force ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk);
        }

        public string[] downloadSystemData(string projectProgramName, bool force = false)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting!\n");
                return null;
            }

            List<string> blocks = new List<string>();
            try
            {
                if (force)
                {
                    simaticapi.enableUnattendedServerMode();
                }
                else
                {
                    simaticapi.disableUnattendedServerMode();
                }

                foreach (S7Block block in simaticProject.Programs[projectProgramName].Next["Blocks"].Next)
                {
                    if (block.Name == "System data")
                    {
                        Logger.log_debug("Downloading the block: " + block.Name);
                        block.Download(force ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk);
                        blocks.Add(block.Name);
                        break;

                    }
                    else
                    {
                        Logger.log_debug("Omitting the block: " + block.Name);
                    }
                } 
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                return null;
            }
            return blocks.ToArray();
        }

        public string[] downloadAllBlocks(string projectProgramName, bool force = false)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting!\n");
                return null;
            }

            List<string> blocks = new List<string>();

            try
            {
                if (force)
                {
                    simaticapi.enableUnattendedServerMode();
                }
                else
                {
                    simaticapi.disableUnattendedServerMode();
                }

                foreach (S7Block block in simaticProject.Programs[projectProgramName].Next["Blocks"].Next)
                {
                    if (block.Name != "System data")
                    {
                        Logger.log_debug("Downloading the block: " + block.Name);
                        block.Download(force ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk);
                        blocks.Add(block.Name);
                    }
                    else
                    {
                        Logger.log_debug("Omitting the block: " + block.Name);
                    }
                }
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                return null;
            }
            return blocks.ToArray();
        }

        public bool startCPU(string projectProgramName)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting!\n");
                return false;
            }
            try
            {
                simaticProject.Programs[projectProgramName].NewStart();
            }
            catch (SystemException exc)
            {
                Logger.log_error(exc.Message);
                return false;
            }
            return true;
        }

        public bool stopCPU(string projectProgramName)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting!\n");
                return false;
            }
            try
            {
                simaticProject.Programs[projectProgramName].Stop();
            }
            catch (SystemException exc)
            {
                Logger.log_error(exc.Message);
                return false;
            }
            return true;
        }

        private void updatePrograms()
        {
            // assuming this is initialized constructing the objects
            // and the list of programs in the project does not change while executing s7cli
            if (this.programs != null)
                return;

            // create new dictionary with the list of programs indexed by name (to avoid accessing COM to retrieve it)
            this.programs = new Dictionary<string, SimaticProgram>();
            foreach (S7Program program in this.simaticProject.Programs)
            {
                if (program.Type != S7ProgramType.S7)
                    continue;
                Logger.log_debug("Found program: " + program.Name + " type: " + program.Type);
                if ( this.programs.Keys.Contains( program.Name ) )
                {
                    Logger.log_error("Error: multiple programs named '" + program.Name + "' in the project - aborting...");
                    Environment.Exit(1);
                }
                SimaticProgram prg = new SimaticProgram(program.Name, program);
                this.programs.Add(program.Name, prg);
            }
        }

        public string [] getListOfAvailablePrograms()
        {
            //string [] availablePrograms;
            List<string> availablePrograms = new List<string>();
/*            foreach (IS7Program program in this.simaticProject.Programs)     {
                availablePrograms.Add (program.Name);
            }
 */
            foreach (string programName in this.programs.Keys)
            {
                availablePrograms.Add (programName);
            }
            return availablePrograms.ToArray();
        }


        public int importSymbols(string symbolsPath, string programName = "S7 Program(1)")
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting import!\n");
                return 0;
            }
            return this.programs[programName].importSymbols(symbolsPath);
        }


        /* returned value
         * 0    - success
         * != 0 - failure
         */
        public int exportSymbols(string symbolsOutputFile, string programName = "S7 Program(1)")
        {
            if (!isProjectOpened()) {
                Logger.log_debug("Error: exportSymbols() called while the project is not opened! Aborting export!\n");
                return -1;
            }
            return this.programs[programName].exportSymbols(symbolsOutputFile);
        }


        public string[] getSourcesList(string programName)
        {
            return this.programs[programName].getSourcesList();
        }


        private bool sourceExists(string programName, string sourceName)
        {
            return this.programs[programName].sourceExists(sourceName);
        }


        public IS7SWItem addSource(string programName, string filenameFullPath, bool forceOverwrite = false)
        {
            return this.programs[programName].addSource(filenameFullPath, forceOverwrite);
        }


        public void removeSource (string programName, string sourceName){
            this.programs[programName].removeSource(sourceName);
        }


        public bool importLibSources(string libProjectName, string libProjectProgramName, 
                                     string destinationProjectProgramName, bool forceOverwrite = false)
        {
            /*
            if (simaticProject == null) {
                Logger.log_debug("Error: Project variable \"simaticProject\" not initialized! Aborting import!\n");
                return false;
            } */
            //Simatic libSimatic = new Simatic();
            Simatic libSimatic = this.simaticapi.getSimatic();

            Logger.log("\nCopying sources from: " + libProjectName + " / " + libProjectProgramName +
                     "\n                  to: " + this.getS7ProjectName() + " / " + destinationProjectProgramName);
            try {
                foreach (S7Source source in
                    libSimatic.Projects[libProjectName].Programs[libProjectProgramName].Next["Sources"].Next) {
                        Logger.log("\nCopying the source: " + source.Name);
                        if (this.sourceExists(destinationProjectProgramName, source.Name)) {
                            Logger.log("This source already exists in " + this.getS7ProjectName() + 
                                       " / " + destinationProjectProgramName);

                            if (forceOverwrite) {
                                Logger.log("Overwrite forced! (removing and copying the source)");
                                //continue;
                                this.removeSource(destinationProjectProgramName, source.Name);
                            } else {
                                Logger.log("Skipping the source!");
                                continue;
                            }
                        }
                        source.Copy(simaticProject.Programs[destinationProjectProgramName].Next["Sources"]);
                }
            } catch (SystemException exc) {
                Logger.log_error("Error: " + exc.Message + "\n");
                return false;
            }

            return true;
        }


        public bool blockExists(string programName, string blockName)
        {
            return this.programs[programName].blockExists(blockName);
        }

        public void removeBlock(string programName, string blockName){
            this.programs[programName].removeBlock(blockName);
        }


        public bool importLibBlocks(string libProjectName, string libProjectProgramName,
                                    string destinationProjectProgramName, bool forceOverwrite = false)
        {
            /*
            if (simaticProject == null) {
                Logger.log_debug("Error: Project variable \"simaticProject\" not initialized! Aborting import!\n");
                return false;
            } */
            //Simatic libSimatic = new Simatic();
            Simatic libSimatic = this.simaticapi.getSimatic();

            Logger.log("\nCopying blocks from: " + libProjectName + " / " + libProjectProgramName +
                     "\n                 to: " + this.getS7ProjectName() + " / " + destinationProjectProgramName);
            try {
                foreach (S7Block block in
                    libSimatic.Projects[libProjectName].Programs[libProjectProgramName].Next["Blocks"].Next) {
                        if (block.Name == "System data") {
                            // cannot copy system data!!!
                            // (Note: 'System Data' block do not have '.SymbolicName'! )
                            Logger.log("\nCannot copy 'System data' - skipping it!");
                            continue;
                        }
                        Logger.log("\nCopying the block: " + block.SymbolicName + " (" + block.Name + ")");

                        if (this.blockExists(destinationProjectProgramName, block.Name)) {
                            Logger.log("The block " + block.Name + " already exists in " + this.getS7ProjectName() + " / " + destinationProjectProgramName);
                            if (forceOverwrite) {
                                Logger.log("Overwrite forced! (removing and copying the block)");
                                //continue;
                                this.removeBlock(destinationProjectProgramName, block.Name);
                            } else {
                                Logger.log("Skipping the block!");
                                continue;
                            }
                        }

                        block.Copy(simaticProject.Programs[destinationProjectProgramName].Next["Blocks"]);                        
                }
            } catch (SystemException exc) {
                Logger.log_error("Error: " + exc.Message + "\n");
                return false;
            }
            return true;
        }


        /* compileSource()
         * 
         * return values:
         *    0 success
         *   <0 error
         *   -1 source not found
         *   -2 exception during compilation
         *    1 unknown
         */
        public int compileSource(string programName, string sourceName)
        {
            return this.programs[programName].compileSource(sourceName);
        }

        public string getSourceTypeString(string programName, string sourceName)
        {
            return this.programs[programName].getSourceTypeString(sourceName);
        }

        /*
         * returned value:
         * 0 - success
         * !0 - error
         */
        public int exportSource(string programName, string sourceName, string ExportFileName)
        {
            return this.programs[programName].exportSource( sourceName, ExportFileName );
        }

        public int exportProgramStructure(
            string programName, string ExportFileName, 
            bool ExportDuplicateCalls = true, int ColumnFlags = 0)
        {            // ExportProgramStructure(ByVal ExportFileName As String, ByVal ExportDuplicateCalls As Boolean, ByVal ColumnFlags As Long)
            if (simaticProject == null)
            {
                Logger.log_debug("exportProgramStructure(): Error: The project is not opened! Aborting operation!\n");
                return 1;
            }

            return this.programs[programName].exportProgramStructure(ExportFileName, ExportDuplicateCalls , ColumnFlags );
        }
    }
}
