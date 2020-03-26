using System;
using System.IO;
using System.Collections.Generic;

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
        private static bool ProjectExists(S7Context ctx, string projectName)
        {
            var api = ctx.Api;
            try
            {
                var project = api.Projects[projectName];
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
        public static S7Project GetProject(S7Context ctx, string project)
        {
            var api = ctx.Api;
            var log = ctx.Log;
            S7Project projectObj = null;

            try
            {
                // Detect if a path was provided
                if (Path.HasExtension(project))
                {
                    projectObj = (S7Project)api.Projects.Add(Name: project);
                }
                else
                {
                    projectObj = (S7Project)api.Projects[project];
                }
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not get project {project}");
            }

            return projectObj;
        }

        /// <summary>
        /// Internal function to create STEP 7 project or library
        /// </summary>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        /// <returns>0 on success, -1 otherwise</returns>
        private static int CreateProjectImpl(S7Context ctx,
            string projectName, string projectDir, S7ProjectType projectType)
        {
            var api = ctx.Api;
            var log = ctx.Log;

            if (projectName.Length > 8)
            {
                log.Error($"Could not create project {projectName} in {projectDir}: " +
                          $"Name can have at most 8 characters");
                return -1;
            }

            if (ProjectExists(ctx, projectName))
            {
                // Otherwise Add spawns a blocking GUI error message
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
        public static int CreateProject(S7Context ctx, string projectName, string projectDir)
        {
            return CreateProjectImpl(ctx, projectName, projectDir, S7ProjectType.S7Project);
        }

        /// <summary>
        /// Create new empty STEP 7 library
        /// </summary>
        /// <param name="projectName">Library name (max 8 characters)</param>
        /// <param name="projectDir">Path to library's parent directory</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int CreateLibrary(S7Context ctx, string projectName, string projectDir)
        {
            return CreateProjectImpl(ctx, projectName, projectDir, S7ProjectType.S7Library);
        }

        /// <summary>
        /// Registers existing STEP 7 project given the path to its .s7p file
        /// </summary>
        /// <param name="projectFilePath">Path to STEP 7 project .s7p file</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int RegisterProject(S7Context ctx, string projectFilePath)
        {
            var api = ctx.Api;
            var log = ctx.Log;

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
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int RemoveProject(S7Context ctx, string project)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;

            try
            {
                projectObj.Remove();
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not remove project {project}");
                return -1;
            }

            log.Debug($"Removed project {project}");
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
        /// Import source into a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Path to source file</param>
        /// <param name="overwrite">Force overwrite existing source in project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSource(S7Context ctx,
            string project, string program, string source, bool overwrite = true)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;
            var container = S7ProgramSource.GetSources(ctx, projectObj, program);
            if (container == null) return -1;
            
            if (S7ProgramSource.ImportSource(ctx, container: container, sourceFilePath: source, overwrite: overwrite) != 0)
            {
                log.Error($"Could not import {source} into project {project}");
                return -1;
            }
            
            log.Debug($"Imported {source} to {project}:{program}");
            return 0;
        }

        /// <summary>
        /// Import sources from a directory into a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory from which to import sources</param>
        /// <param name="overwrite">Force overwrite existing sources in project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSourcesDir(S7Context ctx,
            string project, string program, string sourcesDir, bool overwrite = true)
        {
            var log = ctx.Log;

            var sourceFiles = GetSourcesFromDir(sourcesDir);
            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;
            var container = S7ProgramSource.GetSources(ctx, projectObj, program);
            if (container == null) return -1;            

            foreach (var source in sourceFiles)
            {
                if (S7ProgramSource.ImportSource(ctx, container: container, sourceFilePath: source, overwrite: overwrite) != 0)
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
        /// <param name="library">Source library id, path to .s7l (unique) or library name</param>
        /// <param name="project">Destination project id, path to .s7p (unique) or project name</param>
        /// <param name="libProgram">Source library program name</param>
        /// <param name="projProgram">Destination program name</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportLibSources(S7Context ctx,
            string library, string libProgram, string project, string projProgram, bool overwrite = true)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;
            var libraryObj = GetProject(ctx, library);
            if (libraryObj == null) return -1;
            var libraryContainer = S7ProgramSource.GetSources(ctx, libraryObj, libProgram);
            if (libraryContainer == null) return -1;
            var projectContainer = S7ProgramSource.GetSources(ctx, projectObj, projProgram);
            if (projectContainer == null) return -1;

            if (S7ProgramSource.ImportLibSources(ctx, libSources: libraryContainer, projSources: projectContainer, overwrite) != 0)
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
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ExportAllSources(S7Context ctx,
            string project, string program, string sourcesDir)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;
            var projectSources = S7ProgramSource.GetSources(ctx, projectObj, program);
            if (projectSources == null) return -1;
        
            if (S7ProgramSource.ExportSources(ctx, projectSources, sourcesDir) != 0)
            {
                log.Error($"Could not export sources from {project}:{program} to {sourcesDir}");
            }

            log.Debug($"Exported {projectSources.Next.Count} sources to {sourcesDir}");
            return 0;
        }

        /// <summary>
        /// Exports a source from a program to a directory
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <param name="sourcesDir">Directory to which to export sources</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ExportSource(S7Context ctx,
            string project, string program, string source, string sourcesDir)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;
            var sourceObj = S7ProgramSource.GetSource(ctx, projectObj, program, source);
            if (sourceObj == null) return -1;

            if (S7ProgramSource.ExportSource(ctx, sourceObj, sourcesDir) != 0)
            {
                log.Error($"Could not export source {source} from {project}:{program} to {sourcesDir}");
            }

            log.Debug($"Exported {source} to {sourcesDir}");
            return 0;
        }

        /// <summary>
        /// Creates a new empty S7 program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programName">Program name</param>
        /// <returns></returns>
        static public int CreateProgram(S7Context ctx, string project, string programName)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;

            try
            {
                projectObj.Programs.Add(programName, Type: S7ProgramType.S7);
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
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        static public int CompileSource(S7Context ctx, string project, string program, string source)
        {
            return S7ProgramSource.CompileSource(ctx, project, program, source);
        }

        /// <summary>
        /// Import blocks from a directory into a project
        /// </summary>
        /// <param name="library">Source library id, path to .s7l (unique) or library name</param>
        /// <param name="project">Destination project id, path to .s7p (unique) or project name</param>
        /// <param name="libProgram">Source library program name</param>
        /// <param name="projProgram">Destination program name</param>
        /// <param name="overwrite">Force overwrite existing sources in destination project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportLibBlocks(S7Context ctx,
            string library, string libProgram, string project, string projProgram, bool overwrite = true)
        {
            var log = ctx.Log;

            var libraryObj = GetProject(ctx, library);
            if (libraryObj == null) return -1;
            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;
            var libraryBlocks = S7ProgramSource.GetBlocks(ctx, libraryObj, libProgram);
            if (libraryBlocks == null) return -1;
            var projectBlocks = S7ProgramSource.GetBlocks(ctx, projectObj, projProgram);
            if (projectBlocks == null) return -1;

            if (S7ProgramSource.ImportLibBlocks(ctx,
                libBlocks: libraryBlocks, projBlocks: projectBlocks, overwrite) != 0)
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
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
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
        public static int ImportSymbols(S7Context ctx,
            string project, string program, string symbolFile, int flag = 0, bool allowConflicts = false)
        {
            // TODO: Check file exists
            return S7Symbols.ImportSymbols(ctx, project, program, symbolFile, flag, allowConflicts);
        }

        /// <summary>
        /// Exports symbols from program from into a file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="symbolFile">Path to output symbol table file (usually .sdf)
        ///     Supported extensions .asc, .dif, .sdf, .seq
        /// </param>
        /// <param name="overwrite">Overwrite output file if it exists</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ExportSymbols(S7Context ctx,
            string project, string program, string symbolFile, bool overwrite = false)
        {
            var log = ctx.Log;

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

            return S7Symbols.ExportSymbols(ctx, project, program, symbolFile);
        }

        /// <summary>
        /// Compiles the HW configuration for each of the stations in a project
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="allowFail">Return 0 even if unable to compile some station</param>
        /// <returns></returns>
        public static int CompileAllStations(S7Context ctx, string project, bool allowFail = true)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;

            IS7Stations stations;
            try
            {
                stations = projectObj.Stations;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access stations in project {project}");
                return -1;
            }

            foreach (var station in stations)
            {
                var stationObj = (S7Station5)station;
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

        // List commands

        /// <summary>
        /// Creates List with project name, project path key-value pairs
        /// </summary>
        /// <param name="output">List with project name, project path key-value pairs</param>
        /// <returns>0 on success</returns>
        public static int ListProjects(S7Context ctx, ref List<KeyValuePair<string, string>> output)
        {
            var api = ctx.Api;
            var log = ctx.Log;

            log.Information($"Listing registered projects");
            foreach (var project in api.Projects)
            {
                var projectObj = (S7Project)project;
                var kv = new KeyValuePair<string, string>(projectObj.Name, projectObj.LogPath);
                output.Add(kv);
                log.Information($"Project {kv.Key} Path {kv.Value}");
            }

            return 0;
        }

        /// <summary>
        /// Creates List with programs in a given project
        /// </summary>
        /// <param name="output">List with program names</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ListPrograms(S7Context ctx, ref List<string> output, string project)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;

            log.Information($"Listing programs for project {project}");
            foreach (var program in projectObj.Programs)
            {
                // TODO: If the cast to S7Program is safe, remove try catch block
                try
                {
                    var programObj = (S7Program)program;
                    output.Add(programObj.Name);
                    log.Information($"Program {programObj.Name}");
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not access program in project {project}");
                }
            }
            return 0;
        }

        /// <summary>
        /// Creates List with stations in a given project
        /// </summary>
        /// <param name="output">List with station names</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ListStations(S7Context ctx, ref List<string> output, string project)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;

            log.Information($"Listing stations for project {project}");
            foreach (var station in projectObj.Stations)
            {
                // TODO: If the cast to S7Station is safe, remove try catch block
                try
                {
                    var stationObj = (S7Station)station;
                    output.Add(stationObj.Name);
                    log.Information($"Station {stationObj.Name} ({stationObj.Type})");
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not access station in project {project}");
                }
            }
            return 0;
        }

        /// <summary>
        /// Creates List with containers for each program in a given project
        /// </summary>
        /// TODO: Maybe include program name in output as well?
        /// <param name="output">List with conteiner names</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ListContainers(S7Context ctx, ref List<string> output, string project)
        {
            var log = ctx.Log;

            var projectObj = GetProject(ctx, project);
            if (projectObj == null) return -1;

            log.Debug($"Listing containers for project {project}");
            foreach (var program in projectObj.Programs)
            {
                S7Program programObj;
                // TODO: If the cast to S7Program is safe, remove try catch block
                try
                {
                    programObj = (S7Program)program;
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not access program in project {project}");
                    continue;
                }
                log.Information($"Listing containers for program {project}:{programObj.Name}");
                // TODO: If the cast to S7Container is safe, remove try catch block                
                foreach (var container in programObj.Next)
                {
                    S7Container containerObj;
                    try
                    {
                        containerObj = (S7Container)container;
                        output.Add(containerObj.Name);
                        log.Information($"Container {containerObj.Name} ({containerObj.ConcreteType})");
                    }
                    catch (Exception exc)
                    {
                        log.Error(exc, $"Could not access container in {programObj.Name}");
                    }
                }
            }

            return 0;
        }
    }
}
