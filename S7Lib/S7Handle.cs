using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Globalization;

using SimaticLib;
using S7HCOM_XLib;
using Serilog;
using Serilog.Core;


namespace S7Lib
{
    /// <summary>
    /// Handle for S7Lib functions
    /// </summary>
    public sealed class S7Handle : IDisposable
    {
        /// <summary>
        /// Handle for the Simatic API
        /// </summary>
        public readonly Simatic Api;
        /// <summary>
        /// Handle for Serilog logger
        /// </summary>
        public readonly Logger Log;

        // TODO: Review constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Regarding automaticSave, as per official documentation:
        /// If enabled, the changes are saved immediately, especially for all operations that change structure
        /// (for methods such as Add, Copy, or Remove, with which objects are added or deleted)
        /// as well as for name changes.
        /// </remarks>
        /// <param name="log">Configured logger object</param>
        /// <param name="serverMode">UnattandedServerMode surpress GUI messages</param>
        /// <param name="automaticSave">Save project automatically</param>
        public S7Handle(Logger log = null, bool serverMode = true, bool automaticSave = true)
        {
            Api = new Simatic
            {
                UnattendedServerMode = serverMode,
                AutomaticSave = automaticSave ? 1 : 0
            };

            if (log == null)
                log = CreateConsoleLogger();
            Log = log;
        }
        public void Dispose()
        {
            // Cleanup Simatic, close project?
            Log?.Dispose();
        }

        // Simple default console logger for testing purposes
        private Logger CreateConsoleLogger()
        {
            return new LoggerConfiguration().MinimumLevel.Debug()
               .WriteTo.Console().CreateLogger();
        }

        private bool ProjectExists(string projectName)
        {
            try
            {
                var project = Api.Projects[projectName];
                return true;
            }
            catch (System.Runtime.InteropServices.COMException) { }
            return false;
        }

        /// <summary>
        /// Obtains a project object, from the path to its .s7p project file or its name
        /// </summary>
        /// <remarks>
        /// Project names are not guaranteed to be unique.
        /// However, the path to .s7p project file is, and is therefore preferred as a projectId
        /// </remarks>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <returns>S7Project object on success, null otherwise</returns>
        public S7Project GetProject(string project)
        {
            S7Project projectObj;
            try
            {
                // Detect if a path was provided
                if (Path.HasExtension(project))
                {
                    projectObj = (S7Project)Api.Projects.Add(Name: project);
                }
                else
                {
                    projectObj = (S7Project)Api.Projects[project];
                }
            }
            catch (Exception exc)
            {
                throw new KeyNotFoundException($"Could not get project {project}", exc);
            }

            return projectObj;
        }

        /// <summary>
        /// Obtain S7 program from its logPath property
        /// </summary>
        /// <remarks>
        /// Identifies a program for its logical path property, e.g.
        /// {logPath}={project}\{station}\{module}\{program}
        /// </remarks>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Logical path to program (not including project name)</param>
        /// <returns>S7Program on success, null otherwise</returns>
        public S7Program GetProgram(string project, string programPath)
        {
            var projectObj = GetProject(project);
            return GetProgramImpl(projectObj, programPath);
        }

        private S7Program GetProgramImpl(S7Project project, string programPath)
        {
            var logPath = $"{project.Name}\\{programPath}";
            S7Program programObj;
            foreach (IS7Program p in project.Programs)
            {
                if (p.LogPath == logPath)
                {
                    programObj = (S7Program)p;
                    return programObj;
                }
            }
            throw new KeyNotFoundException($"Could not find program in {logPath}");
        }

        private IS7Station GetStationImpl(S7Project project, string station)
        {
            IS7Station stationObj;
            try
            {
                stationObj = project.Stations[station];
            }
            catch (Exception exc)
            {
                throw new KeyNotFoundException($"Could not find station {station}", exc);
            }
            return stationObj;
        }

