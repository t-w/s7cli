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
