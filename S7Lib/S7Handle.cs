﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Serilog;
using Serilog.Core;

using SimaticLib;
using S7HCOM_XLib;
using Newtonsoft.Json;

namespace S7Lib
{
    /// <summary>
    /// Implements handle that provides functions to interact with Simatic
    /// </summary>
    /// <remarks>
    /// Some method arguments can be specified in more than one way, such as `project` and `program`.
    /// This is so users can conveniently refer to names they know are unique in their system or project.
    /// However, neither project names nor program names are enforced to be unique.
    /// For this reason, whenever possible, the STEP 7 project should be specified by the path to the .s7p file.
    /// Similarly, the program should be preferably specified by its logical path
    /// (`logPath = $"{station}\\{module}\\{program}"`) e.g. `"SIMATIC 300(1)\CPU 319-3 PN/DP\S7 Program"`.
    /// </remarks>
    public sealed class S7Handle : IS7Handle, IDisposable
    {
        #region Class Attributes, Ctor and Dispose

        /// <summary>
        /// Handle for the Simatic API
        /// </summary>
        private Simatic Api;
        /// <summary>
        /// Handle for Serilog logger
        /// </summary>
        public readonly ILogger Log;

        // Default path for symbol import report file
        private static readonly string ReportFilePath = @"C:\ProgramData\Siemens\Automation\Step7\S7Tmp\sym_imp.txt";
        // Default titles for Notepad window opened on import symbols command
        // The title window may or may not include the .txt extension depending on Explorer settings
        private static readonly string[] NotepadWindowTitles = new string[]{
            "sym_imp - Notepad", "sym_imp.txt - Notepad"
        };

        // Block types considered as system, or non-user blocks
        private static readonly HashSet<S7BlockType> SystemBlockTypes = new HashSet<S7BlockType>()
        {
            S7BlockType.S7SDB, S7BlockType.S7SDBs, S7BlockType.S7SFB, S7BlockType.S7SFC
        };

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Regarding automaticSave, as per official documentation:
        /// If enabled, the changes are saved immediately, especially for all operations that change structure
        /// (for methods such as Add, Copy, or Remove, with which objects are added or deleted)
        /// as well as for name changes.
        /// </remarks>
        /// <param name="logger">Configured logger object</param>
        /// <param name="serverMode">UnattandedServerMode surpress GUI messages</param>
        /// <param name="automaticSave">Save project automatically</param>
        public S7Handle(ILogger logger = null, bool serverMode = true, bool automaticSave = true)
        {
            Api = new Simatic
            {
                UnattendedServerMode = serverMode,
                AutomaticSave = automaticSave ? 1 : 0
            };

            if (logger == null)
                logger = CreateConsoleLogger();
            Log = logger;
        }
        public void Dispose()
        {
            if (Api != null)
            {
                // It's unsafe to use `Api.Close()`. As per the official documentation:
                //  "If a station or another hardware object is accessed during the process runtime,
                //  the Close method cannot be used."
                Marshal.ReleaseComObject(Api);
            }
        }

        // Simple default console logger for testing purposes
        private Logger CreateConsoleLogger()
        {
            return new LoggerConfiguration().MinimumLevel.Debug()
               .WriteTo.Console().CreateLogger();
        }

        #endregion

        #region Internal Get Helpers