        private S7Rack GetRackImpl(IS7Station station, string rack)
        {
            S7Rack rackObj = null;
            try
            {
                rackObj = station.Racks[rack];
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not find rack {rack}");
            }
            return rackObj;
        }

        private IS7Module6 GetModuleImpl(S7Modules modules, string modulePath)
        {
            var split = modulePath.Split('\\');
            var childModules = modules;
            IS7Module6 moduleObj = null;

            foreach (var part in split)
            {
                try
                {
                    foreach (IS7Module6 child in childModules)
                    {
                        if (part == child.Name)
                        {
                            moduleObj = child;
                            break;
                        }
                    }
                    childModules = moduleObj.Modules;
                }
                catch (Exception exc)
                {
                    throw new KeyNotFoundException($"Could not find module {modulePath}", exc);
                }
            }
            return moduleObj;
        }

        /// <summary>
        /// Internal function to create STEP 7 project or library
        /// </summary>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        /// <param name="projectType">Project type (S7Project or S7Library)</param>
        private void CreateProjectImpl(string projectName, string projectDir, S7ProjectType projectType)
        {
            if (projectName.Length > 8)
            {
                Log.Error($"Could not create project {projectName} in {projectDir}");
                throw new ArgumentException($"Invalid project name {projectName}: has more than 8 characters.", nameof(projectName));
            }

            if (ProjectExists(projectName))
            {
                // Otherwise Add spawns a blocking GUI error message
                Log.Error($"Could not create project {projectName} in {projectDir}");
                throw new ArgumentException($"Project with name {projectName} already exists");
            }

            try
            {
                Api.Projects.Add(Name: projectName, ProjectRootDir: projectDir, Type: projectType);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not create project {projectName} in {projectDir}");
                throw;
            }

            Log.Debug($"Created empty project {projectName} in {projectDir}");
        }

        /// <summary>
        /// Create new empty STEP 7 project
        /// </summary>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        public void CreateProject(string projectName, string projectDir)
        {
            CreateProjectImpl(projectName, projectDir, S7ProjectType.S7Project);
        }

        /// <summary>
        /// Create new empty STEP 7 library
        /// </summary>
        /// <param name="projectName">Library name (max 8 characters)</param>
        /// <param name="projectDir">Path to library's parent directory</param>
        public void CreateLibrary(string projectName, string projectDir)
        {
            CreateProjectImpl(projectName, projectDir, S7ProjectType.S7Library);
        }

        /// <summary>
        /// Registers existing STEP 7 project given the path to its .s7p file
        /// </summary>
        /// <param name="projectFilePath">Path to STEP 7 project .s7p file</param>
        public void RegisterProject(string projectFilePath)
        {
            try
            {
                Api.Projects.Add(Name: projectFilePath);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not register existing project in {projectFilePath}");
                throw;
            }

            Log.Debug($"Registered existing project from {projectFilePath}");
        }

        /// <summary>
        /// Removes STEP 7 project and deletes all of its files
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        public void RemoveProject(string project)
        {
            var projectObj = GetProject(project);

            try
            {
                projectObj.Remove();
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not remove project {project}");
                throw;
            }

            Log.Debug($"Removed project {project}");
        }

        private List<string> GetSourcesFromDir(string sourcesDir)
        {
            var sourceFiles = new List<string>();
            string[] supportedExtensions = { "*.SCL", "*.AWL", "*.INP", "*.GR7" };
            foreach (string ext in supportedExtensions)
                sourceFiles.AddRange(
                    Directory.GetFiles(sourcesDir, ext,
                        SearchOption.TopDirectoryOnly));
            return sourceFiles;
        }

        /// <summary>
        /// Import source into a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Path to source file</param>
        /// <param name="overwrite">Force overwrite existing source in project</param>
        public void ImportSource(string project, string program, string source, bool overwrite = true)
        {
            var projectObj = GetProject(project);
            var container = S7ProgramSource.GetSources(this, projectObj, program);

            S7ProgramSource.ImportSource(this, container: container, sourceFilePath: source, overwrite: overwrite);

            Log.Debug($"Imported {source} to {project}:{program}");
        }

