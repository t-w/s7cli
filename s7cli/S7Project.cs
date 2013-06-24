using System;
using System.IO;
//using System.Runtime.InteropServices;
//using System.Windows.Automation;
using System.Collections.Generic;

using SimaticLib;

namespace S7_cli
{
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

    public class S7Project
    {
        private static Simatic simatic;
        private IS7Project simaticProject = null;

        public static string getListOfAvailableProjects()
        {
            simatic = new SimaticLib.Simatic();
            string availableProjects = "";
            foreach (IS7Project project in simatic.Projects)  {
                availableProjects += ("- " + project.Name + ", " + project.LogPath + "\n");
            }
            return availableProjects;
        }


        //public plcProject(string Name)
        public S7Project(string pathOrName)
        {
            simatic = new SimaticLib.Simatic();

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
                Logger.log_error("Project with path or name: " + pathOrName + 
                    " not found on list of available project!!!\n\nAvailable projects:\n");
                Logger.log_error(getListOfAvailableProjects());
            }
        }


        public S7Project(string projectName, string projectDirPath)
        {
            simatic = new SimaticLib.Simatic();

            // checking if the project dir path ends with "\"
            if (!projectDirPath.EndsWith("\\"))  {
                projectDirPath = projectDirPath + "\\";
            }

            // concatenating real project dir path
            string projectPath;

            if(projectName.Length > 8)  {
                projectPath = projectDirPath + projectName.Substring(0, 8);
            } else  {
                projectPath = projectDirPath + projectName;
            }

            // checking if directory path is not taken
            if (Directory.Exists(projectPath))  {
                System.Console.Write("Error: Cannot create project because folder " + projectPath + " already exists!\n");
                System.Console.Write("Error: Project not created! Exiting program!\n");
                Environment.Exit(1);
            } else {
                try {
                    simaticProject = simatic.Projects.Add(projectName, projectDirPath, S7ProjectType.S7Project);
                }  catch (SystemException exc)  {
                    System.Console.Write("Error in S7Project(): " + exc.Message + "\n");
                }
            }
        }

