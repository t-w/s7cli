using System;

using SimaticLib;
using S7HCOM_XLib;


namespace S7Lib
{
    /// <summary>
    /// Contains methods that require a real connection to a PLC
    /// </summary>
    public static class Online
    {
        /// <summary>
        /// Obtain S7 program from its child module
        /// </summary>
        /// <remarks>
        /// The only way to uniquely identify a program is to specify its child module.
        /// A program name may not be unique.
        /// To understand the structure of an existing project use SimTree API demo.
        /// It shows a tree in a GUI and highlights the type of each element in it
        /// </remarks>
        /// <param name="project">Project name</param>
        /// <param name="station">Station name</param>
        /// <param name="rack">Rack name</param>
        /// <param name="module">Child module name</param>
        /// <returns>S7Program on success, null otherwise</returns>
        private static S7Program GetProgram(S7Context ctx,
            string project, string station, string rack, string module)
        {
            var log = ctx.Log;
            var projectObj = Api.GetProject(ctx, project);
            if (projectObj == null) return null;

            IS7Station6 stationObj;
            try
            {
                stationObj = projectObj.Stations[station];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not find station {projectObj.Name}:{station}");
                return null;
            }
            IS7Module moduleObj;
            try
            {
                moduleObj = stationObj.Racks[rack].Modules[module];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not find module {module} in {project}:{station}:{rack}");
                return null;
            }
            S7Program programObj;
            try
            {
                programObj = (S7Program)projectObj.Programs[moduleObj];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not find program for module {module}");
                return null;
            }
            return programObj;
        }

        /// <summary>
        /// Downloads all the blocks under an S7Program
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="station">Station name</param>
        /// <param name="rack">Rack name</param>
        /// <param name="module">Child module name</param>
        /// <param name="overwrite">Force overwrite of online blocks</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int DownloadProgramBlocks(S7Context ctx,
            string project, string station, string rack, string module, bool overwrite)
        {
            var log = ctx.Log;

            S7Program programObj = GetProgram(ctx, project, station, rack, module);
            if (programObj == null) return -1;

            var flag = overwrite ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk;

            try
            {
                var blocks = (S7Container)programObj.Next["Blocks"].Next;
                blocks.Download(flag);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not download blocks for {programObj.Name} " +
                               $"{project}:{station}:{rack}:{module}");
                return -1;
            }

            log.Debug($"Downloaded blocks for {programObj.Name} " +
                      $"{project}:{station}:{rack}:{module}");
            return 0;
        }

        /// <summary>
        /// Starts/restarts a program
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="station">Station name</param>
        /// <param name="rack">Rack name</param>
        /// <param name="module">Child module name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int StartProgram(S7Context ctx,
            string project, string station, string rack, string module)
        {
            var log = ctx.Log;

            S7Program programObj = GetProgram(ctx, project, station, rack, module);
            if (programObj == null) return -1;

            try
            {
                if (programObj.ModuleState != S7ModState.S7Run)
                {
                    programObj.NewStart();
                }
                else
                {
                    log.Debug($"{programObj.Name} is already in RUN mode. Restarting.");
                    programObj.Restart();
                }
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not start/restart {programObj.Name} " +
                               $"{project}:{station}:{rack}:{module}");
                return -1;
            }

            log.Debug($"{programObj.Name} is in {programObj.ModuleState} mode " +
                      $"{project}:{station}:{rack}:{module}");
            return 0;
        }

        /// <summary>
        /// Stops a program
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="station">Station name</param>
        /// <param name="rack">Rack name</param>
        /// <param name="module">Child module name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int StopProgram(S7Context ctx,
            string project, string station, string rack, string module)
        {
            var log = ctx.Log;

            S7Program programObj = GetProgram(ctx, project, station, rack, module);
            if (programObj == null) return -1;

            try
            {
                programObj.Stop();
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not stop {programObj.Name} " +
                               $"{project}:{station}:{rack}:{module}");
                return -1;
            }

            log.Debug($"{programObj.Name} is in {programObj.ModuleState} mode " +
                      $"{project}:{station}:{rack}:{module}");
            return 0;
        }
    }

}
