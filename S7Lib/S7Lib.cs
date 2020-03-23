using System;
using System.IO;
using System.Collections.Generic;
using Serilog;

using SimaticLib;
using S7HCOM_XLib;


// TODO: Pass api and log to each function, for improved configurability?

namespace S7Lib
{
    /// <summary>
    /// Provides a static interface for Simatic STEP 7 API
    /// </summary>
    public static class Api
    {
        /// <summary>
        /// Returns new Simatic API handle
        /// </summary>
        /// <remarks>
        /// Regarding automaticSave, as per official documentation:
        /// If enabled, the changes are saved immediately, especially for all operations that change structure
        /// (for methods such as Add, Copy, or Remove, with which objects are added or deleted)
        /// as well as for name changes.
        /// </remarks>
        /// <param name="serverMode">UnattandedServerMode surpress GUI messages</param>
        /// <param name="automaticSave">Save project automatically</param>
        /// <returns>Handle to Simatic API</returns>
        public static Simatic CreateApi(bool serverMode=true, bool automaticSave=true)
        {
            var api = new Simatic();
            api.UnattendedServerMode = serverMode;
            api.AutomaticSave = automaticSave? 1 : 0;
            return api;
        }

        public static Serilog.Core.Logger CreateLog()
        {
             return new LoggerConfiguration().MinimumLevel.Debug()
                .WriteTo.Console().CreateLogger();            
        }
    
        private static bool ProjectExists(string projectName)
        {
            var api = CreateApi();
            try
            {
                var project = api.Projects[projectName];
                return true;
            }
            catch (System.Runtime.InteropServices.COMException) { }
            return false;
        }

        /// <summary>
        /// Internal function to create STEP 7 project or library
        /// </summary>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        /// <returns>0 on success, -1 otherwise</returns>
        private static int CreateProjectImpl(string projectName, string projectDir, S7ProjectType projectType)
        {
            var api = CreateApi();
            var log = CreateLog();

            if (projectName.Length > 8)
            {
                log.Error($"Could not create project {projectName} in {projectDir}: " +
                          $"Name can have at most 8 characters");
                return -1;
            }

            if (ProjectExists(projectName))
            {
                log.Error($"Could not create project {projectName} in {projectDir}: " +
                          $"Project exists");
                return -1;
            }

            try
            {
                api.Projects.Add(Name: projectName, ProjectRootDir: projectDir, Type: projectType);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not create project {projectName} in {projectDir}");
                return -1;
            }

            log.Debug($"Created empty project {projectName} in {projectDir}");
            return 0;
        }

        /// <summary>
        /// Create new empty STEP 7 project
        /// </summary>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int CreateProject(string projectName, string projectDir)
        {
            return CreateProjectImpl(projectName, projectDir, S7ProjectType.S7Project);
        }

        /// <summary>
        /// Create new empty STEP 7 library
        /// </summary>
        /// <param name="projectName">Library name (max 8 characters)</param>
        /// <param name="projectDir">Path to library's parent directory</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int CreateLibrary(string projectName, string projectDir)
        {
            return CreateProjectImpl(projectName, projectDir, S7ProjectType.S7Library);
        }

        /// <summary>
        /// Registers existing STEP 7 project given the path to its .s7p file
        /// </summary>
        /// <param name="projectFilePath">Path to STEP 7 project .s7p file</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int RegisterProject(string projectFilePath)
        {
            var api = CreateApi();
            var log = CreateLog();

            try
            {
                api.Projects.Add(Name: projectFilePath);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not register existing project in {projectFilePath}");
                return -1;
            }

            log.Debug($"Registered existing project from {projectFilePath}");
            return 0;
        }

        /// <summary>
        /// Removes STEP 7 project and deletes all of its files
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int RemoveProject(string projectName)
        {
            var api = CreateApi();
            var log = CreateLog();

            try
            {
                api.Projects.Remove(Index: projectName);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not remove project {projectName}");
                return -1;
            }

            log.Debug($"Removed project {projectName}");
            return 0;
        }