        /// <summary>
        /// Import sources from a directory into a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory from which to import sources</param>
        /// <param name="overwrite">Force overwrite existing sources in project</param>
        public void ImportSourcesDir(string project, string program, string sourcesDir, bool overwrite = true)
        {
            var sourceFiles = GetSourcesFromDir(sourcesDir);
            var projectObj = GetProject(project);
            var container = S7ProgramSource.GetSources(this, projectObj, program);

            foreach (var source in sourceFiles)
            {
                S7ProgramSource.ImportSource(this, container: container, sourceFilePath: source, overwrite: overwrite);
            }

            Log.Debug($"Imported sources to {project}:{program} for {sourcesDir}");
        }

        /// <summary>
        /// Import sources from a library into a program
        /// </summary>
        /// <param name="library">Source library id, path to .s7l (unique) or library name</param>
        /// <param name="project">Destination project id, path to .s7p (unique) or project name</param>
        /// <param name="libProgram">Source library program name</param>
        /// <param name="projProgram">Destination program name</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        public void ImportLibSources(string library, string libProgram, string project, string projProgram, bool overwrite = true)
        {
            var projectObj = GetProject(project);
            var libraryObj = GetProject(library);
            var libraryContainer = S7ProgramSource.GetSources(this, libraryObj, libProgram);
            var projectContainer = S7ProgramSource.GetSources(this, projectObj, projProgram);

            S7ProgramSource.ImportLibSources(this, libSources: libraryContainer, projSources: projectContainer, overwrite);

            Log.Debug($"Imported sources from {library}:{libProgram} into {project}:{projProgram}");
        }

        /// <summary>
        /// Exports all sources from a program to a directory
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        public void ExportAllSources(string project, string program, string sourcesDir)
        {
            var projectObj = GetProject(project);
            var projectSources = S7ProgramSource.GetSources(this, projectObj, program);

            S7ProgramSource.ExportSources(this, projectSources, sourcesDir);

            Log.Debug($"Exported {projectSources.Next.Count} sources to {sourcesDir}");
        }

        /// <summary>
        /// Exports a source from a program to a directory
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        public void ExportSource(string project, string program, string source, string sourcesDir)
        {
            var projectObj = GetProject(project);
            var sourceObj = S7ProgramSource.GetSource(this, projectObj, program, source);

            S7ProgramSource.ExportSource(this, sourceObj, sourcesDir);

            Log.Debug($"Exported {source} to {sourcesDir}");
        }

        /// <summary>
        /// Creates a new empty S7 program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programName">Program name</param>
        public void CreateProgram(string project, string programName)
        {
            var projectObj = GetProject(project);

            try
            {
                projectObj.Programs.Add(programName, Type: S7ProgramType.S7);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not create S7 program {programName} in {project}");
                throw;
            }

            Log.Debug($"Created S7 program {programName} in {project}");
        }

        /// <summary>
        /// Compile source
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        public void CompileSource(string project, string program, string source)
        {
            S7ProgramSource.CompileSource(this, project, program, source);
        }

        /// <summary>
        /// Compiles multiple source, in order
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sources">Ordered list of source names</param>
        public void CompileSources(string project, string program, List<string> sources)
        {
            foreach (var source in sources)
            {
                S7ProgramSource.CompileSource(this, project, program, source);
            }
        }

        /// <summary>
        /// Import blocks from a directory into a project
        /// </summary>
        /// <param name="library">Source library id, path to .s7l (unique) or library name</param>
        /// <param name="project">Destination project id, path to .s7p (unique) or project name</param>
        /// <param name="libProgram">Source library program name</param>
        /// <param name="projProgram">Destination program name</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        public void ImportLibBlocks(string library, string libProgram, string project, string projProgram, bool overwrite = true)
        {
            var libraryObj = GetProject(library);
            var projectObj = GetProject(project);
            var libraryBlocks = S7ProgramSource.GetBlocks(this, libraryObj, libProgram);
            var projectBlocks = S7ProgramSource.GetBlocks(this, projectObj, projProgram);

            S7ProgramSource.ImportLibBlocks(this, libBlocks: libraryBlocks, projBlocks: projectBlocks, overwrite);

            Log.Debug($"Imported blocks from {library}:{libProgram} into {project}:{projProgram}");
        }

