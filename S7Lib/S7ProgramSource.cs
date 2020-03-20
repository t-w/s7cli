using System;
using System.IO;
using System.Text.RegularExpressions;

using SimaticLib;


namespace S7Lib
{
    class S7ProgramSource
    {
        /// <summary>
        /// Imports source into project
        /// </summary>
        /// <param name="parent">Parent S7SWItem container object</param>
        /// <param name="sourceFilePath">Path to source file</param>
        /// <param name="sourceType">SW object type</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSource(S7SWItems parent, string sourceFilePath,
            S7SWObjType sourceType = S7SWObjType.S7Source, bool overwrite = true)
        {
            var log = Api.CreateLog();
            string sourceName = Path.GetFileNameWithoutExtension(sourceFilePath);

            IS7SWItem source = null;
            // Check if source is already present
            try
            {
                source = parent[sourceName];
            }
            catch (Exception) { }

            if (source != null && !overwrite)
            {
                log.Error($"Could not import {sourceName} from {sourceFilePath}: " +
                          $"Source with the same name exists.");
                return -1;
            }
            else if (source != null && overwrite)
            {
                log.Debug($"{sourceName} already exists. Overwriting.");
                try
                {
                    parent.Remove(sourceName);
                }
                catch (Exception exc)
                {
                    log.Error($"Could not remove existing source {sourceName}: ", exc);
                    return -1;
                }
            }

            try
            {
                var item = parent.Add(sourceName, sourceType, sourceFilePath);
            }
            catch (Exception exc)
            {
                log.Error($"Could not import source {sourceName} ({sourceType}) " +
                          $"from {sourceFilePath}: ", exc);
                return -1;
            }
            log.Debug($"Imported source {sourceName} ({sourceType}) from {sourceFilePath}");
            return 0;
        }

        // TODO: Reduce code duplication
        /// <summary>
        /// Copies S7Source to destination S7SWItems container
        /// </summary>
        /// <param name="source">Target source to copy</param>
        /// <param name="destination">Target container onto which to copy source</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        /// <returns>0 on success, -1 otherwise</returns>
        private static int CopySource(S7Source source, S7SWItems destination, bool overwrite = true)
        {
            var log = Api.CreateLog();
            var sourceName = source.Name;
            var sourceType = source.ConcreteType;

            IS7SWItem destSource = null;
            // Check if source is already present
            try
            {
                destSource = destination[sourceName];
            }
            catch (Exception) { }

            if (source != null && !overwrite)
            {
                log.Error($"Could not import {sourceName} from library: " +
                          $"Source with the same name exists.");
                return -1;
            }
            else if (source != null && overwrite)
            {
                log.Debug($"{sourceName} already exists. Overwriting.");
                try
                {
                    destination.Remove(sourceName);
                }
                catch (Exception exc)
                {
                    log.Error($"Could not remove existing source {sourceName}: ", exc);
                    return -1;
                }
            }

            try
            {
                var item = source.Copy(destination);
            }
            catch (Exception exc)
            {
                log.Error($"Could not import source {sourceName} ({sourceType}) " +
                          $"from library: ", exc);
                return -1;
            }
            log.Debug($"Imported source {sourceName} ({sourceType}) from library");
            return 0;
        }

        /// <summary>
        /// Import sources from library into project 
        /// </summary>
        /// <param name="libParent">Source library container from which to copy source</param>
        /// <param name="projParent">Target project container onto which to copy source</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportLibSources(S7SWItems libParent, S7SWItems projParent, bool overwrite = true)
        {
            foreach (S7Source libSource in libParent)
            {
                if (CopySource(libSource, projParent, overwrite) != 0)
                    return -1;
            }
            return 0;
        }

        /// <summary>
        /// Compile source
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourceName">Source name</param>
        /// <returns></returns>
        public static int CompileSource(string project, string program, string sourceName)
        {
            var api = Api.CreateApi();
            var log = Api.CreateLog();
            S7Source source;

            try
            {
                source = (S7Source) api.Projects[project].Programs[program].Next["Sources"].Next[sourceName];
            }
            catch (Exception exc)
            {
                log.Error($"Could not find source {sourceName} in project {project} program {program}: ", exc);
                return -1;
            }

            var sourceType = source.ConcreteType;
            if (sourceType == S7SourceType.S7SCL || sourceType == S7SourceType.S7SCLMake)
            {
                return CompileSclSource(source);
            }
            else if (sourceType == S7SourceType.S7AWL)
            {
                return CompileAwlSource(source);
            }

            try
            {
                source.Compile();
            }
            catch (Exception exc)
            {
                log.Error($"Could not compile source {sourceName} in project {project} program {program}: ", exc);
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Compile .SCL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        /// <returns>TODO: Improve return codes; for now 0 on success, -1 otherwise</returns>
        private static int CompileSclSource(S7Source src)
        {
            var log = Api.CreateLog();
            try
            {
                IS7SWItems items = src.Compile();
            }
            catch (Exception exc)
            {
                log.Error($"Error compiling {src.Name}: ", exc);
                return -1;
            }

            // get status and close the SCL compiler
            S7CompilerSCL compiler = new S7CompilerSCL();
            log.Information($"SCL status buffer: {compiler.getSclStatusBuffer()}");
            string statusLine = compiler.getSclStatusLine();
            int errors = compiler.getErrorCount();
            int warnings = compiler.getWarningCount();
            compiler.closeSclWindow();

            if (errors > 0)
            {
                log.Warning($"Could not compile {src.Name}: {errors} error(s)");
                return -1;
            }
            else if (warnings > 0)
            {
                log.Warning($"Compiled {src.Name} with {warnings} warning(s)");
                return 0;
            }
            return 0;
        }

        /// <summary>
        /// Compile .AWL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        /// <returns>TODO: Improve return codes; for now 0 on success, -1 otherwise</returns>
        public static int CompileAwlSource(S7Source src)
        {
            var log = Api.CreateLog();
            var api = Api.CreateApi();

            // special setting for STL compilation, CRG-1417
            // ("quiet" compilation with status written to a log file)
            var verbLogFile = Path.GetTempFileName();
            api.VerbLogFile = verbLogFile;
            log.Debug($"Set Simatic.VerbLogFile to {api.VerbLogFile}");

            // truncate log file
            FileStream oStream = new FileStream(verbLogFile, FileMode.Open, FileAccess.ReadWrite);
            oStream.SetLength(0);
            oStream.Close();

            try
            {
                IS7SWItems items = src.Compile();
            }
            catch (Exception exc)
            {
                log.Error($"Error compiling {src.Name}: ", exc);
                if (!File.Exists(verbLogFile))
                {
                    log.Error($"Compilation log file not found {verbLogFile}");
                    return -2;
                }
            }

            // read and show the log file
            string[] logfile = File.ReadAllLines(verbLogFile);
            Array.ForEach<string>(logfile, s => log.Information(s));
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
                log.Error($"Could not retrieve compilation result from {verbLogFile}");
                return -1;
            }

            if (errors > 0)
            {
                log.Warning($"Could not compile {src.Name}: {errors} error(s)");
                return -1;
            }
            else if (warnings > 0)
            {
                log.Warning($"Compiled {src.Name} with {warnings} warning(s)");
                return 0;
            }
            return 0;
        }
    }
}