        private static List<string> GetSourcesFromDir(string sourcesDir)
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
        /// Import sources from a directory into a program
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory from which to import sources</param>
        /// <param name="overwrite">Force overwrite existing sources in project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSourcesDir(string project, string program, string sourcesDir, bool overwrite=true)
        {
            var api = CreateApi();
            var log = CreateLog();

            var sourceFiles = GetSourcesFromDir(sourcesDir);
            S7SWItems sourcesParent;
            try
            {
                sourcesParent = api.Projects[project].Programs[program].Next["Sources"].Next;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access sources in program {program} in project {project}");
                return -1;
            }

            foreach (var source in sourceFiles)
            {
                if (S7ProgramSource.ImportSource(parent: sourcesParent, sourceFilePath: source, overwrite: overwrite) != 0)
                {
                    log.Error($"Could not import {source} into project {project}");
                    return -1;
                }
            }

            log.Debug($"Imported sources to {project}:{program} for {sourcesDir}");
            return 0;
        }

        /// <summary>
        /// Import sources from a library into a program
        /// </summary>
        /// <param name="library">Source library name</param>
        /// <param name="project">Destination project name</param>
        /// <param name="libProgram">Source library program name</param>
        /// <param name="projProgram">Destination program name</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportLibSources(string library, string libProgram,
            string project, string projProgram, bool overwrite = true)
        {
            var api = CreateApi();
            var log = CreateLog();

            S7SWItems libSourcesParent, projSourcesParent;
            try
            {
                libSourcesParent = api.Projects[library].Programs[libProgram].Next["Sources"].Next;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access sources in program {libProgram} in library {library}");
                return -1;
            }
            try
            {
                projSourcesParent = api.Projects[project].Programs[projProgram].Next["Sources"].Next;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access sources in program {projProgram} in project {project}");
                return -1;
            }

            if (S7ProgramSource.ImportLibSources(libParent: libSourcesParent, projParent: projSourcesParent, overwrite) != 0)
            {
                log.Error($"Could not import sources from {library}:{libProgram} into {project}:{projProgram}");
                return -1;
            }

            log.Debug($"Imported sources from {library}:{libProgram} into {project}:{projProgram}");
            return 0;
        }

        /// <summary>
        /// Exports all sources from a program to a directory
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ExportSources(string project, string program, string sourcesDir)
        {
            var api = CreateApi();
            var log = CreateLog();

            S7SWItems sourcesParent;
            try
            {
                sourcesParent = api.Projects[project].Programs[program].Next["Sources"].Next;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access sources in {project}:{program}");
                return -1;
            }

            if (S7ProgramSource.ExportSources(sourcesParent, sourcesDir) != 0)
            {
                log.Error($"Could not export sources from {project}:{program} to {sourcesDir}");
            }

            log.Debug($"Exported {sourcesParent.Count} sources to {sourcesDir}");
            return 0;
        }

        /// <summary>
        /// Exports a source from a program to a directory
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ExportSource(string project, string program, string source, string sourcesDir)
        {
            var api = CreateApi();
            var log = CreateLog();

            S7Source sourceObj;
            try
            {
                sourceObj = (S7Source) api.Projects[project].Programs[program].Next["Sources"].Next[source];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access source {source} in {project}:{program}");
                return -1;
            }

            if (S7ProgramSource.ExportSource(sourceObj, sourcesDir) != 0)
            {
                log.Error($"Could not export source {source} from {project}:{program} to {sourcesDir}");
            }

            log.Debug($"Exported {source} to {sourcesDir}");
            return 0;
        }

        /// <summary>
        /// Creates a new empty S7 program
        /// </summary>
        /// <param name="project">Parent project name</param>
        /// <param name="programName">Program name</param>
        /// <returns></returns>
        static public int CreateProgram(string project, string programName)
        {
            var log = Api.CreateLog();
            var api = Api.CreateApi();

            try
            {
                api.Projects[project].Programs.Add(programName, Type: S7ProgramType.S7);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not create S7 program {programName} in {project}");
                return -1;
            }

            log.Debug($"Created S7 program {programName} in {project}");
            return 0;
        }