        /// <summary>
        /// Imports symbols into a program from a file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Logical path to program (not including project name)</param>
        /// <param name="symbolFile">Path to input symbol table file (usually .sdf)
        ///     Supported extensions .asc, .dif, .sdf, .seq
        /// </param>
        /// <param name="flag">Symbol import flag (S7SymImportFlags)
        ///     - 0, S7SymImportInsert - Symbols are imported even if present, which may lead to ambiguities
        ///     - 1, S7SymImportOverwriteNameLeading - existing values with the same name are replaced. 
        ///         The addresses are adjusted according to the specifications in the import file.
        ///     - 2, S7SymImportOverwriteOperandLeading - existing values with identical addresses are replaced.
        ///         Symbol names are adjusted to the specifications in the import file.
        /// </param>
        /// <param name="allowConflicts">If false, an exception is raised if a conflict is detected</param>
        public void ImportSymbols(string project, string programPath, string symbolFile, int flag = 0, bool allowConflicts = false)
        {
            // TODO: Check if symbol table file exists?
            S7Symbols.ImportSymbols(this, project, programPath, symbolFile, flag, allowConflicts);
        }

        /// <summary>
        /// Exports symbols from program from into a file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Logical path to program (not including project name)</param>
        /// <param name="symbolFile">Path to output symbol table file (usually .sdf)
        ///     Supported extensions .asc, .dif, .sdf, .seq
        /// </param>
        /// <param name="overwrite">Overwrite output file if it exists</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public void ExportSymbols(string project, string programPath, string symbolFile, bool overwrite = false)
        {

            string exportDir = Path.GetDirectoryName(symbolFile);
            if (!Directory.Exists(exportDir))
            {
                Log.Error($"Could not export symbols from {project}:{programPath}");
                throw new IOException($"Output directory does not exist {exportDir}");
            }

            // TODO: Ensure output has supported extension?

            if (File.Exists(symbolFile) && !overwrite)
            {
                Log.Error($"Could not export symbols from {project}:{programPath}");
                throw new IOException($"Output file already exists {symbolFile}");
            }
            else if (File.Exists(symbolFile))
            {
                Log.Information($"Overwriting {symbolFile}");
            }

            S7Symbols.ExportSymbols(this, project, programPath, symbolFile);
        }

        /// <summary>
        /// Exports the hardware configuration of a target station
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Name of target station to export</param>
        /// <param name="exportFile">Path to output export file (generally .cfg file)</param>
        public void ExportStation(string project, string station, string exportFile)
        {
            var projectObj = GetProject(project);

            S7Station6 stationObj;
            try
            {
                stationObj = projectObj.Stations[station];
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not access station {station} in project {project}");
                throw;
            }
            try
            {
                stationObj.Export(exportFile);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not export station {station} to {exportFile}");
                throw;
            }
        }

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
        public void EditModule(string project, string station, string rack, string modulePath, Dictionary<string, object> properties)
        {
            var projectObj = GetProject(project);
            var stationObj = GetStationImpl(projectObj, station);
            var rackObj = GetRackImpl(stationObj, rack);
            var moduleObj = GetModuleImpl(rackObj.Modules, modulePath);

            SetModuleProperties(moduleObj, properties);
        }

        private void SetModuleProperties(IS7Module6 module, Dictionary<string, object> properties)
        {
            foreach (var kvPair in properties)
            {
                try
                {
                    SetModuleProperty(module, kvPair.Key, kvPair.Value);
                    Log.Debug($"Set {module.LogPath}.{kvPair.Key}={kvPair.Value}");
                }
                catch (Exception exc)
                {
                    Log.Error(exc, $"Could not set {module.LogPath}.{kvPair.Key}={kvPair.Value}");
                    throw;
                }
            }
        }

