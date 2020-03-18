using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

using SimaticLib;
using S7HCOM_XLib;


namespace S7Lib
{
    public static class Api
    {
        private static Serilog.Core.Logger CreateLog()
        {
             return new LoggerConfiguration().MinimumLevel.Debug()
                .WriteTo.Console().CreateLogger();            
        }
    
        private static bool projectExists(string projectName)
        {
            var api = new Simatic();
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
            var api = new Simatic();
            var log = CreateLog();

            if (projectName.Length > 8)
            {
                log.Error($"Could not create project {projectName} in {projectDir}: " +
                          $"Name can have at most 8 characters");
                return -1;
            }

            if (projectExists(projectName))
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
            return 0;
        }

        /// <summary>
        /// Registers existing STEP 7 project given the path to its .s7p file
        /// </summary>
        /// <param name="projectFilePath">Path to STEP 7 project .s7p file</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int RegisterProject(string projectFilePath)
        {
            var api = new Simatic();
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
        /// <returns></returns>
        public static int RemoveProject(string projectName)
        {
            var api = new Simatic();
            var log = CreateLog();

            try
            {
                api.Projects.Remove(Index: projectName);
            }
            catch (Exception exc)
            {
                log.Error($"Could remove create project {projectName}:", exc);
                return -1;
            }
            return 0;
        }
    }
}