        /// <summary>
        /// Compile source
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        static public int CompileSource(string project, string program, string source)
        {
            return S7ProgramSource.CompileSource(project, program, source);
        }

        /// <summary>
        /// Import blocks from a directory into a project
        /// </summary>
        /// <param name="library">Source library name</param>
        /// <param name="project">Destination project name</param>
        /// <param name="libProgram">Source library program name</param>
        /// <param name="projProgram">Destination program name</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportLibBlocksDir(string library, string libProgram,
            string project, string projProgram, bool overwrite = true)
        {
            var api = CreateApi();
            var log = CreateLog();

            S7SWItems libBlocksParent, projBlocksParent;
            try
            {
                libBlocksParent = api.Projects[library].Programs[libProgram].Next["Blocks"].Next;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access blocks in program {libProgram} in library {library}");
                return -1;
            }
            try
            {
                projBlocksParent = api.Projects[project].Programs[projProgram].Next["Blocks"].Next;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access blocks in program {projProgram} in project {project}");
                return -1;
            }

            if (S7ProgramSource.ImportLibBlocks(libParent: libBlocksParent, projParent: projBlocksParent, overwrite) != 0)
            {
                log.Error($"Could not import blocks from {library}:{libProgram} into {project}:{projProgram}");
                return -1;
            }

            log.Debug($"Imported blocks from {library}:{libProgram} into {project}:{projProgram}");
            return 0;
        }

        /// <summary>
        /// Imports symbols into a program from a file
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
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
        /// <param name="allowConflicts">Succeed (return 0) even if conflits are detected</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSymbols(string project, string program, string symbolFile,
            int flag = 0, bool allowConflicts = false)
        {
            // TODO: Check file exists
            return S7Symbols.ImportSymbols(project, program, symbolFile, flag, allowConflicts);
        }

        /// <summary>
        /// Exports symbols from program from into a file
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
        /// <param name="symbolFile">Path to output symbol table file (usually .sdf)
        ///     Supported extensions .asc, .dif, .sdf, .seq
        /// </param>
        /// <param name="overwrite">Overwrite output file if it exists</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ExportSymbols(string project, string program, string symbolFile,
            bool overwrite = false)
        {
            var log = CreateLog();

            string exportDir = Path.GetDirectoryName(symbolFile);
            if (!Directory.Exists(exportDir))
            {
                log.Error($"Could not export symbols from {project}:{program}: " +
                          $"Output directory does not exist {exportDir}");
                return -1;
            }

            // TODO: Ensure output has supported extension?

            if (File.Exists(symbolFile) && !overwrite)
            {
                log.Error($"Could not export symbols from {project}:{program}: " +
                          $"Output file exists {symbolFile}");
                return -1;
            }
            else if (File.Exists(symbolFile))
            {
                log.Information($"Overwriting {symbolFile}");
            }

            return S7Symbols.ImportSymbols(project, program, symbolFile);
        }

        /// <summary>
        /// Compiles the HW configuration for each of the stations in a project
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="allowFail">Return 0 even if unable to compile some station</param>
        /// <returns></returns>
        public static int compileAllStations(string project, bool allowFail = true)
        {
            var api = CreateApi();
            var log = CreateLog();

            IS7Stations stations;
            try
            {
                stations = api.Projects[project].Stations;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access stations in project {project}");
                return -1;
            }

            foreach (var station in stations)
            {
                var stationObj = (S7Station5) station;
                try
                {
                    stationObj.Compile();
                    log.Debug($"Compiled HW config for {stationObj.Name}");
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not compile HW config for {stationObj.Name}");
                    if (!allowFail) return -1;
                }
            }

            return 0;
        }
    }
}