        private void SetModuleProperty(IS7Module6 module, string property, object value)
        {
            // TODO: Need to link/unlink subnetworks?
            switch (property)
            {
                case "IPAddress":
                    module.IPAddress = AddressToHex((string)value);
                    break;
                case "SubnetMask":
                    module.SubnetMask = AddressToHex((string)value);
                    break;
                case "RouterAddress":
                    module.RouterAddress = AddressToHex((string)value);
                    break;
                case "MACAddress":
                    module.MACAddress = (string)value;
                    break;
                case "IPActive":
                    module.IPActive = (bool)value ? 1 : 0;
                    break;
                case "RouterActive":
                    module.RouterActive = (bool)value ? 1 : 0;
                    break;
                default:
                    Log.Error($"Unknown module property {property}");
                    break;
            }
        }

        /// <summary>
        /// Returns IP address from hexadecimal string representation of address.
        /// Assumes big-endianness
        /// </summary>
        /// <param name="hex">Hex input string</param>
        /// <returns>IP Adress object</returns>
        private IPAddress HexToAddress(string hex)
        {
            string address = uint.Parse(hex, NumberStyles.AllowHexSpecifier).ToString();
            return IPAddress.Parse(address);
        }

        /// <summary>
        /// Returns hexadecimal string representation of IP Address.
        /// Assumes big-endianness
        /// </summary>
        /// <param name="address">Input IP Address string (e.g. "127.0.0.1")</param>
        /// <returns>Hex string with IP address</returns>
        private string AddressToHex(string address)
        {
            var ipAddress = IPAddress.Parse(address);
            byte[] bytes = ipAddress.GetAddressBytes();
            string hex = "";
            foreach (byte val in bytes) hex += $"{val:X2}";
            return hex;
        }

        /// <summary>
        /// Compiles the HW configuration for each of the stations in a project
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="allowFail">If false, an exception is thrown if a station fials to compile</param>
        public int CompileAllStations(string project, bool allowFail = true)
        {
            var projectObj = GetProject(project);

            IS7Stations stations;
            try
            {
                stations = projectObj.Stations;
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not access stations in project {project}");
                throw;
            }

            foreach (var station in stations)
            {
                var stationObj = (S7Station5)station;
                try
                {
                    stationObj.Compile();
                    Log.Debug($"Compiled HW config for {stationObj.Name}");
                }
                catch (Exception exc)
                {
                    Log.Error(exc, $"Could not compile HW config for {stationObj.Name}");
                    if (!allowFail) throw;
                }
            }

            return 0;
        }

        // List commands

        /// <summary>
        /// Returns dictionary with {projectDir, projectName} key-value pairs
        /// </summary>
        /// <param name="output">Dictionary with {projectDir, projectName} key-value pairs</param>
        public Dictionary<string, string> ListProjects()
        {
            Log.Information($"Listing registered projects");
            foreach (var project in Api.Projects)
            var output = new Dictionary<string, string>();
            {
                var projectObj = (S7Project)project;
                output.Add(projectObj.LogPath, projectObj.Name);
                Log.Information($"Project {projectObj.Name} Path {projectObj.LogPath}");
            }
            return output;
        }

        /// <summary>
        /// Returns list with programs in a given project
        /// </summary>
        public List<string> ListPrograms(string project)
        {
            var projectObj = GetProject(project);

            Log.Information($"Listing programs for project {project}");
            foreach (var program in projectObj.Programs)
            var output = new List<string>();
            {
                // TODO: If the cast to S7Program is safe, remove try catch block
                try
                {
                    var programObj = (S7Program)program;
                    output.Add(programObj.Name);
                    Log.Information($"Program {programObj.Name}");
                }
                catch (Exception exc)
                {
                    Log.Error(exc, $"Could not access program in project {project}");
                }
            }
            return output;
        }

