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
        /// Downloads all the blocks under an S7Program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Station name</param>
        /// <param name="module">Parent module name</param>
        /// <param name="program">Program name</param>
        /// <param name="overwrite">Force overwrite of online blocks</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int DownloadProgramBlocks(S7Context ctx,
            string project, string station, string module, string program, bool overwrite)
        {
            var log = ctx.Log;
            S7Project projectObj = Api.GetProject(ctx, project);
            S7Program programObj = Api.GetProgram(ctx, project, $"{station}//{module}//{program}");
            if (programObj == null) return -1;

            var flag = overwrite ? S7OverwriteFlags.S7OverwriteAll : S7OverwriteFlags.S7OverwriteAsk;

            try
            {
                var blocks = S7ProgramSource.GetBlocks(ctx, projectObj, programObj.Name);
                blocks.Download(flag);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not download blocks for {programObj.Name} {programObj.LogPath}");
                return -1;
            }

            log.Debug($"Downloaded blocks for {programObj.Name} {programObj.LogPath}");
            return 0;
        }

        /// <summary>
        /// Starts/restarts a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Station name</param>
        /// <param name="module">Parent module name</param>
        /// <param name="program">Program name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int StartProgram(S7Context ctx,
            string project, string station, string module, string program)
        {
            var log = ctx.Log;
            S7Program programObj = Api.GetProgram(ctx, project, $"{station}//{module}//{program}");
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
                log.Error(exc, $"Could not start/restart {programObj.Name} {programObj.LogPath}");
                return -1;
            }

            log.Debug($"{programObj.Name} is in {programObj.ModuleState} mode");
            return 0;
        }

        /// <summary>
        /// Stops a program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="station">Station name</param>
        /// <param name="module">Parent module name</param>
        /// <param name="program">Program name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int StopProgram(S7Context ctx,
            string project, string station, string module, string program)
        {
            var log = ctx.Log;
            S7Program programObj = Api.GetProgram(ctx, project, $"{station}//{module}//{program}");
            if (programObj == null) return -1;

            try
            {
                if (programObj.ModuleState != S7ModState.S7Stop)
                    programObj.Stop();
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not stop {programObj.Name} {programObj.LogPath}");
                return -1;
            }

            log.Debug($"{programObj.Name} is in {programObj.ModuleState} mode");
            return 0;
        }
    }

}