        public bool isProjectOpened()
        {
            if (this.simaticProject != null)
                return true;
            else
                return false;
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




        public bool importConfig(string projectConfigPath)
        {
            if (simaticProject == null)  {
                System.Console.Write("Error: Project variable \"simaticProject\" not initialized! Aborting import!\n");
                return false;
            } else  {
                try  {
                    simaticProject.Stations.Import(projectConfigPath);
                } catch (SystemException exc) {
                    System.Console.Write("Error: " + exc.Message + "\n");
                    return false;
                }
            }
            return true;
        }


        /**********************************************************************************
         * Program methods
         **********************************************************************************/
        public string [] getListOfAvailablePrograms()
        {
            //string [] availablePrograms;
            List<string> availablePrograms = new List<string>();
            foreach (IS7Program program in this.simaticProject.Programs)     {
                availablePrograms.Add (program.Name);
            }
            return availablePrograms.ToArray();
        }

        public IS7Program getProgram(string Name)
        {
            /*
             *  Notice: Name has to be UNIQUE for each application, for each module.
             *          safest way is to make names completely unique like "ARC56_program"
             */
            foreach (IS7Program program in simaticProject.Programs) {
                //System.Console.Write("Program name: " + program.Name + "\n");
                //System.Console.Write("Program LogPath: " + program.LogPath + "\n");
                if (//program.Parent == sPr)
                    program.Name == Name) {
                    //System.Console.Write("Program OK: " + program.Name + "\n");
                    return program;
                }
            }
            return null;
        }


        public IS7SymbolTable getSymbolTable(string programName)
        {
            try {
                return this.simaticProject.Programs[programName].SymbolTable;
            } catch (System.Exception exc) {
                System.Console.Write("\n** getSymbolTable(): Error accessing symbol table for program: '" +
                                     programName + "':\n" + exc.Message + "\n");
                return null;
            }
        }


        public S7SWItem getSoftwareItem(string programName, string itemName)
        {
            IS7Program prg = getProgram(programName);

            S7SWItems items = prg.Next;
            //Logger.log_debug("\nSoftware items count: " + items.Count + "\n");
            foreach (S7SWItem item in items)  {
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

        public S7SWItem getSources(string programName)
        {
            return getSoftwareItem(programName, "Sources");
        }

        public S7SWItem getBlocks(string programName)
        {
            return getSoftwareItem(programName, "Blocks");
        }


        public S7SWItems getSourceModules(string programName)
        {
            S7SWItem sources = getSources(programName);
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

        public string[] getSourcesList(string programName)
        {
            List<string> srcs = new List<string>();
            S7SWItems src_modules = this.getSourceModules(programName);
            if (src_modules == null)
                return null;   // failure
            foreach (S7SWItem src_module in getSourceModules(programName)) {
                //Logger.log_debug("\nsrc name: " + src_module.Name + "\n");
                srcs.Add(src_module.Name);
            }
            return srcs.ToArray();
        }

        public IS7SWItem getSourceModule(string programName, string sourceName)
        {
            /*
             foreach (S7SWItem src_module in getSourceModules(programName))   {
                //Logger.log_debug("\nsrc name: " + src_module.Name + "\n");
                if (src_module.Name == sourceName)
                    return src_module;
            }
             return null;*/
            try {
                IS7SWItem src_module = getSourceModules(programName)[sourceName];
                return src_module;
            } catch (System.Exception exc) {
                Logger.log_debug("\n** getSourceModule(): Error getting source '" + sourceName + "':\n" + exc.Message + "\n");
                return null;
            }
        }

        private bool sourceExists(string programName, string sourceName)
        {
            if (this.getSourceModule(programName, sourceName) != null)
                return true;
            else
                return false;
        }

        private IS7SWItem addSourceWithType(string programName, SimaticLib.S7SWObjType sourceType, 
                                            string filename, bool forceOverwrite = false)
        {
            S7SWItems src_modules = getSourceModules(programName);
            IS7SWItem item;

            string sourceName = System.IO.Path.GetFileNameWithoutExtension(filename);

            if (forceOverwrite && this.sourceExists(programName, sourceName)){
                Logger.log("Source '" + sourceName + "' already exists - removing it (overwriting forced!)...");
                src_modules.Remove(sourceName);
                Logger.log("... and importing new one.");
            }
            try  { 
                item = src_modules.Add(sourceName, sourceType, filename);
            } catch( System.Exception exc ) {
                Logger.log("\n** Error importing '" + filename + "':\n" + exc.Message + "\n");
                item = null;
            }
            return item;
        }

        public IS7SWItem addSource(string programName, string filenameFullPath, bool forceOverwrite = false)
        {
            string filename = System.IO.Path.GetFileName(filenameFullPath);
            string extension = System.IO.Path.GetExtension(filename);
            if (extension.ToLower() == ".scl" || extension.ToLower() == ".awl" || extension.ToLower() == ".inp")
            //if (Array.IndexOf ( string [] Array = {".scl", ".awl"}, extension.ToLower() > -1 )
                return addSourceWithType(programName, SimaticLib.S7SWObjType.S7Source, filenameFullPath, forceOverwrite);
            else {
                Logger.log("addSource(): Error - unknown source extension '" + extension + "' (file: " + filename + ")\n");
                return null;
            }
        }


        public int importSymbols(string symbolsPath, string programName = "S7 Program(1)")
        {
            if (simaticProject == null) {
                Logger.log_debug("Error: Project variable \"simaticProject\" not initialized! Aborting import!\n");
                return 0;
            } else if (!File.Exists(symbolsPath)) {
                System.Console.Write("Error: File " + symbolsPath + " does not exist! Aborting import!\n");
                return 0;
            } else {
                int nrOfSymbols = 0;

                try {
                    nrOfSymbols = simaticProject.Programs[programName].SymbolTable.Import(symbolsPath);
                } catch (SystemException exc) {
                    System.Console.Write("Error: " + exc.Message + "\n");
                }

                return nrOfSymbols;
            }
        }

        public bool importLibSources(string libProjectName, string libProjectProgramName, string destinationProjectProgramName)
        {
            if (simaticProject == null) {
                Logger.log_debug("Error: Project variable \"simaticProject\" not initialized! Aborting import!\n");
                return false;
            } 
            Simatic libSimatic = new Simatic();

            try {
                foreach (SimaticLib.S7Source source in
                    libSimatic.Projects[libProjectName].Programs[libProjectProgramName].Next["Sources"].Next) {
                    source.Copy(simaticProject.Programs[destinationProjectProgramName].Next["Sources"]);
                }
            } catch (SystemException exc) {
                Console.WriteLine("Error: " + exc.Message + "\n");
                return false;
            }
            return true;
        }

        public bool importLibBlocks(string libProjectName, string libProjectProgramName, string destinationProjectProgramName)
        {
            if (simaticProject == null) {
                Logger.log_debug("Error: Project variable \"simaticProject\" not initialized! Aborting import!\n");
                return false;
            }
            Simatic libSimatic = new Simatic();

            try {
                foreach (SimaticLib.S7Block block in
                    libSimatic.Projects[libProjectName].Programs[libProjectProgramName].Next["Blocks"].Next) {
                    block.Copy(simaticProject.Programs[destinationProjectProgramName].Next["Blocks"]);
                }
            } catch (SystemException exc) {
                Console.WriteLine("Error: " + exc.Message + "\n");
                return false;
            }
            return true;
        }


        public S7Source getS7SourceModule(string programName, string moduleName)
        {
            //S7SWItem item = getSourceModule(programName, moduleName);
            //SimaticLib.S7Source source = (SimaticLib.S7Source) item.Program.;

            //IS7Source src = getSources("ARC56_program").get_Child("test11"); //get_Collection("test11");

            foreach (S7Source src in simaticProject.Programs[programName].Next["Sources"].Next)
                if (src.Name == moduleName)
                    return src;
            return null;
        }

        /* compileSource()
         * 
         * return values:
         *    0 success
         *   -1 error
         *    1 unknown
         */
        public int compileSource(string programName, string sourceName)
        {
            S7Source src = getS7SourceModule(programName, sourceName);
            if (src == null)
                return -1;

            try {
                IS7SWItems items = src.Compile();  // this thing returns nothing useful -> bug in SimaticLib(!)
            } catch (System.Exception exc) {
                System.Console.Write("\n** Error compiling '" + sourceName + "':\n" + exc.Message + "\n");
                return -1;
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

        public int exportProgramStructure(
            string programName, string ExportFileName, 
            bool ExportDuplicateCalls = true, int ColumnFlags = 0)
        {            // ExportProgramStructure(ByVal ExportFileName As String, ByVal ExportDuplicateCalls As Boolean, ByVal ColumnFlags As Long)
            if (simaticProject == null)
            {
                Logger.log_debug("exportProgramStructure(): Error: Project not opened! Aborting operation!\n");
                return 1;
            }

            //S7Program program = (S7Program) this.getProgram(programName);
            S7Program program = (S7Program) simaticProject.Programs[programName];
            program.ExportProgramStructure(ExportFileName, ExportDuplicateCalls, ColumnFlags);
            return 0;

        }
    }
}