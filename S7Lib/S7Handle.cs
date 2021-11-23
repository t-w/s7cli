using System;
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

namespace S7Lib
{
    /// <summary>
    /// Handle for S7Lib functions
    /// </summary>
    public sealed class S7Handle : IDisposable
    {
        #region Class Attributes, Ctor and Dispose

        /// <summary>
        /// Handle for the Simatic API
        /// </summary>
        public readonly Simatic Api;
        /// <summary>
        /// Handle for Serilog logger
        /// </summary>
        public readonly Logger Log;

        // Default path for symbol import report file
        private static readonly string ReportFilePath = @"C:\ProgramData\Siemens\Automation\Step7\S7Tmp\sym_imp.txt";
        // Default titles for Notepad window opened on import symbols command
        // The title window may or may not include the .txt extension depending on Explorer settings
        private static readonly string[] NotepadWindowTitles = new string[]{
            "sym_imp - Notepad", "sym_imp.txt - Notepad"
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
            if (Api != null)
                Marshal.FinalReleaseComObject(Api);
            Log?.Dispose();
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
        /// Obtains a project object, from the path to its .s7p project file or its name
        /// </summary>
        /// <remarks>
        /// Project names are not guaranteed to be unique.
        /// However, the path to .s7p project file is, and is therefore preferred as a projectId.
        /// </remarks>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
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
                        return (S7Project)projects.Add(Name: project);
                    }
                    else
                    {
                        return (S7Project)projects[project];
                    }
                }
                catch (Exception exc)
                {
                    throw new KeyNotFoundException($"Could not get project {project}", exc);
                }
            }
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
        private S7Program GetProgram(string project, string programPath)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                return GetProgramImpl(projectObj, programPath);
            }
        }

        private S7Program GetProgramImpl(S7Project project, string programPath)
        {
            var logPath = $"{project.Name}\\{programPath}";
            S7Program programObj;

            using (var wrapper = new ReleaseWrapper())
            {
                var programs = project.Programs;
                wrapper.Add(() => programs);
                foreach (IS7Program s7Program in programs)
                {
                    if (s7Program.LogPath == logPath)
                    {
                        programObj = (S7Program)s7Program;
                        Log.Debug($"Found S7Program(Name={s7Program.Name}, LogPath={s7Program.LogPath})");
                        return programObj;
                    }
                    wrapper.Add(() => s7Program);
                }
            }
            throw new KeyNotFoundException($"Could not find program in {logPath}");
        }

        private IS7Station GetStationImpl(S7Project project, string station)
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
                    throw new KeyNotFoundException($"Could not find station {station}", exc);
                }
            }
        }

        private S7Rack GetRackImpl(IS7Station station, string rack)
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
                    throw new KeyNotFoundException($"Could not find rack {rack}", exc);
                }
            }
        }

        private IS7Module6 GetModuleImpl(S7Modules modules, string modulePath)
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
                        throw new KeyNotFoundException($"Could not find module {modulePath}", exc);
                    }
                }
            }
            return moduleObj;
        }

        private static S7Container GetContainer(S7Project projectObj, string program, S7ContainerType type)
        {
            IS7Program programObj;

            using (var wrapper = new ReleaseWrapper())
            {
                var programs = projectObj.Programs;
                wrapper.Add(() => programs);
                try
                {
                    programObj = wrapper.Add(() => programs[program]);
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not access program {projectObj.Name}\\{program}", exc);
                }

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
            throw new KeyNotFoundException($"Could not find container of type {type} in {projectObj.Name}\\{program}");
        }

        private static S7Container GetSources(S7Project projectObj, string program)
        {
            return GetContainer(projectObj, program, S7ContainerType.S7SourceContainer);
        }

        private static S7Container GetBlocks(S7Project projectObj, string program)
        {
            return GetContainer(projectObj, program, S7ContainerType.S7BlockContainer);
        }


        /// <summary>
        /// Gets a S7Source object from a program
        /// </summary>
        /// <remarks>The source container may not be named "Sources"</remarks>
        /// <param name="program">Program name</param>
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
                    throw new KeyNotFoundException($"Could not find source {source} in {program}", exc);
                }
            }
        }

        /// <summary>
        /// Gets a S7Block object from a program
        /// </summary>
        /// <remarks>The block container may not be named "Blocks"</remarks>
        /// <param name="projectObj">S7Project object</param>
        /// <param name="program">Program name</param>
        /// <param name="block">Block name</param>
        /// <returns>S7 Block</returns>
        private static S7Block GetBlock(S7Project projectObj, string program, string block)
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
                    throw new KeyNotFoundException($"Could not find block {block} in {program}", exc);
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
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        /// <param name="projectType">Project type (S7Project or S7Library)</param>
        private void CreateProjectImpl(string projectName, string projectDir, S7ProjectType projectType)
        {
            Log.Debug($"Creating empty project {projectName} in {projectDir}");

            if (projectName.Length > 8)
            {
                Log.Error($"Could not create project {projectName} in {projectDir}");
                throw new ArgumentException($"Invalid project name {projectName}: has more than 8 characters.", nameof(projectName));
            }

            if (ProjectIsRegistered(projectName))
            {
                // Otherwise Projects.Add() spawns a blocking GUI error message
                Log.Error($"Could not create project {projectName} in {projectDir}");
                throw new ArgumentException($"Project with name {projectName} already exists");
            }

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    wrapper.Add(() => Api.Projects.Add(projectName, projectDir, projectType));

                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not create project {projectName} in {projectDir}");
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
                    Log.Debug($"SWItem {swItemName} found in container {container.Name}.");
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

                Log.Debug($"SWItem {swItemName} already exists. Overwriting.");
                try
                {
                    swItems.Remove(swItemName);
                }
                catch (COMException exc)
                {
                    throw new ArgumentException($"Could not remove existing SWItem {swItemName}", exc);
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

            Log.Debug($"Importing source {sourceName} ({sourceType}) from {sourceFilePath}");

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
                    Log.Error(exc, $"Could not import source {sourceName} ({sourceType}) from {sourceFilePath}");
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
        private void CopySource(S7Source source, S7Container destination, bool overwrite = true)
        {
            Log.Debug($"Importing source {source.Name} ({source.ConcreteType}) to {destination.Name}");

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
                    Log.Error(exc, $"Could not copy source {source.Name} ({source.ConcreteType}) to {destination.Name}");
                    throw;
                }
            }
        }

        private void ImportLibSourcesImpl(S7Container libSources, S7Container projSources, bool overwrite = true)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var swItems = libSources.Next;
                wrapper.Add(() => swItems);
                foreach (S7Source libSource in swItems)
                {
                    CopySource(libSource, projSources, overwrite);
                }
            }
        }

        private void ExportSourceImpl(S7Source source, string exportDir)
        {
            var sourceType = source.ConcreteType;
            string outputFile = Path.Combine(exportDir, source.Name);

            Log.Debug($"Exporting {source.Name} ({sourceType}) to {outputFile}");

            try
            {
                source.Export(outputFile);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not export source {source.Name} ({sourceType}) to {outputFile}");
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
            Log.Warning($"File {path} not found");
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
            string report = ReadFile(ReportFilePath);
            string[] split = report.Split('\n');

            int errorIndex = Array.FindIndex<string>(split, s => Regex.IsMatch(s, "^Error:.*"));
            int warningsIndex = errorIndex + 1;
            int conflictsIndex = errorIndex + 2;

            errors = Int32.Parse(split[errorIndex].Split(' ')[1]);
            warnings = Int32.Parse(split[warningsIndex].Split(' ')[1]);
            conflicts = Int32.Parse(split[conflictsIndex].Split(' ')[1]);

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
                if (windowHandle != IntPtr.Zero) break;
            }
            if (windowHandle == IntPtr.Zero)
            {
                Log.Warning($"Could not find Notepad window with importation Log.");
                return;
            }
            WindowsAPI.SendMessage(windowHandle, WindowsAPI.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            Log.Debug($"Closed Notepad window with importation log");
        }

        /// <summary>
        /// Import symbol table into target program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Path to program in S7 project</param>
        /// <param name="symbolFile">Path symbol file to import</param>
        /// <param name="flag">Importation flags</param>
        /// <param name="allowConflicts">
        /// Whether to allow conflicts. If false, then an exception is raised if conflicts are detected.
        /// </param>
        private void ImportSymbolsImpl(string project, string programPath, string symbolFile,
            S7SymImportFlags flags = S7SymImportFlags.S7SymImportInsert, bool allowConflicts = false)
        {
            S7SymbolTable symbolTable = null;
            int numImportedSymbols = 0;

            Log.Debug($"Importing symbols from {symbolFile} into {project}\\{programPath}");

            using (var wrapper = new ReleaseWrapper())
            {
                S7Program programObj = wrapper.Add(() => GetProgram(project, programPath));
                try
                {
                    symbolTable = (S7SymbolTable)wrapper.Add(() => programObj.SymbolTable);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not access symbol table in {project}\\{programObj.LogPath}");
                    throw;
                }

                try
                {
                    numImportedSymbols = symbolTable.Import(symbolFile, Flags: flags);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not import symbol table into {project}\\{programObj.LogPath} " +
                                   $"from {symbolFile}");
                    throw;
                }
            }

            string report = GetImportReport(out int errors, out int warnings, out int conflicts);
            CloseSymbolImportationLogWindow();

            Log.Debug($"Imported {numImportedSymbols} symbols from {symbolFile} into {project}\\{programPath}\n" +
                      $"Report {errors} error(s), {warnings} warning(s) and {conflicts} conflict(s):\n" +
                      $"{report}");

            if (!allowConflicts && conflicts > 0)
                throw new Exception($"Symbols importation finished with {conflicts} conflict(s)");
        }

        /// <summary>
        /// Export symbol table to file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Path to program in S7 project</param>
        /// <param name="symbolFile">Path to output symbol table file</param>
        private void ExportSymbolsImpl(string project, string programPath, string symbolFile)
        {
            Log.Debug($"Exporting symbols from {project}\\{programPath} to {symbolFile}");
            S7SymbolTable symbolTable = null;

            using (var wrapper = new ReleaseWrapper())
            {
                S7Program programObj = wrapper.Add(() => GetProgram(project, programPath));
                try
                {
                    symbolTable = (S7SymbolTable)wrapper.Add(() => programObj.SymbolTable);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not access symbol table in {project}\\{programObj.LogPath}");
                    throw;
                }

                try
                {
                    symbolTable.Export(symbolFile);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not export symbols from {project}\\{programObj.LogPath} " +
                                   $"to {symbolFile}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Copies S7Block to destination S7SWItems container
        /// </summary>
        /// <param name="block">Target block to copy</param>
        /// <param name="destination">Target container onto which to copy block</param>
        /// <param name="overwrite">Overwrite existing block if present</param>
        private void CopyBlock(S7Block block, S7Container destination, bool overwrite = true)
        {
            Log.Debug($"Copying block {block.Name} to container {destination.Name}.");

            if (block.ConcreteType == S7BlockType.S7SDBs)
            {
                Log.Warning($"Block {block.Name} is a system data block: skipping.");
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
                    throw new Exception($"Could not import block {block.Name} ({block.ConcreteType}) to {destination.Name}.", exc);
                }
            }
        }

        #endregion

        #region Public Commands

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
            Log.Debug($"Registering existing project from {projectFilePath}");

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    wrapper.Add(() => Api.Projects.Add(projectFilePath, "", S7ProjectType.S7Project));
                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not register existing project in {projectFilePath}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Removes STEP 7 project and deletes all of its files
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        public void RemoveProject(string project)
        {
            Log.Information($"Removing project {project}");

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                try
                {
                    projectObj.Remove();
                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not remove project {project}");
                    throw;
                }
            }
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
            Log.Debug($"Importing {source} to {project}\\{program}");

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var container = wrapper.Add(() => GetSources(projectObj, program));
                ImportSourceImpl(container: container, sourceFilePath: source, overwrite: overwrite);
            }
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
            Log.Debug($"Importing sources to {project}\\{program} for {sourcesDir}");

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
            Log.Debug($"Importing sources from {library}\\{libProgram} into {project}\\{projProgram}");

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var libraryObj = wrapper.Add(() => GetProject(library));
                var projectContainer = wrapper.Add(() => GetSources(projectObj, projProgram));
                var libraryContainer = wrapper.Add(() => GetSources(libraryObj, libProgram));
                ImportLibSourcesImpl(libSources: libraryContainer, projSources: projectContainer, overwrite);
            }
        }

        /// <summary>
        /// Exports all sources from a program to a directory
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        public void ExportAllSources(string project, string program, string sourcesDir)
        {
            Log.Debug($"Exporting sources {project}/{program} to {sourcesDir}");

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var projectSources = wrapper.Add(() => GetSources(projectObj, program));
                ExportSourcesImpl(projectSources, sourcesDir);
            }
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
            Log.Debug($"Exporting {source} to {sourcesDir}");

            using (var wrapper = new ReleaseWrapper())
            {
                var sourceObj = wrapper.Add(() => GetSource(project, program, source));
                ExportSourceImpl(sourceObj, sourcesDir);
            }
        }

        /// <summary>
        /// Creates a new empty S7 program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programName">Program name</param>
        public void CreateProgram(string project, string programName)
        {
            Log.Debug($"Creating S7 program {programName} in {project}");

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
                    Log.Error(exc, $"Could not create S7 program {programName} in {project}");
                    throw;
                }
            }
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
                CompileSource(project, program, source);
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
            Log.Debug($"Importing blocks from {library}\\{libProgram} into {project}\\{projProgram}");

            using (var wrapper = new ReleaseWrapper())
            {
                var libraryObj = wrapper.Add(() => GetProject(library));
                var projectObj = wrapper.Add(() => GetProject(project));
                var libraryBlocks = wrapper.Add(() => GetBlocks(libraryObj, libProgram));
                var projectBlocks = wrapper.Add(() => GetBlocks(projectObj, projProgram));

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
                    CopyBlock(libBlock, projectBlocks, overwrite);
                }

            }
        }

        /// <summary>
        /// Imports symbols into a program from a file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Logical path to program (not including project name)</param>
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
        public void ImportSymbols(string project, string programPath, string symbolFile, bool overwrite = false, bool nameLeading = false, bool allowConflicts = false)
        {
            var flags = GetSymImportFlags(overwrite, nameLeading);
            ImportSymbolsImpl(project, programPath, symbolFile, flags, allowConflicts);
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
                Log.Error($"Could not export symbols from {project}\\{programPath}");
                throw new IOException($"Output directory does not exist {exportDir}");
            }

            // TODO: Ensure output has supported extension?

            if (File.Exists(symbolFile) && !overwrite)
            {
                Log.Error($"Could not export symbols from {project}\\{programPath}");
                throw new IOException($"Output file already exists {symbolFile}");
            }
            else if (File.Exists(symbolFile))
            {
                Log.Information($"Overwriting {symbolFile}");
            }

            ExportSymbolsImpl(project, programPath, symbolFile);
        }

        /// <summary>
        /// Exports the hardware configuration of a target station
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Name of target station to export</param>
        /// <param name="exportFile">Path to output export file (generally .cfg file)</param>
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
                    throw new KeyNotFoundException($"Could not access station {station} in project {project}", exc);
                }

                try
                {
                    stationObj.Export(exportFile);
                }
                catch (COMException exc)
                {
                    Log.Error(exc, $"Could not export station {station} to {exportFile}");
                    throw;
                }
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
                    throw new ArgumentException($"Unknown module property {property}", nameof(property));
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

        /// <summary>
        /// Compiles the HW configuration for each of the stations in a project
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="allowFail">If false, an exception is thrown if a station fials to compile</param>
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
                    Log.Error(exc, $"Could not access stations in project {project}");
                    throw;
                }

                foreach (IS7Station station in stations)
                {
                    var stationObj = wrapper.Add(() => station);
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
            }
        }

        /// <summary>
        /// Compile source
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        public void CompileSource(string project, string program, string sourceName)
        {
           Log.Debug($"Compiling source {sourceName} in {project}\\{program}");

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
                    Log.Error($"Could not compile source {src.Name}.", exc);
                }
            }

            // get status and close the SCL compiler
            S7CompilerSCL compiler = new S7CompilerSCL(Log);
            Log.Debug($"SCL status buffer:\n{compiler.GetSclStatusBuffer()}");
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
                Log.Warning($"Compiled {src.Name} with {warnings} warning(s).");
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
            Log.Debug($"Set Simatic.VerbLogFile to {Api.VerbLogFile}");

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
                    Log.Error($"Could not compile source {src.Name}.", exc);
                }
            }

            if (!File.Exists(verbLogFile))
            {
                throw new Exception($"Compilation log file not found {verbLogFile}.");
            }

            // read and show the log file
            string[] logfile = File.ReadAllLines(verbLogFile);
            Array.ForEach<string>(logfile, s => Log.Debug(s));
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
                Log.Warning($"Compiled {src.Name} with {warnings} warning(s).");
            }
        }

        #endregion

        #region List Commands

        /// <summary>
        /// Returns dictionary with {projectDir, projectName} key-value pairs
        /// </summary>
        /// <param name="output">Dictionary with {projectDir, projectName} key-value pairs</param>
        public Dictionary<string, string> ListProjects()
        {
            Log.Debug($"Listing registered projects");

            var output = new Dictionary<string, string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projects = wrapper.Add(() => Api.Projects);
                foreach (S7Project project in projects)
                {
                    var projectObj = wrapper.Add(() => project);
                    output.Add(projectObj.LogPath, projectObj.Name);
                    Log.Debug($"Project {projectObj.Name} Path {projectObj.LogPath}");
                }
            }
            return output;
        }

        /// <summary>
        /// Returns list with programs in a given project
        /// </summary>
        public List<string> ListPrograms(string project)
        {
            Log.Debug($"Listing programs for project {project}");

            var output = new List<string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var programs = wrapper.Add(() => projectObj.Programs);
                foreach (S7Program program in programs)
                {
                    var programObj = wrapper.Add(() => program);
                    output.Add(programObj.Name);
                    Log.Debug($"Program {programObj.Name}");
                }
            }
            return output;
        }

        /// <summary>
        /// Returns list with stations in a given project
        /// </summary>
        public List<string> ListStations(string project)
        {
            Log.Debug($"Listing stations for project {project}");

            var output = new List<string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var stations = wrapper.Add(() => projectObj.Stations);
                foreach (var station in stations)
                {
                    var stationObj = (IS7Station6)wrapper.Add(() => station);
                    output.Add(stationObj.Name);
                    Log.Debug($"Station {stationObj.Name}");
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
            Log.Debug($"Listing containers for project {project}");

            var output = new List<string>();
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var programs = wrapper.Add(() => projectObj.Programs);
                foreach (S7Program program in projectObj.Programs)
                {
                    var programObj = wrapper.Add(() => program);
                    Log.Information($"Listing containers for program {project}\\{programObj.Name}");

                    var containers = wrapper.Add(() => programObj.Next);
                    foreach (S7Container container in programObj.Next)
                    {
                        wrapper.Add(() => container);
                        output.Add(container.Name);
                        Log.Information($"Container {container.Name} ({container.ConcreteType})");
                    }
                }
            }
            return output;
        }

        #endregion

        #region Online Commands

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
            Log.Information($"[ONLINE] Downloading blocks for {project}\\{station}\\{module}\\{program}");

            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => GetProject(project));
                var programObj = wrapper.Add(() => GetProgram(project, $"{station}\\{module}\\{program}"));
                var flag = overwrite ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk;

                try
                {
                    var blocks = GetBlocks(projectObj, programObj.Name);
                    blocks.Download(flag);
                }
                catch (Exception exc)
                {
                    Log.Error(exc, $"Could not download blocks for {programObj.Name}\\{programObj.LogPath}");
                    throw;
                }
            }
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
            Log.Information($"[ONLINE] Starting program {project}\\{station}\\{module}\\{program}");

            using (var wrapper = new ReleaseWrapper())
            {
                var programObj = wrapper.Add(() => GetProgram(project, $"{station}\\{module}\\{program}"));
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
                    Log.Error(exc, $"Could not start/restart {programObj.Name}\\{programObj.LogPath}");
                    throw;
                }
            }
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
            Log.Information($"[ONLINE] Stopping program {project}\\{station}\\{module}\\{program}");

            using (var wrapper = new ReleaseWrapper())
            {
                var programObj = wrapper.Add(() => GetProgram(project, $"{station}\\{module}\\{program}"));

                try
                {
                    if (programObj.ModuleState != S7ModState.S7Stop)
                        programObj.Stop();
                }
                catch (Exception exc)
                {
                    Log.Error(exc, $"Could not stop {programObj.Name}\\{programObj.LogPath}");
                    throw;
                }
            }
        }

        #endregion

        #endregion
    }
}
