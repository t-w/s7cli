/************************************************************************
 * S7Project.cs - class with the main interface to S7project            *
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
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Net;
using System.Globalization;

using SimaticLib;
using S7HCOM_XLib;

namespace S7_cli
{


    //////////////////////////////////////////////////////////////////////////
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


    //////////////////////////////////////////////////////////////////////////
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
            simaticapi = SimaticAPI.Instance;
            Simatic simatic = simaticapi.getSimatic();

            simaticProject = simaticapi.getProject(pathOrName);

            if (simaticProject == null) {
                Logger.log_error("Project with the path or name: '" + pathOrName +
                    "' not found on the list of available projects!!!\n\nS7 projects found:\n");
                Logger.log( simaticapi.getListOfAvailableProjectsAsString() );
                return;
            }

            this.updatePrograms();
        }


        public S7Project(string projectName, string projectDirPath, S7ProjectType projectType)
        {
            simaticapi = SimaticAPI.Instance;
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
         * Private methods
         */

        // updates list of programs in the project (for all configured PLCs/CPUs!)
        // (normally called only by constructors while opening a project,
        // assuming that the s7cli does not add/remove programs )
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
                if (this.programs.Keys.Contains(program.Name))
                {
                    Logger.log_error("Error: multiple programs named '" + program.Name + "' in the project - aborting...");
                    Environment.Exit(1);
                }
                SimaticProgram prg = new SimaticProgram(program.Name, program);
                this.programs.Add(program.Name, prg);
            }
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

        private bool checkProjectOpened()
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
         * HW config methods
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

        /// <summary>
        /// Compiles all the connections
        /// </summary>
        /// <returns>0 on success, 1 otherwise</returns>
        public int compileAllConnections()
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting station compilation!\n");
                return 1;
            }

            IS7Stations stations = this.simaticProject.Stations;
            foreach (IS7Station station in stations)
            {
                S7Station5 station5 = (S7Station5)station;
                IS7Racks racks = station.Racks;
                foreach (IS7Rack rack in racks)
                {
                    IS7Modules modules = rack.Modules;
                    foreach (IS7Module module in modules)
                    {
                        S7Module6 module6 = (S7Module6)module;
                        IS7Conns connections = (IS7Conns)module6.Conns;
                        try
                        {
                            station5.CompileConns(connections);
                        }
                        catch (SystemException exc)
                        {
                            Logger.log_debug($"Could not compile connections for module {station.Name}.{rack.Name}.{module.Name}: {exc}");
                            //return 1;
                        }
                    }
                }

            }
            return 0;
        }

        /// <summary>
        /// Compiles all stations' HW configuration
        /// </summary>
        /// <returns></returns>
        public int compileAllStations()
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting station compilation!\n");
                return 1;
            }

            IS7Stations stations = this.simaticProject.Stations;
            foreach (IS7Station station in stations)
            {
                S7Station5 station5 = (S7Station5)station;
                try
                {
                    Logger.log_debug($"Attempting to compile station {station.Name}");
                    station5.Compile();
                }
                catch (SystemException exc)
                {
                    Logger.log_debug($"Could not compile station {station.Name}: {exc}");
                    //return 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Downloads all stations
        /// </summary>
        /// <returns></returns>
        public int downloadAllConnections()
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting station compilation!\n");
                return 1;
            }
            
            S7ConnType[] connectionTypes = { S7ConnType.S7_CONNECTION };

            IS7Stations stations = this.simaticProject.Stations;
            foreach (IS7Station station in stations)
            {
                S7Station5 station5 = (S7Station5)station;
                IS7Racks racks = station.Racks;
                foreach (IS7Rack rack in racks)
                {
                    IS7Modules modules = rack.Modules;
                    foreach (IS7Module module in modules)
                    {
                        IList<IS7Module> submodules = getListChildModules(module);
                        foreach (IS7Module submodule in submodules)
                        {
                            S7Module6 module6 = (S7Module6)submodule;
                            foreach (S7ConnType type in connectionTypes)
                            {
                                try
                                {
                                    if (module6.Conns.Count > 0)
                                    {
                                        Logger.log_debug($"Downloading {type} connections for module {station.Name}.{rack.Name}.{submodule.Name}...");
                                        module6.DownloadConnections(type);
                                    }
                                }
                                catch (SystemException exc)
                                {
                                    Logger.log_debug($"Error: {exc}");
                                }
                            }
                        }
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Exports station information to text files
        /// </summary>
        /// <param name="outputDir">Output directory</param>
        /// <returns>0 on success, 1 otherwise</returns>
        public int exportAllStations(string outputDir)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting station compilation!\n");
                return 1;
            }

            IS7Stations stations = this.simaticProject.Stations;
            foreach (IS7Station station in stations)
            {
                station.Export(Path.Combine(outputDir, station.Name + ".txt"));
            }
            return 0;
        }

        /*
         *  Blocks management
         */

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



        public bool downloadProgramBlockContainer(string projectProgramName)
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting!\n");
                return false;
            }

            Logger.log("Downloading program: " + projectProgramName);

            //simaticapi.enableUnattendedServerMode();

            foreach (S7Container container in simaticProject.Programs[projectProgramName].Next)
            {
                if (container.Name == "Blocks")
                {
                    Logger.log_debug("Found Blocks Container");
                    // Stop the CPU
                    // Check if already in stop

                    if (simaticProject.Programs[projectProgramName].ModuleState.Equals(S7ModState.S7Stop))
                    {
                        Logger.log("PLC already in STOP!");
                    }
                    else
                    {
                        Logger.log("Stopping the CPU!");
                        //add try catch
                        simaticProject.Programs[projectProgramName].Stop();
                    }

                    container.Download(S7OverwriteFlags.S7OverwriteAll);
                    //container.Download();
                    // Restart the CPU
                    Logger.log("Restarting the CPU!");
                    simaticProject.Programs[projectProgramName].NewStart();
                    return true;
                }
            } // else return false if we haven't found "Blocks"
            return false;
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

        /*
         * PLC CPU state management
         */

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


        /*
         * Program management
         */

        /// <summary>
        /// Returns array with names of all SIMATIC programs in project
        /// </summary>
        /// <returns>Names of all SIMATIC programs</returns>
        public string [] getListOfAvailablePrograms()
        {
            //string [] availablePrograms;
            List<string> availablePrograms = new List<string>();
            foreach (string programName in this.programs.Keys)
            {
                availablePrograms.Add(programName);
            }
            return availablePrograms.ToArray();
        }

        /// <summary>
        /// Returns array with names of all programs, including hidden i.e. non-SIMATIC programs in project
        /// </summary>
        /// <returns>Names of all programs</returns>
        public string[] getAllProgramNames()
        {
            List<string> allPrograms = new List<string>();
            foreach (IS7Program program in this.simaticProject.Programs)
            {
                allPrograms.Add(program.Name);
            }
            return allPrograms.ToArray();
        }

        /*
         * Station management
         */

        /// <summary>
        /// Attempts to start all programs in a station
        /// </summary>
        /// <param name="station">Target station</param>
        /// <returns>0 on success, 1 otherwise</returns>
        public int startStation(string station, string module)
        {
            IList<IS7Program> programs = getStationPrograms(parentStation: station, parentModule: module);
            IS7Station5 station5 = this.simaticProject.Stations[station];

            foreach (IS7Program program in programs)
            {
                try
                {
                    if (program.ModuleState != S7ModState.S7Run)
                    {
                        Logger.log_debug($"Attempting to start program {program.Module.Name}::{program.Name} ({program.ModuleState}) ...");
                        program.NewStart();
                    }
                    else
                    {
                        Logger.log_debug($"{program.Module.Name}::{program.Name} is already in {program.ModuleState} mode. Restarting...");
                        program.Restart();
                    }
                }
                catch (SystemException exc)
                {
                    Logger.log_error($"Could not start program {program.Module.Name}::{program.Name}: " + exc.Message);
                    return 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Attempts to stop all programs in a station
        /// </summary>
        /// <param name="station">Target station</param>
        /// <returns>0 on success, 1 otherwise</returns>
        public int stopStation(string station, string module)
        {
            IList<IS7Program> programs = getStationPrograms(parentStation: station, parentModule: module);
            foreach (IS7Program program in programs)
            {
                try
                {
                    if (program.ModuleState != S7ModState.S7Stop)
                    {
                        Logger.log_debug($"Attempting to stop program {program.Module.Name}::{program.Name} ({program.ModuleState}) ...");
                        program.Stop();
                    }
                    else
                    {
                        Logger.log_debug($"{program.Module.Name}::{program.Name} is already in {program.ModuleState} mode");
                    }
                }
                catch (SystemException exc)
                {
                    Logger.log_error($"Could not stop program {program.Module.Name}::{program.Name}: " + exc.Message);
                    return 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Attempts to donwload all programs to a station
        /// </summary>
        /// <param name="station">Target station</param>
        /// <returns>0 on success, 1 otherwise</returns>
        public int downloadStation(string station, string module, bool force = false)
        {
            IList<IS7Program> programs = getStationPrograms(parentStation: station, parentModule: module);

            foreach (IS7Program program in programs)
            {
                try
                {
                    program.Stop();
                }
                catch (Exception exc)
                {
                    Logger.log_error($"Can't stop {program.Module.Name}::{program.Name}: {exc}");
                }

                foreach (S7Container container in program.Next)
                {
                    try
                    {
                        // Only Block containers can be downloaded
                        if (container.ConcreteType == S7ContainerType.S7BlockContainer)
                        {
                            Logger.log_debug($"Attempting to download container {program.Module.Name}::{program.Name} - {container.Name}...");
                            container.Download(force ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk);
                        }
                    }
                    catch (SystemException exc)
                    {
                        Logger.log_error($"Could not download container {program.Module.Name}::{program.Name} - {container.Name}:" + exc.Message);
                        //return 1;
                    }
                }

                try
                {
                    program.NewStart();
                }
                catch (Exception exc)
                {
                    Logger.log_error($"Can't start {program.Module.Name}::{program.Name}: {exc}");
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns names of available containers
        /// </summary>
        /// <returns>Array og string containing the names of found containers</returns>
        public string [] getListOfAvailableContainers()
        {
            // 
            List<string> availableContainers = new List<string>();
            foreach ( string program in this.programs.Keys )
            {
                Logger.log_debug( "Program: " + program );
                foreach ( S7Container container in programs[ program ].getProgram().Next )
                {
                    Logger.log_debug( "Adding the container: " + container.Name );
                    availableContainers.Add(container.Name);
                }
            }
            return availableContainers.ToArray();
        }

        public string[] getListOfStations()
        {
            List<string> stations = new List<string>();
            foreach (IS7Station station in this.simaticProject.Stations)
            {
                stations.Add($"{station.Name} of type {station.Type.ToString()}");
            }
            return stations.ToArray();
        }

        /// <summary>
        /// Check if given program exists in the project
        /// </summary>
        /// <param name="programName"></param>
        /// <returns>True if program exists, false otherwise</returns>
        public bool programExists( string programName, bool showError = false )
        {
            bool exists = Array.Exists<string>( getListOfAvailablePrograms(),
                                                s => s.Equals( programName ) );
            if ( (! exists) && showError )
            {
                Logger.log_error("Program '" + programName + "' not found!\n");
            }

            return exists;
        }

        
        public int importSymbols(string symbolsPath, string programName = "S7 Program(1)")
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Project not opened - aborting the import!\n");
                return 0;
            }

            return this.programs[programName].importSymbols(symbolsPath);
        }

        /// <summary>
        /// Exports symbol table to file
        /// </summary>
        /// <param name="symbolsOutputFile">Path to output symbol table file</param>
        /// <param name="programName">Name of target program</param>
        /// <returns>0 on success, error code otherwise</returns>
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

        
        public IS7SWItem importSource(string programName, string filenameFullPath, bool forceOverwrite = false)
        {
            return this.programs[programName].importSource(filenameFullPath, forceOverwrite);
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

            return this.programs[destinationProjectProgramName].importLibSources(
                libSimatic.Projects[libProjectName].Programs[libProjectProgramName].Next["Sources"].Next,
                forceOverwrite);
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
         *    1 success
         *    0 warning during compilation
         *   <0 error
         *   -1 source not found
         *   -2 exception during compilation
         *   -3 error during compilation
         *   -4 S7 program not found
         *   -10 unknown
         */
        public int compileSource(string programName, string sourceName)
        {
            return this.programs[programName].compileSource(sourceName);
        }


        public string getCompilationStatusInfo(int status)
        {
            Dictionary<int, string> statusInfo= new Dictionary<int, string>(){
                {  1, "success" },
                {  0, "warning during compilation "},
                { -1, "source not found" },
                { -2, "exception during compilation"},
                { -3, "error during compilation"},
                { -4, "S7 program not found"},
                { -10, "unknown"}
            };

            return statusInfo[status];
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


        // Connection related commands

        /// <summary>
        /// Lists connection attributes
        /// </summary>
        /// <param name="conn">Connection object</param>
        /// <returns></returns>
        public int listConnectionAttributes(S7Conn conn)
        {
            String attrName;
            object attrVal;

            try
            {
                if (conn.GetFirstAttribute(out attrName, out attrVal) == 0)
                {
                    return 1;
                }
                do
                {
                    Logger.log_result($"Connection {conn.Name} Attribute[{attrName}]={attrVal}");
                }
                while (conn.GetNextAttribute(out attrName, out attrVal) > 0);
            }
            catch (SystemException exc)
            {
                Logger.log_error($"Cannot access attribute in {conn.Name}: {exc}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// List connection details 
        /// </summary>
        /// <returns>0 on success, 1 otherwise</returns>
        public int listConnections()
        {
            if (!checkProjectOpened())
            {
                Logger.log_error("Error: Project not opened - aborting station compilation!\n");
                return 1;
            }

            IList<IS7Conn> connections = getConnections();
            foreach (IS7Conn conn in connections)
            {
                S7Conn s7Conn = (S7Conn)conn;
                listConnectionAttributes(s7Conn);
            }
            return 0;

            //if (module6.IPAddress != null)
            //{
            //    IPAddress ip_addr = new IPAddress(long.Parse(module6.IPAddress, NumberStyles.AllowHexSpecifier));
            //    Logger.log_result($"Module {module6.Name} with IP {ip_addr}");
            //}
        }

        // Utilities for navigating project

        /// <summary>
        /// Returns list of all stations, or a set of stations filtered by name or type
        /// </summary>
        /// <param name="name">name of target station</param>
        /// <param name="type">Type of target stations</param>
        /// <param name="all">Whether to return all stations</param>
        /// <returns>List of stations</returns>
        public IList<IS7Station> getStations(string name = "", string type = "", bool all = false)
        {
            List<IS7Station> output = new List<IS7Station>();
            bool filterName = !string.IsNullOrEmpty(name);
            bool filterType = !string.IsNullOrEmpty(type);

            if (all)
            {
                foreach (IS7Station station in this.simaticProject.Stations)
                {
                    output.Add(station);
                }
            }
            else if (filterName)
            {
                try
                {
                    IS7Station station = this.simaticProject.Stations[name];
                    if (filterType && station.Type.ToString() == type || !filterType)
                        output.Add(station);
                }
                catch (SystemException exc)
                {
                    Logger.log_error($"Could not find station {name}: {exc}");
                }
            }
            else
            {
                foreach (IS7Station station in this.simaticProject.Stations)
                {
                    if (filterType && station.Type.ToString() != type)
                        continue;
                    output.Add(station);
                }
            }
            if (output.Count == 0)
            {
                Logger.log_warning($"No station found: name={name} type={type}");
            }
            return output;
        }

        /// <summary>
        /// Recursively obtains the list of child modules, given a root module
        /// </summary>
        /// <param name="parentModule"></param>
        /// <returns></returns>
        public IList<IS7Module> getListChildModules(IS7Module rootModule)
        {
            List<IS7Module> output = new List<IS7Module>();
            output.Add(rootModule);
            foreach (IS7Module module in rootModule.Modules)
            {
                output.AddRange(getListChildModules(module));
            }
            return output;
        }

        /// <summary>
        /// Returns list of all programs
        /// </summary>
        /// <returns>List with all programs</returns>
        public IList<IS7Program> getPrograms()
        {
            IList<IS7Program> output = new List<IS7Program>();
            S7Programs programs = this.simaticProject.Programs;
            foreach (IS7Program program in programs)
            {
                output.Add(program);
            }
            return output;
        }

        /// <summary>
        /// Returns array of program objects associated with project stations
        /// </summary>
        /// <note>
        /// Some additional programs may exist which cannot be acessed by stations,
        /// yet can still be obtained via `this.simaticProject.Programs`
        /// </note>
        /// <param name="parentStation">Name of program's parent station</param>
        /// <param name="parentModule">Name of program's parent module</param>
        /// <returns>List of program objects</returns>
        public IList<IS7Program> getStationPrograms(string parentStation = "", string parentModule = "")
        {
            IList<IS7Program> output = new List<IS7Program>();
            bool filterStation = !string.IsNullOrEmpty(parentStation);
            bool filterModule = !string.IsNullOrEmpty(parentModule);
            S7Programs programs = this.simaticProject.Programs;

            foreach (IS7Station station in simaticProject.Stations)
            {
                if (filterStation && station.Name != parentStation)
                    continue;
                foreach (IS7Rack rack in station.Racks)
                {
                    foreach (IS7Module rootModule in rack.Modules)
                    {
                        IList<IS7Module> childModules = getListChildModules(rootModule);
                        foreach (IS7Module module in childModules)
                        {
                            if (filterModule && module.Name != parentModule) continue;
                            try
                            {
                                IS7Program program = programs[module];
                                Logger.log_debug($"Added {station.Name}::{module.Name}::{program.Name} ({program.ModuleState})");
                                output.Insert(0, program);
                            }
                            catch (SystemException exc)
                            {
                                Logger.log_error($"Module {module.Name} has no child program: {exc}");
                            }
                        }
                    }
                }
            }
            if (output.Count == 0)
            {
                Logger.log_warning($"No program found: station={parentStation} module={parentModule}");
            }
            return output;
        }

        /// <summary>
        /// Returns list of modules that match given filters 
        /// </summary>
        /// <param name="name">Target module name</param>
        /// <param name="station">Parent station of target connection</param>
        /// <returns></returns>
        public IList<IS7Module> getModules(string name = "", string station = "")
        {
            IList<IS7Module> output = new List<IS7Module>();
            bool filterName = !string.IsNullOrEmpty(name);
            bool filterType = !string.IsNullOrEmpty(name);

            IList<IS7Station> stations = getStations(name: station);
            foreach (IS7Station s7Station in stations)
            {
                IS7Racks racks = s7Station.Racks;
                foreach (IS7Rack rack in racks)
                {
                    IS7Modules modules = rack.Modules;
                    List<IS7Module> allModules = new List<IS7Module>();
                    foreach (IS7Module s7Module in modules)
                    {
                        allModules.AddRange(getListChildModules(s7Module));
                    }
                    foreach (IS7Module s7module in allModules)
                    {
                        if (filterName && s7module.Name != name) continue;
                        output.Add(s7module);
                    }
                }
            }
            if (output.Count == 0)
            {
                Logger.log_warning($"No module found: name={name} station={station}");
            }
            return output;
        }

        /// <summary>
        /// Returns list of connections that match given filters 
        /// </summary>
        /// <param name="name">Target connection name</param>
        /// <param name="type">Type of target connection</param>
        /// <param name="module">Parent module of target connection</param>
        /// <param name="station">Parent station of target connection</param>
        /// <returns></returns>
        public IList<IS7Conn> getConnections(string name = "", string type = "", string module = "", string station = "")
        {
            IList<IS7Conn> output = new List<IS7Conn>();
            bool filterName = !string.IsNullOrEmpty(name);
            bool filterType = !string.IsNullOrEmpty(type);
            bool filterModule = !string.IsNullOrEmpty(module);

            IS7Stations stations = this.simaticProject.Stations;
            foreach (IS7Station s7Station in stations)
            {
                IS7Racks racks = s7Station.Racks;
                foreach (IS7Rack rack in racks)
                {
                    IS7Modules modules = rack.Modules;
                    List<IS7Module> allModules = new List<IS7Module>();
                    foreach (IS7Module s7Module in modules)
                    {
                        allModules.AddRange(getListChildModules(s7Module));
                    }
                    foreach (IS7Module s7module in allModules)
                    {
                        if (filterModule && s7module.Name != module) continue;

                        S7Module6 module6 = (S7Module6)s7module;
                        foreach (S7Conn conn in module6.Conns)
                        {
                            if (filterName && conn.Name.ToString() != name) continue;
                            if (filterType && conn.Type.ToString() != type) continue;
                            output.Add(conn);
                        }
                    }
                }
            }
            if (output.Count == 0)
            {
                Logger.log_warning($"No connection found: name={name} type={type} station={station} module={module}");
            }
            return output;
        }
    }
}
