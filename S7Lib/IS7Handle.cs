using System.Collections.Generic;

namespace S7Lib
{
    /// <summary>
    /// Provides functions to interact with Simatic
    /// </summary>
    /// <remarks>
    /// Some method arguments can be specified in more than one way, such as `project` and `program`.
    /// This is so users can conveniently refer to names they know are unique in their system or project.
    /// However, neither project names nor program names are enforced to be unique.
    /// For this reason, whenever possible, the STEP 7 project should be specified by the path to the .s7p file.
    /// Similarly, the program should be preferably specified by its logical path
    /// (`logPath = $"{station}\\{module}\\{program}"`) e.g. `"SIMATIC 300(1)\CPU 319-3 PN/DP\S7 Program"`.
    /// </remarks>
    public interface IS7Handle
    {
        /// <summary>
        /// Create new empty STEP 7 project
        /// </summary>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        void CreateProject(string projectName, string projectDir);

        /// <summary>
        /// Create new empty STEP 7 library
        /// </summary>
        /// <param name="projectName">Library name (max 8 characters)</param>
        /// <param name="projectDir">Path to library's parent directory</param>
        void CreateLibrary(string projectName, string projectDir);

        /// <summary>
        /// Registers existing STEP 7 project given the path to its .s7p file
        /// </summary>
        /// <param name="projectFilePath">Path to STEP 7 project .s7p file</param>
        void RegisterProject(string projectFilePath);

        /// <summary>
        /// Removes STEP 7 project and deletes all of its files
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        void RemoveProject(string project);

        /// <summary>
        /// Import source into a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="source">Path to source file</param>
        /// <param name="overwrite">Force overwrite existing source in project</param>
        void ImportSource(string project, string program, string source, bool overwrite = true);

        /// <summary>
        /// Import sources from a directory into a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="sourcesDir">Directory from which to import sources</param>
        /// <param name="overwrite">Force overwrite existing sources in project</param>
        void ImportSourcesDir(string project, string program, string sourcesDir, bool overwrite = true);

        /// <summary>
        /// Import sources from a library into a program
        /// </summary>
        /// <param name="project">Destination project id, path to .s7p (unique) or project name</param>
        /// <param name="program">Destination project S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="library">Source library id, path to .s7l (unique) or library name</param>
        /// <param name="libProgram">Source library S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        void ImportLibSources(string project, string program, string library, string libProgram, bool overwrite = true);

        /// <summary>
        /// Exports all sources from a program to a directory
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        void ExportAllSources(string project, string program, string sourcesDir);

        /// <summary>
        /// Exports a source from a program to a directory
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="source">Source name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        void ExportSource(string project, string program, string source, string sourcesDir);

        /// <summary>
        /// Creates a new empty S7 program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programName">Program name</param>
        void CreateProgram(string project, string programName);

        /// <summary>
        /// Compiles multiple source, in order
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="sources">Ordered list of source names</param>
        void CompileSources(string project, string program, List<string> sources);

        /// <summary>
        /// Import blocks from a directory into a project
        /// </summary>
        /// <param name="project">Destination project id, path to .s7p (unique) or project name</param>
        /// <param name="program">Destination project S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="library">Source library id, path to .s7l (unique) or library name</param>
        /// <param name="libProgram">Source library S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        void ImportLibBlocks(string project, string program, string library, string libProgram, bool overwrite = true);

        /// <summary>
        /// Imports symbols into a program from a file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="symbolFile">Path to input symbol table file (usually .sdf)
        ///     Supported extensions .asc, .dif, .sdf, .seq
        /// </param>
        /// <param name="overwrite">Whether to overwrite symbols if present</param>
        /// <param name="nameLeading">
        /// When overwrite is selected, defines whether symbol names or addresses are replaced as follows:
        /// - false: entries with the same symbol address are replaced. Symbol names are adjusted to the specifications in the import file.
        /// - true: entries with the same symbol name are replaced. The addresses are adjusted according to the specifications in the import file.
        /// </param>
        /// <param name="allowConflicts">If false, an exception is raised if a conflict is detected</param>
        void ImportSymbols(string project, string program, string symbolFile, bool overwrite = false,
            bool nameLeading = false, bool allowConflicts = false);

        /// <summary>
        /// Exports symbols from program from into a file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="symbolFile">Path to output symbol table file (usually .sdf)
        ///     Supported extensions .asc, .dif, .sdf, .seq
        /// </param>
        /// <param name="overwrite">Overwrite output file if it exists</param>
        void ExportSymbols(string project, string program, string symbolFile, bool overwrite = false);