        /// <summary>
        /// Obtains a project or library object, from the path to its .s7p/.s7l file or its name
        /// </summary>
        /// <remarks>
        /// Project and library names are not guaranteed to be unique.
        /// However, the path to .s7p/.s7l files is, and is therefore preferred as identifier.
        /// </remarks>
        /// <param name="project">Project or library identifier, path to .s7p or .s7l (unique) or project/library name</param>
        /// <returns>S7Project object on success</returns>
        private S7Project GetProject(string project)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var projects = wrapper.Add(() => Api.Projects);
                try
                {
                    // Detect if a path was provided
                    if (Path.HasExtension(project))
                    {
                        if (Path.GetExtension(project) == ".s7l")
                        {
                            return (S7Project)projects.Add(Name: project, Type: S7ProjectType.S7Library);
                        }
                        else if (Path.GetExtension(project) == ".s7p")
                        {
                            return (S7Project)projects.Add(Name: project);
                        }
                    }
                    return (S7Project)projects[project];
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not get project {project}. " +
                        $"Make sure that the path points to a .s7p or .s7l.", exc);
                }
            }
        }

        /// <summary>
        /// Obtain S7 program from either its name or its logical path
        /// </summary>
        /// <remarks>
        /// Identifies a program either by its name or its logPath property, e.g.
        /// {logPath}={project}\{station}\{module}\{program}.
        /// If several programs exist with the same name the first one will be returned.
        /// </remarks>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name or its logical path to program (not including project name)</param>
        /// <returns>S7Program on success, null otherwise</returns>
        private S7Program GetProgram(string project, string program)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                return GetProgramImpl(projectObj, program);
            }
        }

        private S7Program GetProgramImpl(S7Project project, string program)
        {
            S7Program programObj;

            using (var wrapper = new ReleaseWrapper())
            {
                var programs = project.Programs;
                wrapper.Add(() => programs);

                // Program name provided
                if (!program.Contains("\\"))
                {
                    try
                    {
                        programObj = (S7Program)programs[program];
                        return programObj;
                    }
                    catch (COMException exc)
                    {
                        throw new KeyNotFoundException($"Could not find program with name {program}.", exc);
                    }
                }

                // Program logPath provided 
                var logPath = $"{project.Name}\\{program}";
                foreach (IS7Program s7Program in programs)
                {
                    if (s7Program.LogPath == logPath)
                    {
                        programObj = (S7Program)s7Program;
                        Log.Debug("Found S7Program(Name={Name}, LogPath={LogPath}).",
                            s7Program.Name, s7Program.LogPath);
                        return programObj;
                    }
                    wrapper.Add(() => s7Program);
                }
            }
            throw new KeyNotFoundException($"Could not find program with path {program}.");
        }

        private static IS7Station GetStationImpl(S7Project project, string station)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var stations = project.Stations;
                    wrapper.Add(() => stations);
                    return stations[station];
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not find station {station}.", exc);
                }
            }
        }

        private static S7Rack GetRackImpl(IS7Station station, string rack)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var racks = station.Racks;
                    wrapper.Add(() => racks);
                    return racks[rack];
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not find rack {rack}.", exc);
                }
            }
        }

        private static IS7Module6 GetModuleImpl(S7Modules modules, string modulePath)
        {
            var split = modulePath.Split('\\');
            var childModules = modules;
            IS7Module6 moduleObj = null;

            using (var wrapper = new ReleaseWrapper())
            {
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
                            else
                            {
                                wrapper.Add(() => child);
                            }
                        }
                        childModules = moduleObj.Modules;
                        // TODO Debug edge cases
                        wrapper.Add(() => childModules);
                    }
                    catch (COMException exc)
                    {
                        throw new KeyNotFoundException($"Could not find module {modulePath}.", exc);
                    }
                }
            }
            return moduleObj;
        }

        private S7Container GetContainer(S7Project projectObj, string program, S7ContainerType type)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var programObj = wrapper.Add(() => GetProgramImpl(projectObj, program));
                var next = wrapper.Add(() => programObj.Next);
                foreach (S7Container container in next)
                {
                    if (container.ConcreteType == type)
                    {
                        return container;
                    }
                    wrapper.Add(() => container);
                }
            }
            throw new KeyNotFoundException($"Could not find container of type {type} in {projectObj.Name}\\{program}.");
        }

        private S7Container GetSources(S7Project projectObj, string program)
        {
            return GetContainer(projectObj, program, S7ContainerType.S7SourceContainer);
        }

        private S7Container GetBlocks(S7Project projectObj, string program)
        {
            return GetContainer(projectObj, program, S7ContainerType.S7BlockContainer);
        }

        /// <summary>
        /// Traverses modules recursively and appends their name to list
        /// </summary>
        /// <param name="moduleNames">Output list of module names</param>
        /// <param name="modules">Modules root</param>
        //
        private void GetModuleNames(List<string> moduleNames, IS7Modules modules)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                wrapper.Add(() => modules);
                foreach (var module in modules)
                {
                    var moduleObj = (IS7Module)wrapper.Add(() => module);
                    Log.Debug("Module {Name} Path={LogPath}", moduleObj.Name, moduleObj.LogPath);
                    moduleNames.Add(moduleObj.Name);
                    GetModuleNames(moduleNames, moduleObj.Modules);
                }
            }
        }

        /// <summary>
        /// Gets a S7Source object from a program
        /// </summary>
        /// <remarks>The source container may not be named "Sources"</remarks>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target S7 program</param>
        /// <param name="source">Source name</param>
        /// <returns>S7 Source</returns>
        private S7Source GetSource(string project, string program, string source)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var container = wrapper.Add(() => GetSources(projectObj, program));
                var swItems = wrapper.Add(() => container.Next);
                try
                {
                    var sourceObj = (S7Source)swItems[source];
                    return sourceObj;
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not find source {source} in {program}.", exc);
                }
            }
        }

        /// <summary>
        /// Gets a S7Block object from a program
        /// </summary>
        /// <remarks>The block container may not be named "Blocks"</remarks>
        /// <param name="projectObj">S7Project object</param>
        /// <param name="program">Target S7 program</param>
        /// <param name="block">Block name</param>
        /// <returns>S7 Block</returns>
        private S7Block GetBlock(S7Project projectObj, string program, string block)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var container = wrapper.Add(() => GetBlocks(projectObj, program));
                var swItems = wrapper.Add(() => container.Next);
                try
                {
                    var blockObj = (S7Block)swItems[block];
                    return blockObj;
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not find block {block} in {program}.", exc);
                }
            }
        }

        #endregion

        #region Internal helper methods

        private bool ProjectIsRegistered(string projectName)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var project = wrapper.Add(() => Api.Projects[projectName]);
                    Log.Debug("Found project {Name} in {Path}.", project.Name, project.LogPath);
                    return true;
                }
                catch (COMException)
                {
                    // Project does not exist
                    // Log.Debug($"Project {projectName} not found", exc);
                }
                return false;
            }
        }

        /// <summary>
        /// Internal function to create STEP 7 project or library
        /// </summary>
        /// <remarks>
        /// If the project name is longer than 8 characters it will be shortened.
        /// It's impossible to create projects with the same name in the same directory.
        /// However, it's possible to create them if they exist in different directories.
        /// </remarks>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        /// <param name="projectType">Project type (S7Project or S7Library)</param>
        private void CreateProjectImpl(string projectName, string projectDir, S7ProjectType projectType)
        {
            Log.Debug("Creating empty project {Name} in {Dir}.", projectName, projectDir);

            var projectRootDir = Path.Combine(projectDir, projectName);
            if (projectName.Length > 8)
            {
                Log.Warning("Provided project name {Name} is longer than 8 characters. " +
                    "The name of the parent directory and .s7p/.s7l file will be shortened.", projectName);
                projectRootDir = Path.Combine(projectDir, projectName.Substring(0, 8));
            }
            if (ProjectIsRegistered(projectName))
            {
                Log.Warning("Project with the name {Name} already exists.", projectName);
                if (Directory.Exists(projectRootDir))
                {
                    // Otherwise Projects.Add() spawns a blocking GUI error message
                    Log.Error("Could not create project {Name} in {Dir}.", projectName, projectDir);
                    throw new ArgumentException($"Project with name {projectName} already exists in {projectDir}.", nameof(projectName));
                }
            }

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var project = wrapper.Add(() => Api.Projects.Add(projectName, projectDir, projectType));
                    Log.Information("Created empty project {Name} in {LogPath}.", project.Name, project.LogPath);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not create project {Name} in {Dir}.", projectName, projectDir);
                    throw;
                }
            }
        }

        // Obtain source files from a given dir
        private List<string> GetSourcesFromDir(string sourcesDir)
        {
            var sourceFiles = new List<string>();
            string[] supportedExtensions = { "*.SCL", "*.AWL", "*.INP", "*.GR7" };
            foreach (string ext in supportedExtensions)
                sourceFiles.AddRange(Directory.GetFiles(sourcesDir, ext, SearchOption.TopDirectoryOnly));
            return sourceFiles;
        }

        // Helper function to search for a source in a container.
        // If not found, simply return. If found and !overwrite it throws and error.
        // If found and remove then it removes the existing source.
        // Does not release container
        private void SearchRemoveSwItem(S7Container container, string swItemName, bool remove = true)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var swItems = container.Next;
                wrapper.Add(() => swItems);
                try
                {
                    wrapper.Add(() => swItems[swItemName]);
                    Log.Debug("SWItem {SwItem} found in container {Container}.", swItemName, container.Name);
                }
                catch (COMException)
                {
                    return;
                }

                if (!remove)
                {
                    throw new ArgumentException($"SWItem {swItemName} already exists and overwrite is disabled.",
                                                nameof(remove));
                }

                Log.Debug("SWItem {SwItem} already exists. Overwriting.", swItemName);
                try
                {
                    swItems.Remove(swItemName);
                }
                catch (COMException exc)
                {
                    throw new ArgumentException($"Could not remove existing SWItem {swItemName}.", exc);
                }
            }
        }

        /// <summary>
        /// Imports source into program
        /// </summary>
        /// <param name="container">Parent container object</param>
        /// <param name="sourceFilePath">Path to source file</param>
        /// <param name="sourceType">SW object type</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        private void ImportSourceImpl(S7Container container, string sourceFilePath,
            S7SWObjType sourceType = S7SWObjType.S7Source, bool overwrite = true)
        {
            string sourceName = Path.GetFileNameWithoutExtension(sourceFilePath);

            Log.Debug("Importing source {Name} ({Type}) from {FilePath}.",
                sourceName, sourceType, sourceFilePath);

            SearchRemoveSwItem(container, sourceName, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                var swItems = container.Next;
                try
                {
                    wrapper.Add(() => swItems.Add(sourceName, sourceType, sourceFilePath));
                }
                catch (Exception exc)
                {
                    Log.Error(exc, "Could not import source {Source} ({Type}) from {FilePath}.",
                        sourceName, sourceType, sourceFilePath);
                    throw;
                }
            }
        }

        /// <summary>
        /// Copies S7Source to destination S7SWItems container
        /// </summary>
        /// <param name="source">Target source to copy</param>
        /// <param name="destination">Target container onto which to copy source</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        private void CopySource(S7Container destination, S7Source source, bool overwrite = true)
        {
            Log.Debug("Importing source {Source} ({Type}) to {Destination}.",
                source.Name, source.ConcreteType, destination.Name);

            SearchRemoveSwItem(destination, source.Name, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var copy = source.Copy(destination);
                    wrapper.Add(() => copy);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not copy source {Source} ({Type}) to {Destination}.",
                        source.Name, source.ConcreteType, destination.Name);
                    throw;
                }
            }
        }

        private void ImportLibSourcesImpl(S7Container projSources, S7Container libSources, bool overwrite = true)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var swItems = libSources.Next;
                wrapper.Add(() => swItems);
                foreach (S7Source libSource in swItems)
                {
                    CopySource(destination: projSources, source: libSource, overwrite: overwrite);
                }
            }
        }

        private void ExportSourceImpl(S7Source source, string exportDir)
        {
            var sourceType = source.ConcreteType;
            string outputFile = Path.Combine(exportDir, source.Name);

            Log.Debug("Exporting {Source} ({Type}) to {OutputFile}.", source.Name, sourceType, outputFile);

            try
            {
                source.Export(outputFile);
            }
            catch (Exception exc)
            {
                Log.Error(exc, "Could not export source {Source} ({Type}) to {OutputFile}.",
                    source.Name, sourceType, outputFile);
                throw;
            }
        }

        private void ExportSourcesImpl(S7Container sources, string exportDir)
        {
            foreach (S7Source source in sources.Next)
            {
                ExportSourceImpl(source, exportDir);
            }
        }

        // - 0, S7SymImportInsert - Symbols are imported even if present, which may lead to ambiguities.
        // - 1, S7SymImportOverwriteNameLeading - existing values with the same name are replaced.
        // - 2, S7SymImportOverwriteOperandLeading - existing values with identical addresses are replaced.
        //  Symbol names are adjusted to the specifications in the import file.
        private S7SymImportFlags GetSymImportFlags(bool overwrite = false, bool nameLeading = false)
        {
            if (!overwrite)
                return S7SymImportFlags.S7SymImportInsert;
            else if (nameLeading)
                return S7SymImportFlags.S7SymImportOverwriteNameLeading;
            else
                return S7SymImportFlags.S7SymImportOverwriteOperandLeading;
        }

        // Read text file into a single string
        private string ReadFile(string path)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
            Log.Warning("File {Path} not found.", path);
            return "";
        }

        /// <summary>
        /// Returns counted errors, warnings and conflicts by parsing the content
        /// of the symbol importation file.
        /// </summary>
        /// <param name="errors">Number of errors during importation</param>
        /// <param name="warnings">Number of warnings during importation</param>
        /// <param name="conflicts">Number of symbol conflicts during importation</param>
        /// <returns>Report string</returns>
        private string GetImportReport(out int errors, out int warnings, out int conflicts)
        {
            Log.Debug("Reading import report in {ReportFile}.", ReportFilePath);

            string report = ReadFile(ReportFilePath);
            string pattern = @"Error: (\d+)[\n\r]+Warning\(s\): (\d+)[\n\r]+Conflict\(s\): (\d+)";
            var matches = Regex.Matches(report, pattern, RegexOptions.Multiline);
            if (matches.Count != 1 || matches[0].Groups.Count != 4)
            {
                throw new Exception("Could not extract error count from import report.");
            }

            try
            {
                errors = Int32.Parse(matches[0].Groups[1].Value);
                warnings = Int32.Parse(matches[0].Groups[2].Value);
                conflicts = Int32.Parse(matches[0].Groups[3].Value);
            }
            catch (Exception)
            {
                Log.Error("Could not extract error count from report in {ReportFile}.", ReportFilePath);
                throw;
            }

            return report;
        }

        /// <summary>
        /// Close the Notepad window with importation Log.
        /// </summary>
        private void CloseSymbolImportationLogWindow()
        {
            IntPtr windowHandle = IntPtr.Zero;
            foreach (var windowTitle in NotepadWindowTitles)
            {
                windowHandle = WindowsAPI.FindWindow(null, windowTitle);
                if (windowHandle != IntPtr.Zero)
                    break;
            }
            if (windowHandle == IntPtr.Zero)
            {
                Log.Warning("Could not find Notepad window with importation log.");
                return;
            }
            WindowsAPI.SendMessage(windowHandle, WindowsAPI.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            Log.Debug("Closed Notepad window with importation log.");
        }

        /// <summary>
        /// Import symbol table into target program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target program in S7 project</param>
        /// <param name="symbolFile">Path symbol file to import</param>
        /// <param name="flags">Importation flags</param>
        /// <param name="allowConflicts">
        /// Whether to allow conflicts. If false, then an exception is raised if conflicts are detected.
        /// </param>
        private void ImportSymbolsImpl(string project, string program, string symbolFile,
            S7SymImportFlags flags = S7SymImportFlags.S7SymImportInsert, bool allowConflicts = false)
        {
            S7SymbolTable symbolTable = null;
            int numImportedSymbols = 0;

            Log.Debug("Importing symbols from {SymbolFile} into {Project}\\{Program}.",
                symbolFile, project, program);

            using (var wrapper = new ReleaseWrapper())
            {
                S7Program programObj = wrapper.Add(() => GetProgram(project, program));
                try
                {
                    symbolTable = (S7SymbolTable)wrapper.Add(() => programObj.SymbolTable);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not access symbol table in {Project}\\{LogPath}.",
                        project, programObj.LogPath);
                    throw;
                }

                try
                {
                    numImportedSymbols = symbolTable.Import(symbolFile, Flags: flags);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not import symbol table into {Project}\\{LogPath} from {SymbolFile}.",
                        project, programObj.LogPath, symbolFile);
                    throw;
                }
            }

            string report = GetImportReport(out int errors, out int warnings, out int conflicts);
            CloseSymbolImportationLogWindow();

            Log.Debug("Imported {Symbols} symbols from {File} into {Project}\\{ProgramPath}.",
                numImportedSymbols, symbolFile, project, program);
            Log.Debug("Found {Errors} error(s), {Warnings} warning(s) and {Conflicts} conflict(s).",
                errors, warnings, conflicts);
            Log.Debug("{Report}", report);

            if (!allowConflicts && conflicts > 0)
                throw new Exception($"Symbols importation finished with {conflicts} conflict(s).");
        }

        /// <summary>
        /// Export symbol table to file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Target program in S7 project</param>
        /// <param name="symbolFile">Path to output symbol table file</param>
        private void ExportSymbolsImpl(string project, string program, string symbolFile)
        {
            Log.Debug("Exporting symbols from {Project}\\{Program} to {SymbolFile}.",
                project, program, symbolFile);
            S7SymbolTable symbolTable = null;

            using (var wrapper = new ReleaseWrapper())
            {
                S7Program programObj = wrapper.Add(() => GetProgram(project, program));
                try
                {
                    symbolTable = (S7SymbolTable)wrapper.Add(() => programObj.SymbolTable);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not access symbol table in {Project}\\{LogPath}.",
                        project, programObj.LogPath);
                    throw;
                }

                try
                {
                    symbolTable.Export(symbolFile);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not export symbols from {Project}\\{LogPath} to {SymbolFile}.",
                        project, programObj.LogPath, symbolFile);
                    throw;
                }
            }
        }

        /// <summary>
        /// Copies S7Block to destination S7SWItems container
        /// </summary>
        /// <param name="destination">Target container onto which to copy block</param>
        /// <param name="block">Target block to copy</param>
        /// <param name="overwrite">Overwrite existing block if present</param>
        private void CopyBlock(S7Container destination, S7Block block, bool overwrite = true)
        {
            Log.Debug("Copying block {Block} to container {Destination}.", block.Name, destination.Name);

            if (block.ConcreteType == S7BlockType.S7SDBs)
            {
                Log.Warning("Skiping system block {Block}.", block.Name);
                return;
            }

            SearchRemoveSwItem(destination, block.Name, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var item = block.Copy(destination);
                }
                catch (Exception exc)
                {
                    throw new Exception($"Could not import block {block.Name} ({block.ConcreteType})" +
                                        $" to {destination.Name}.", exc);
                }
            }
        }

        #endregion

        #region Public Commands

        /// <inheritdoc/>
        public void CreateProject(string projectName, string projectDir)
        {
            CreateProjectImpl(projectName, projectDir, S7ProjectType.S7Project);
        }

        /// <summary>
        /// Initializes existing project with basic elements.
        ///
        /// Concretely, creates a station (either S7-300 or S7-400) with a rack and a CPU.
        /// Adds a subnetwork and a PN-IO subsystem attached to it and the CPU's PN-IO submodule.
        /// Optionally adds an S7 connection for connecting to a remote server, e.g. a WinCC OA SCADA server.
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="plcName">Name of PLC station</param>
        /// <param name="plcType">Type of PLC station {"S7-300", "S7-400"}</param>
        /// <param name="cpuName">Name of CPU</param>
        /// <param name="cpuOrderNumber">CPU order number or MLFB (Machine-Readable Product Designation)</param>
        /// <param name="cpuFirmwareVersion">CPU firmware version</param>
        /// <param name="cpuIpAddress">IP address for CPU's PN-IO submodule</param>
        /// <param name="cpuSubnetMask">Subnetwork mask for CPU's PN-IO submodule</param>
        /// <param name="cpuRouterAddress">Router address for CPU's PN-IO submodule. By default it's not used.</param>
        /// <param name="remoteIpAddress">Address for S7 connection with remote server. By default it's not created.</param>
        /// <exception cref="ArgumentException"></exception>
        public void InitializeProject(string project, string plcName, string plcType,
            string cpuName, string cpuOrderNumber, string cpuFirmwareVersion,
            string cpuIpAddress, string cpuSubnetMask, string cpuRouterAddress = "",
            string remoteIpAddress = "")
        {
            // Derived parameters
            var stationType = S7StationType.S7300Station;
            if (plcType.Equals("S7-300"))
                stationType = S7StationType.S7300Station;
            else if (plcType.Equals("S7-400"))
                stationType = S7StationType.S7400Station;
            else
                throw new ArgumentException("Unsupported PLC type. Chose from {`S7-300`,`S7-400`}", nameof(plcType));

            bool routerActive = !string.IsNullOrEmpty(cpuRouterAddress);

            string rackName, rackOrderNumber, rackVersion;
            if (stationType == S7StationType.S7300Station)
            {
                rackName = "UR";
                rackOrderNumber = "6ES7 390-1???0-0AA0";
                rackVersion = "";
            }
            else
            {
                // TODO What should the default rack be? Turn into parameter?
                rackName = "UR1";
                rackOrderNumber = "6ES7 400-1TA00-0AA0";
                rackVersion = "";
            }

            // Constants
            const int RackSubstationNumber = 0;
            const int CpuSlot = 2;
            const string EthernetName = "ETHERNET(1)";
            const string SubnetId = "006A00000005";
            const int PnIoSubSystemIndex = 100;

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));

                Log.Debug("Adding S7Station for PLC {Name} ({Type}).", plcName, stationType);
                var station = wrapper.Add(() => projectObj.Stations.Add(plcName, stationType)) as IS7Station5;

                Log.Debug("Adding rack to station.");
                var racks = wrapper.Add(() => station.Racks);
                var rack = racks.Add(rackName, rackOrderNumber, rackVersion, RackSubstationNumber);
                wrapper.Add(() => rack);
                var modules = wrapper.Add(() => rack.Modules);

                Log.Debug("Adding CPU {Name} ({OrderNumber}) {FWVersion}.", cpuName, cpuOrderNumber, cpuFirmwareVersion);
                var cpu = modules.Add(cpuName, cpuOrderNumber, cpuFirmwareVersion, CpuSlot) as IS7Module6;
                wrapper.Add(() => cpu);

                Log.Debug("Adding subnetwork {EthernetName}.", EthernetName);
                var ethernetSubnet = wrapper.Add(() => station.Subnets.Add(EthernetName, S7SubnetType.INDUSTRIAL_ETHERNET));
                ethernetSubnet.Attribute["NET_ID"] = SubnetId;

                Log.Debug("Adding PN/IO subsystem to CPU's PN-IO submodule.");
                var pnIoModule = GetModuleImpl(cpu.Modules, "PN-IO");
                wrapper.Add(() => pnIoModule);
                var pnIoSubSystem = pnIoModule.AddSubSystem(EthernetName, PnIoSubSystemIndex);
                wrapper.Add(() => pnIoSubSystem);
                pnIoSubSystem.Attribute["NAME"] = "PROFINET IO System";

                Log.Debug("Setting PN/IO subnetwork attributes.");
                pnIoModule.IPAddress = AddressToHex(cpuIpAddress);
                pnIoModule.SubnetMask = AddressToHex(cpuSubnetMask);
                pnIoModule.RouterActive = routerActive ? 1 : 0;
                pnIoModule.RouterAddress = AddressToHex(cpuRouterAddress);
                cpu.LinkSubnet(ethernetSubnet);

                if (string.IsNullOrEmpty(remoteIpAddress))
                    return;

                Log.Debug("Adding S7 connection for WinCC OA project.");
                var remoteConnection = wrapper.Add(() => cpu.Conns.Add(S7ConnType.S7_CONNECTION, null));
                remoteConnection.Attribute["UNSPECIFIED"] = 1;
                // Do not establish active connection
                remoteConnection.Attribute["ACTIVE_CONN_SETUP"] = 0;
                remoteConnection.Attribute["REMOTE_ADDRESS"] = AddressToHex(remoteIpAddress);
            }
        }

        /// <inheritdoc/>
        public void CreateLibrary(string projectName, string projectDir)
        {
            CreateProjectImpl(projectName, projectDir, S7ProjectType.S7Library);
        }

        /// <inheritdoc/>
        public void RegisterProject(string projectFilePath)
        {
            Log.Debug("Registering existing project from {FilePath}.", projectFilePath);

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    wrapper.Add(() => Api.Projects.Add(projectFilePath, "", S7ProjectType.S7Project));
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not register existing project from {Filepath}.", projectFilePath);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveProject(string project)
        {
            Log.Information("Removing project {Project}.", project);

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                try
                {
                    projectObj.Remove();
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not remove project {Project}.", project);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void ImportSource(string project, string program, string source, bool overwrite = true)
        {
            Log.Debug("Importing {Source} into {Project}\\{Program} with overwrite={Overwrite}.",
                source, project, program, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var container = wrapper.Add(() => GetSources(projectObj, program));
                ImportSourceImpl(container: container, sourceFilePath: source, overwrite: overwrite);
            }
        }

        /// <inheritdoc/>
        public void ImportSourcesDir(string project, string program, string sourcesDir, bool overwrite = true)
        {
            Log.Debug("Importing sources into {Project}\\{Program} from {SourcesDir} with overwrite={Overwrite}.",
                project, program, sourcesDir, overwrite);

            var sourceFiles = GetSourcesFromDir(sourcesDir);

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var container = wrapper.Add(() => GetSources(projectObj, program));
                foreach (var source in sourceFiles)
                {
                    ImportSourceImpl(container: container, sourceFilePath: source, overwrite: overwrite);
                }
            }
        }

        /// <inheritdoc/>
        public void ImportLibSources(string project, string program, string library, string libProgram,
            bool overwrite = true)
        {
            Log.Debug("Importing sources from {Library}\\{LibProgram} into {Project}\\{ProjProgram}"+
                " with overwrite={Overwrite}.", library, libProgram, project, program, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var libraryObj = wrapper.Add(() => GetProject(library));
                var projectContainer = wrapper.Add(() => GetSources(projectObj, program));
                var libraryContainer = wrapper.Add(() => GetSources(libraryObj, libProgram));
                ImportLibSourcesImpl(projSources: projectContainer, libSources: libraryContainer, overwrite: overwrite);
            }
        }

        /// <inheritdoc/>
        public void ExportAllSources(string project, string program, string sourcesDir)
        {
            Log.Debug("Exporting sources {Project}/{Program} to {Dir}.", project, program, sourcesDir);

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var projectSources = wrapper.Add(() => GetSources(projectObj, program));
                ExportSourcesImpl(projectSources, sourcesDir);
            }
        }

        /// <inheritdoc/>
        public void ExportSource(string project, string program, string source, string sourcesDir)
        {
            Log.Debug("Exporting {Source} to {Dir}.", source, sourcesDir);

            using (var wrapper = new ReleaseWrapper())
            {
                var sourceObj = wrapper.Add(() => GetSource(project, program, source));
                ExportSourceImpl(sourceObj, sourcesDir);
            }
        }

        /// <inheritdoc/>
        public void CreateProgram(string project, string programName)
        {
            Log.Debug("Creating S7 program {Name} in {Project}.", programName, project);

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var programs = wrapper.Add(() => projectObj.Programs);
                try
                {
                    wrapper.Add(() => programs.Add(programName, S7ProgramType.S7));
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not create S7 program {Name} in {Project}.", programName, project);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void CompileSources(string project, string program, List<string> sources)
        {
            foreach (var source in sources)
            {
                CompileSource(project, program, source);
            }
        }

        /// <inheritdoc/>
        public void ImportLibBlocks(string project, string program, string library, string libProgram, bool overwrite = true)
        {
            Log.Debug("Importing blocks from {Library}\\{LibProgram} into {Project}\\{ProjProgram} with overwrite={Overwrite}.",
                library, libProgram, project, program, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                var libraryObj = wrapper.Add(() => GetProject(library));
                var projectObj = wrapper.Add(() => GetProject(project));
                var libraryBlocks = wrapper.Add(() => GetBlocks(libraryObj, libProgram));
                var projectBlocks = wrapper.Add(() => GetBlocks(projectObj, program));

                var container = wrapper.Add(() => libraryBlocks.Next);
                foreach (S7Block libBlock in container)
                {
                    wrapper.Add(() => libBlock);
                    // Note: "System data" blocks to not have SymbolicName attribute
                    if (libBlock.Name == "System data")
                    {
                        Log.Debug("Cannot copy System data block. Skipping.");
                        continue;
                    }
                    CopyBlock(destination: projectBlocks, block: libBlock, overwrite: overwrite);
                }
            }
        }

        /// <inheritdoc/>
        public void ImportSymbols(string project, string program, string symbolFile, bool overwrite = false, bool nameLeading = false, bool allowConflicts = false)
        {
            var flags = GetSymImportFlags(overwrite, nameLeading);
            ImportSymbolsImpl(project, program, symbolFile, flags, allowConflicts);
        }

        /// <inheritdoc/>
        public void ExportSymbols(string project, string program, string symbolFile, bool overwrite = false)
        {
            string exportDir = Path.GetDirectoryName(symbolFile);
            if (!Directory.Exists(exportDir))
            {
                Log.Error("Could not export symbols from {Project}\\{Program}.", project, program);
                throw new IOException($"Output directory does not exist {exportDir}.");
            }

            // TODO: Ensure output has supported extension?

            if (File.Exists(symbolFile) && !overwrite)
            {
                Log.Error("Could not export symbols from {Project}\\{Program}.", project, program);
                throw new IOException($"Output file already exists {symbolFile}.");
            }
            else if (File.Exists(symbolFile))
            {
                Log.Information("Overwriting {SymbolFile}.", symbolFile);
            }

            ExportSymbolsImpl(project, program, symbolFile);
        }

        /// <inheritdoc/>
        public void ExportStation(string project, string station, string exportFile)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                S7Station stationObj = null;
                var projectObj = wrapper.Add(() => GetProject(project));
                try
                {
                    stationObj = wrapper.Add(() => projectObj.Stations[station]);
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not access station {station} in project {project}.", exc);
                }

                try
                {
                    stationObj.Export(exportFile);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not export station {Station} to {ExportFile}.", station, exportFile);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void EditModule(string project, string station, string rack, string modulePath, Dictionary<string, object> properties)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var stationObj = wrapper.Add(() => GetStationImpl(projectObj, station));
                var rackObj = wrapper.Add(() => GetRackImpl(stationObj, rack));
                var moduleObj = wrapper.Add(() => GetModuleImpl(rackObj.Modules, modulePath));
                SetModuleProperties(moduleObj, properties);
            }
        }

        private void SetModuleProperties(IS7Module6 module, Dictionary<string, object> properties)
        {
            foreach (var kvPair in properties)
            {
                try
                {
                    SetModuleProperty(module, kvPair.Key, kvPair.Value);
                    Log.Debug("Set {Module} {Key}={Value}.", module.LogPath, kvPair.Key, kvPair.Value);
                }
                catch (Exception exc)
                {
                    Log.Error(exc, "Could not set {Module} {Key}={Value}.", module.LogPath, kvPair.Key, kvPair.Value);
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
                    throw new ArgumentException($"Unknown module property {property}.", nameof(property));
            }
        }

        /// <summary>
        /// Returns IP address from hexadecimal string representation of address.
        /// Assumes big-endianness
        /// </summary>
        /// <param name="hex">Hex input string</param>
        /// <returns>IP Adress object</returns>
        private static IPAddress HexToAddress(string hex)
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
        private static string AddressToHex(string address)
        {
            var ipAddress = IPAddress.Parse(address);
            byte[] bytes = ipAddress.GetAddressBytes();
            string hex = "";
            foreach (byte val in bytes) hex += $"{val:X2}";
            return hex;
        }

        #region Compile Methods

        /// <inheritdoc/>
        public void CompileAllStations(string project, bool allowFail = true)
        {
            IS7Stations stations = null;

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                try
                {
                    stations = wrapper.Add(() => projectObj.Stations);
                }
                catch (Exception exc)
                {
                    Log.Error(exc, "Could not access stations in project {Project}", project);
                    throw;
                }

                foreach (IS7Station station in stations)
                {
                    var stationObj = wrapper.Add(() => station);
                    try
                    {
                        stationObj.Compile();
                        Log.Debug("Compiled HW config for {Station}", stationObj.Name);
                    }
                    catch (Exception exc)
                    {
                        Log.Error(exc, "Could not compile HW config for {Station}", stationObj.Name);
                        if (!allowFail) throw;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void CompileSource(string project, string program, string sourceName)
        {
            Log.Debug("Compiling source {Source} in {Project}\\{Program}", sourceName, project, program);

            using (var wrapper = new ReleaseWrapper())
            {
                var source = wrapper.Add(() => GetSource(project, program, sourceName));
                var sourceType = source.ConcreteType;

                if (sourceType == S7SourceType.S7SCL || sourceType == S7SourceType.S7SCLMake)
                {
                    CompileSclSource(source);
                }
                else if (sourceType == S7SourceType.S7AWL)
                {
                    CompileAwlSource(source);
                }
                else
                {
                    try
                    {
                        var swItems = source.Compile();
                        wrapper.Add(() => swItems);
                    }
                    catch (COMException exc)
                    {
                        throw new Exception($"Could not compile source {sourceName} in {project}\\{program}", exc);
                    }
                }
            }
        }

        /// <summary>
        /// Compiles .SCL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        private void CompileSclSource(S7Source src)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var swItems = src.Compile();
                    wrapper.Add(() => swItems);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not compile source {Name}.", src.Name);
                }
            }

            // get status and close the SCL compiler
            S7CompilerSCL compiler = new S7CompilerSCL(Log);
            Log.Debug("SCL status buffer:\n{Buffer}", compiler.GetSclStatusBuffer());
            //string statusLine = compiler.getSclStatusLine();
            int errors = compiler.GetErrorCount();
            int warnings = compiler.GetWarningCount();
            compiler.CloseSclWindow();

            if (errors > 0)
            {
                throw new Exception($"Could not compile {src.Name}: {errors} error(s).");
            }
            else if (warnings > 0)
            {
                Log.Warning("Compiled {Source} with {Warnings} warning(s).", src.Name, warnings);
            }
        }

        /// <summary>
        /// Compiles .AWL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        private void CompileAwlSource(S7Source src)
        {
            // special setting for STL compilation, CRG-1417
            // ("quiet" compilation with status written to a log file)
            var verbLogFile = Path.GetTempFileName();
            Api.VerbLogFile = verbLogFile;
            Log.Debug("Set Simatic.VerbLogFile to {VerbLogFile}", Api.VerbLogFile);

            // truncate log file
            FileStream oStream = new FileStream(verbLogFile, FileMode.Open, FileAccess.ReadWrite);
            oStream.SetLength(0);
            oStream.Close();

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var swItems = src.Compile();
                    wrapper.Add(() => swItems);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not compile source {Name}.", src.Name);
                }
            }

            if (!File.Exists(verbLogFile))
            {
                throw new Exception($"Compilation log file not found {verbLogFile}.");
            }

            // read and show the log file
            string[] logfile = File.ReadAllLines(verbLogFile);
            Array.ForEach(logfile, l => Log.Debug("{Line}", l));
            File.Delete(verbLogFile);

            // parse status in the logfile
            int errors, warnings;
            string statusLineRegExStr = "Compiler result.*Error.*Warning.*";
            //Regex statusLineRegEx = new Regex(statusLineRegExStr);

            if (Array.Exists<string>(
                    logfile, s => Regex.IsMatch(s, statusLineRegExStr)))
            {
                // we get line like:
                // Compiler result: 0 Error(s), 0 Warning(s)
                // -> have to parse it to get the numbers of errors and warnings
                string[] statusLine =
                    Array.Find<string>(
                        logfile, s => Regex.IsMatch(s, statusLineRegExStr)).Split(' ');
                errors = Int32.Parse(statusLine[2]);
                warnings = Int32.Parse(statusLine[4]);
            }
            else
            {
                throw new Exception($"Could not retrieve compilation result from {verbLogFile}.");
            }

            if (errors > 0)
            {
                throw new Exception($"Could not compile {src.Name}: {errors} error(s).");
            }
            else if (warnings > 0)
            {
                Log.Warning("Compiled {Name} with {Warnings} warning(s).", src.Name, warnings);
            }
        }

        #endregion

        #region List Commands

        /// <inheritdoc/>
        public Dictionary<string, string> ListProjects()
        {
            Log.Debug($"Listing registered projects.");

            var output = new Dictionary<string, string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projects = wrapper.Add(() => Api.Projects);
                foreach (S7Project project in projects)
                {
                    var projectObj = wrapper.Add(() => project);
                    output.Add(projectObj.LogPath, projectObj.Name);
                    Log.Debug("Project={Name}, LogPath={LogPath}", projectObj.Name, projectObj.LogPath);
                }
            }
            return output;
        }

        /// <inheritdoc/>
        public List<string> ListPrograms(string project, bool json = true)
        {
            Log.Debug("Listing programs for project {Project}.", project);

            var output = new List<string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var programs = wrapper.Add(() => projectObj.Programs);
                var programStr = new List<string>(); 
                foreach (S7Program program in programs)
                {
                    var programObj = wrapper.Add(() => program);
                    Log.Debug("Program {Name} Path={LogPath}", programObj.Name, programObj.LogPath);
                    var programEntry = json? JsonConvert.SerializeObject(new { name = programObj.Name, logPath = programObj.LogPath })
                        : programObj.Name;
                    output.Add(programEntry);
                }
            }

            return output;
        }

        /// <inheritdoc/>
        public List<string> ListModules(string project)
        {
            Log.Debug("Listing modules for project {Project}.", project);

            var output = new List<string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var stations = wrapper.Add(() => projectObj.Stations);
                foreach (var station in stations)
                {
                    var stationObj = (IS7Station6)wrapper.Add(() => station);
                    var racks = wrapper.Add(() => stationObj.Racks);
                    foreach (var rack in racks)
                    {
                        var rackObj = (IS7Rack)wrapper.Add(() => rack);
                        GetModuleNames(output, (IS7Modules)rackObj.Modules);
                    }
                }
            }
            return output;
        }

        /// <inheritdoc/>
        public List<string> ListStations(string project)
        {
            Log.Debug("Listing stations in project {Project}.", project);

            var output = new List<string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var stations = wrapper.Add(() => projectObj.Stations);
                foreach (var station in stations)
                {
                    var stationObj = (IS7Station6)wrapper.Add(() => station);
                    output.Add(stationObj.Name);
                    Log.Debug("Station {Name}", stationObj.Name);
                }
            }
            return output;
        }

        /// <inheritdoc/>
        // TODO: Maybe include program name in output as well?
        public List<string> ListContainers(string project)
        {
            Log.Debug("Listing containers in project {Project}.", project);

            var output = new List<string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var programs = wrapper.Add(() => projectObj.Programs);
                foreach (S7Program program in projectObj.Programs)
                {
                    var programObj = wrapper.Add(() => program);
                    Log.Debug("Listing containers for program {Project}\\{Program}.", project, programObj.Name);

                    var containers = wrapper.Add(() => programObj.Next);
                    foreach (S7Container container in containers)
                    {
                        wrapper.Add(() => container);
                        output.Add(container.Name);
                        Log.Debug("Container {Name} ({Type})", container.Name, container.ConcreteType);
                    }
                }
            }
            return output;
        }

        #endregion

        #region Online Commands

        /// <inheritdoc/>
        public void DownloadProgramBlocks(string project, string program, bool overwrite)
        {
            Log.Information("[ONLINE] Downloading blocks for {Project}\\{Program}.", project, program);

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var programObj = wrapper.Add(() => GetProgram(project, program));
                var flag = overwrite ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk;

                try
                {
                    var blocks = GetBlocks(projectObj, programObj.Name);
                    blocks.Download(flag);
                }
                catch (Exception exc)
                {
                    Log.Error(exc, "Could not download blocks to {Program} in {LogPath}.", programObj.Name, programObj.LogPath);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void StartProgram(string project, string program)
        {
            Log.Information("[ONLINE] Starting program {Project}\\{Program}.", project, program);

            using (var wrapper = new ReleaseWrapper())
            {
                var programObj = wrapper.Add(() => GetProgram(project, program));
                try
                {
                    if (programObj.ModuleState != S7ModState.S7Run)
                    {
                        programObj.NewStart();
                    }
                    else
                    {
                        Log.Debug("{Program} is already in RUN mode. Restarting.", programObj.Name);
                        programObj.Restart();
                    }
                }
                catch (Exception exc)
                {
                    Log.Error(exc, "Could not start/restart {Program} in {LogPath}.", programObj.Name, programObj.LogPath);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void StopProgram(string project, string program)
        {
            Log.Information("[ONLINE] Stopping program {Project}\\{Program}.", project, program);

            using (var wrapper = new ReleaseWrapper())
            {
                var programObj = wrapper.Add(() => GetProgram(project, program));

                try
                {
                    if (programObj.ModuleState != S7ModState.S7Stop)
                        programObj.Stop();
                }
                catch (Exception exc)
                {
                    Log.Error(exc, "Could not stop {Program} in {LogPath}.", programObj.Name, programObj.LogPath);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveProgramOnlineBlocks(string project, string program)
        {
            Log.Information("[ONLINE] Removing blocks {Project}\\{Program}.", project, program);

            using (var wrapper = new ReleaseWrapper())
            {
                var programObj = wrapper.Add(() => GetProgram(project, program));
                var onlineBlocks = wrapper.Add(() => programObj.OnlineBlocks);

                // Improve performance by stopping program
                try { programObj.Stop(); } catch (COMException) { }
                // Improve performance by preventing automatic project saves on `block.Remove()`
                var prevAutomaticSave = Api.AutomaticSave;
                Api.AutomaticSave = 0;

                try
                {
                    foreach (var onlineBlock in onlineBlocks)
                    {
                        var block = onlineBlock as IS7Block3;
                        wrapper.Add(() => block);
                        if (SystemBlockTypes.Contains(block.ConcreteType))
                            continue;
                        Log.Debug("Removing online block {Name}.", block.Name);
                        block.Remove();
                    }
                }
                catch (COMException exc)
                {
                    Log.Error(exc, "Could not remove blocks in {Program} in {LogPath}.", programObj.Name, programObj.LogPath);
                    throw;
                }
                finally
                {
                    Api.AutomaticSave = prevAutomaticSave;
                }
            }
        }

        #endregion

        #endregion
    }
}