        /// <summary>
        /// Returns list with stations in a given project
        /// Creates List with stations in a given project
        /// </summary>
        public List<string> ListStations(string project)
        {
            var projectObj = GetProject(project);

            Log.Information($"Listing stations for project {project}");
            foreach (var station in projectObj.Stations)
            var output = new List<string>();
            {
                // TODO: If the cast to S7Station is safe, remove try catch block
                try
                {
                    var stationObj = (S7Station)station;
                    output.Add(stationObj.Name);
                    Log.Information($"Station {stationObj.Name} ({stationObj.Type})");
                }
                catch (Exception exc)
                {
                    Log.Error(exc, $"Could not access station in project {project}");
                }
            }
            return output;
        }

        /// <summary>
        /// Creates List with containers for each program in a given project
        /// </summary>
        /// TODO: Maybe include program name in output as well?
        public List<string> ListContainers(string project)
        {
            var projectObj = GetProject(project);

            Log.Debug($"Listing containers for project {project}");
            foreach (var program in projectObj.Programs)
            var output = new List<string>();
            {
                S7Program programObj;
                // TODO: If the cast to S7Program is safe, remove try catch block
                try
                {
                    programObj = (S7Program)program;
                }
                catch (Exception exc)
                {
                    Log.Error(exc, $"Could not access program in project {project}");
                    continue;
                }

                Log.Information($"Listing containers for program {project}:{programObj.Name}");
                // TODO: If the cast to S7Container is safe, remove try catch block                
                foreach (var container in programObj.Next)
                {
                    S7Container containerObj;
                    try
                    {
                        containerObj = (S7Container)container;
                        output.Add(containerObj.Name);
                        Log.Information($"Container {containerObj.Name} ({containerObj.ConcreteType})");
                    }
                    catch (Exception exc)
                    {
                        Log.Error(exc, $"Could not access container in {programObj.Name}");
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// Downloads all the blocks under an S7Program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Station name</param>
        /// <param name="module">Parent module name</param>
        /// <param name="program">Program name</param>
        /// <param name="overwrite">Force overwrite of online blocks</param>
        public void DownloadProgramBlocks(string project, string station, string module, string program, bool overwrite)
        {
            S7Project projectObj = GetProject(project);
            S7Program programObj = GetProgram(project, $"{station}\\{module}\\{program}");

            var flag = overwrite ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk;

            try
            {
                var blocks = S7ProgramSource.GetBlocks(this, projectObj, programObj.Name);
                blocks.Download(flag);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not download blocks for {programObj.Name} {programObj.LogPath}");
                throw;
            }

            Log.Debug($"Downloaded blocks for {programObj.Name} {programObj.LogPath}");
        }

        /// <summary>
        /// Starts/restarts a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Station name</param>
        /// <param name="module">Parent module name</param>
        /// <param name="program">Program name</param>
        public void StartProgram(string project, string station, string module, string program)
        {
            S7Program programObj = GetProgram(project, $"{station}\\{module}\\{program}");

            try
            {
                if (programObj.ModuleState != S7ModState.S7Run)
                {
                    programObj.NewStart();
                }
                else
                {
                    Log.Debug($"{programObj.Name} is already in RUN mode. Restarting.");
                    programObj.Restart();
                }
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not start/restart {programObj.Name} {programObj.LogPath}");
                throw;
            }

            Log.Debug($"{programObj.Name} is in {programObj.ModuleState} mode");
        }

        /// <summary>
        /// Stops a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Station name</param>
        /// <param name="module">Parent module name</param>
        /// <param name="program">Program name</param>
        public void StopProgram(string project, string station, string module, string program)
        {
            S7Program programObj = GetProgram(project, $"{station}\\{module}\\{program}");

            try
            {
                if (programObj.ModuleState != S7ModState.S7Stop)
                    programObj.Stop();
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not stop {programObj.Name} {programObj.LogPath}");
                throw;
            }

            Log.Debug($"{programObj.Name} is in {programObj.ModuleState} mode");
        }
    }
}
