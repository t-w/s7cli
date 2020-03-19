using System;
using System.Collections.Generic;
using Serilog;

using SimaticLib;
using S7HCOM_XLib;


namespace S7Lib
{
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
        /// <returns></returns>
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
        /// Create new empty STEP 7 project
        /// </summary>
        /// <param name="projectName">Project name (max 8 characters)</param>
        /// <param name="projectDir">Path to project's parent directory</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int CreateProject(string projectName, string projectDir)
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
                api.Projects.Add(Name: projectName, ProjectRootDir: projectDir);
            }
            catch (Exception exc)
            {
                log.Error($"Could not create project {projectName} in {projectDir}:", exc);
                return -1;
            }
            log.Debug($"Created empty project {projectName} in {projectDir}");
            return 0;
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
                log.Error($"Could not register existing project in {projectFilePath}:", exc);
                return -1;
            }
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
                log.Error($"Could not remove project {projectName}:", exc);
                return -1;
            }
            return 0;
        }

        private static List<string> GetSourcesFromDir(string sourcesDir)
        {
            var sourceFiles = new List<string>();
            string[] supportedExtensions = { "*.SCL", "*.AWL", "*.INP", "*.GR7" };
            foreach (string ext in supportedExtensions)
                sourceFiles.AddRange(
                    System.IO.Directory.GetFiles(sourcesDir, ext,
                        System.IO.SearchOption.TopDirectoryOnly));
            return sourceFiles;
        }

        /// <summary>
        /// Import sources from a directory into a project
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourcesDir">Directory from which to import sources</param>
        /// <param name="force">Force overwrite existing sources in project</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSourcesDir(string project, string program, string sourcesDir, bool force=true)
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
                log.Error($"Could not access program {program} in project {project}:", exc);
                return -1;
            }

            foreach (var source in sourceFiles)
            {
                if (S7ProgramSource.ImportSource(parent: sourcesParent, sourceFilePath: source) != 0)
                {
                    log.Error($"Could not import {source} into project {project}");
                    return -1;
                }
            }

            return 0;
        }

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
                log.Error($"Could not create S7 program {programName} in {project}: ", exc);
                return -1;
            }
            log.Debug($"Created S7 program {programName} in {project}");
            return 0;
        }

        static public int CompileSource(string project, string program, string source)
        {
            return S7ProgramSource.CompileSource(project, program, source);
        }
    }
}