        /// <summary>
        /// Exports the hardware configuration of a target station
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Name of target station to export</param>
        /// <param name="exportFile">Path to output export file (generally .cfg file)</param>
        void ExportStation(string project, string station, string exportFile);

        /// <summary>
        /// Creates a new S7 connection
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Name of parent station</param>
        /// <param name="rack">Name of parent rack</param>
        /// <param name="cpu">Name of the CPU</param>
        /// <param name="partnerName">Name of the partner, connection identifier</param>
        /// <param name="isActive">Whether to establish an active connection</param>
        /// <param name="partnerAddress">Partner IP address</param>
        /// <param name="localConnResource">Local connection resource</param>
        /// <param name="partnerRack">Partner rack</param>
        /// <param name="partnerSlot">Partner slot</param>
        /// <param name="partnerConnResource">Partner connection resource</param>
        void CreateConnection(string project, string station, string rack, string cpu,
                                    string partnerName, bool isActive, string partnerAddress,
                                    string localConnResource, int partnerRack, int partnerSlot, string partnerConnResource);

        /// <summary>
        /// Edit properties of target S7 connection
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Name of parent station</param>
        /// <param name="rack">Name of parent rack</param>
        /// <param name="cpu">Name of the CPU</param>
        /// <param name="partnerName">Name of the partner, connection identifier</param>
        /// <param name="properties">Connection properties as key-value pairs, e.g.
        /// {"IsActive", false}
        /// {"PartnerAddress", "127.0.0.1"}
        /// {"LocalConnRes", "10"}
        /// {"PartnerRack", 0}
        /// {"PartnerSlot", 0}
        /// {"PartnerConnRes", "A0"}
        /// </param>
        void EditConnection(string project, string station, string rack, string cpu,
                                    string partnerName, Dictionary<string, object> properties);

        /// <summary>
        /// Edit properties of target module
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Name of parent station</param>
        /// <param name="rack">Name of parent rack</param>
        /// <param name="modulePath">Path to targt module</param>
        /// <param name="properties">Module properties as key-value pairs, e.g.
        /// {"IPAddress", "127.0.0.1"}
        /// {"SubnetMask", "255.255.255.192"}
        /// {"RouterAdress", "127.0.0.2"}
        /// {"MACAddress", "080006010000"}
        /// {"IPActive", true}
        /// {"RouterActive", false}
        /// </param>
        void EditModule(string project, string station, string rack, string modulePath, Dictionary<string, object> properties);

        /// <summary>
        /// Compiles the HW configuration for each of the stations in a project
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="allowFail">If false, an exception is thrown if a station fials to compile</param>
        void CompileAllStations(string project, bool allowFail = true);

        /// <summary>
        /// Compile source
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="sourceName">Source name</param>
        void CompileSource(string project, string program, string sourceName);

        /// <summary>
        /// Returns dictionary with {projectDir, projectName} key-value pairs
        /// </summary>
        Dictionary<string, string> ListProjects();

        /// <summary>
        /// Returns list with the names of every program in a given project
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="json">Whether to produce JSON output</param>
        List<string> ListPrograms(string project, bool json = false);

        /// <summary>
        /// Returns list with the names of every station in a given project
        /// </summary>
        List<string> ListStations(string project);

        /// <summary>
        /// Returns list with the names of every module in a given project
        /// </summary>
        List<string> ListModules(string project);

        /// <summary>
        /// Returns list with the names of every container in a given project
        /// </summary>
        List<string> ListContainers(string project);

        /// <summary>
        /// Downloads all the blocks under a program to an online CPU
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        /// <param name="overwrite">Force overwrite of online blocks</param>
        void DownloadProgramBlocks(string project, string program, bool overwrite);

        /// <summary>
        /// Starts/restarts a program in the online CPU
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        void StartProgram(string project, string program);

        /// <summary>
        /// Stops a program in the online CPU
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        void StopProgram(string project, string program);

        /// <summary>
        /// Removes user blocks under a program in the online CPU
        /// </summary>
        /// <remarks>
        /// Skips system blocks, such as SFC, SFB, SDB and SDBs.
        /// </remarks>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program specified by its name or logical path (excluding project name)</param>
        void RemoveProgramOnlineBlocks(string project, string program);
    }
}
