using System;
using System.IO;
using System.Text.RegularExpressions;

using SimaticLib;


namespace S7Lib
{
    /// <summary>
    /// Contains methods related to S7 program sources
    /// </summary>
    public static class S7ProgramSource
    {
        public static S7Container GetContainer(S7Context ctx, S7Project projectObj, string program, S7ContainerType type)
        {
            var log = ctx.Log;

            foreach (var container in projectObj.Programs[program].Next)
            {
                try
                {
                    var containerObj = (S7Container)container;
                    if (containerObj.ConcreteType == type)
                    {
                        return containerObj;
                    }
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not access container in {projectObj.Name}:{program}");
                }
            }
            log.Error($"Could not find container of type {type} in {projectObj.Name}:{program}");
            return null;
        }

        public static S7Container GetSources(S7Context ctx, S7Project projectObj, string program)
        {
            return GetContainer(ctx, projectObj, program, S7ContainerType.S7SourceContainer);
        }

        public static S7Container GetBlocks(S7Context ctx, S7Project projectObj, string program)
        {
            return GetContainer(ctx, projectObj, program, S7ContainerType.S7BlockContainer);
        }


        /// <summary>
        /// Gets a S7Source object from a program
        /// </summary>
        /// <remarks>The source container may not be named "Sources"</remarks>
        /// <param name="projectObj">S7Project object</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <returns></returns>
        public static S7Source GetSource(S7Context ctx, S7Project projectObj, string program, string source)
        {
            var log = ctx.Log;
            S7Source sourceObj = null;
            var container = GetSources(ctx, projectObj, program);
            if (container == null) return null;

            try
            {
                sourceObj = (S7Source)container.Next[source];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not find source {source} in {projectObj.Name}:{program}");
            }
            return sourceObj;
        }

        /// <summary>
        /// Gets a S7Block object from a program
        /// </summary>
        /// <remarks>The block container may not be named "Blocks"</remarks>
        /// <param name="projectObj">S7Project object</param>
        /// <param name="program">Program name</param>
        /// <param name="block">Block name</param>
        /// <returns></returns>
        public static S7Block GetBlock(S7Context ctx, S7Project projectObj, string program, string block)
        {
            var log = ctx.Log;
            S7Block blockObj = null;
            var container = GetBlocks(ctx, projectObj, program);
            if (container == null) return null;

            try
            {
                blockObj = (S7Block)container.Next[block];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not find block {block} in {projectObj.Name}:{program}");
            }
            return blockObj;
        }

        /// <summary>
        /// Imports source into program
        /// </summary>
        /// <param name="container">Parent container object</param>
        /// <param name="sourceFilePath">Path to source file</param>
        /// <param name="sourceType">SW object type</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSource(S7Context ctx,
            S7Container container, string sourceFilePath, S7SWObjType sourceType = S7SWObjType.S7Source,
            bool overwrite = true)
        {
            var log = ctx.Log;
            string sourceName = Path.GetFileNameWithoutExtension(sourceFilePath);

            IS7SWItem source = null;
            // Check if source is already present
            try
            {
                source = container.Next[sourceName];
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
                    container.Next.Remove(sourceName);
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not remove existing source {sourceName}");
                    return -1;
                }
            }

            try
            {
                var item = container.Next.Add(sourceName, sourceType, sourceFilePath);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import source {sourceName} ({sourceType}) from {sourceFilePath}");
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
        private static int CopySource(S7Context ctx,
            S7Source source, S7Container destination, bool overwrite = true)
        {
            var log = ctx.Log;
            var sourceName = source.Name;
            var sourceType = source.ConcreteType;

            IS7SWItem destSource = null;
            // Check if source is already present
            try
            {
                destSource = destination.Next[sourceName];
            }
            catch (Exception) { }

            if (destSource != null && !overwrite)
            {
                log.Error($"Could not import {sourceName} from library: " +
                          $"Source with the same name exists.");
                return -1;
            }
            else if (destSource != null && overwrite)
            {
                log.Debug($"{sourceName} already exists. Overwriting.");
                try
                {
                    destination.Next.Remove(sourceName);
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not remove existing source {sourceName}");
                    return -1;
                }
            }

            try
            {
                var item = source.Copy(destination);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import source {sourceName} ({sourceType}) from library: ");
                return -1;
            }

            log.Debug($"Imported source {sourceName} ({sourceType}) from library");
            return 0;
        }

        /// <summary>
        /// Imports sources from library into project 
        /// </summary>
        /// <param name="libSources">Source library container from which to copy source</param>
        /// <param name="projSources">Target project container onto which to copy source</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportLibSources(S7Context ctx,
            S7Container libSources, S7Container projSources, bool overwrite = true)
        {
            foreach (S7Source libSource in libSources.Next)
            {
                if (CopySource(ctx, libSource, projSources, overwrite) != 0)
                    return -1;
            }
            return 0;
        }

        /// <summary>
        /// Exports source to output directory
        /// </summary>
        /// <remarks>Output file will be named {sourceName}.{sourceType}</remarks>
        /// <param name="source">S7Source object</param>
        /// <param name="exportDir">Output directory</param>
        /// <returns></returns>
        public static int ExportSource(S7Context ctx, S7Source source, string exportDir)
        {
            var log = ctx.Log;
            var sourceType = source.ConcreteType;
            string outputFile = Path.Combine(exportDir, source.Name);

            try
            {
                source.Export(outputFile);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not export source {source.Name} ({sourceType}) to {outputFile}");
                return -1;
            }

            log.Debug($"Exported {source.Name} ({sourceType}) to {outputFile}");
            return 0;
        }

        /// <summary>
        /// Exports all sources from a program to a directory
        /// </summary>
        /// <param name="sources">Parent sources container object</param>
        /// <param name="exportDir">Path to output source dir</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ExportSources(S7Context ctx, S7Container sources, string exportDir)
        {
            foreach (S7Source source in sources.Next)
            {
                if (ExportSource(ctx, source, exportDir) != 0)
                    return -1;
            }
            return 0;
        }

        /// <summary>
        /// Compiles source
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourceName">Source name</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int CompileSource(S7Context ctx, string project, string program, string sourceName)
        {
            var log = ctx.Log;

            var projectObj = Api.GetProject(ctx, project);
            if (projectObj == null) return -1;
            var source = GetSource(ctx, projectObj, program, sourceName);
            if (source == null) return -1;

            var sourceType = source.ConcreteType;
            if (sourceType == S7SourceType.S7SCL || sourceType == S7SourceType.S7SCLMake)
            {
                return CompileSclSource(ctx, source);
            }
            else if (sourceType == S7SourceType.S7AWL)
            {
                return CompileAwlSource(ctx, source);
            }

            try
            {
                source.Compile();
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not compile source {sourceName} in project {project} program {program}");
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Compiles .SCL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        /// <returns>TODO: Improve return codes; for now 0 on success, -1 otherwise</returns>
        private static int CompileSclSource(S7Context ctx, S7Source src)
        {
            var log = ctx.Log;
            try
            {
                IS7SWItems items = src.Compile();
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Error compiling {src.Name}");
                return -1;
            }

            // get status and close the SCL compiler
            S7CompilerSCL compiler = new S7CompilerSCL(ctx);
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
        /// Compiles .AWL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        /// <returns>TODO: Improve return codes; for now 0 on success, -1 otherwise</returns>
        public static int CompileAwlSource(S7Context ctx, S7Source src)
        {
            var log = ctx.Log;
            var api = ctx.Api;

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
                log.Error(exc, $"Error compiling {src.Name}");
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

        // TODO: Reduce code duplication
        /// <summary>
        /// Copies S7Block to destination S7SWItems container
        /// </summary>
        /// <param name="block">Target block to copy</param>
        /// <param name="destination">Target container onto which to copy block</param>
        /// <param name="overwrite">Overwrite existing block if present</param>
        /// <returns>0 on success, -1 otherwise</returns>
        private static int CopyBlock(S7Context ctx,
            S7Block block, S7Container destination, bool overwrite = true)
        {
            var log = ctx.Log;
            var blockName = block.Name;
            var blockType = block.ConcreteType;

            IS7SWItem destBlock = null;
            // Check if block is already present
            try
            {
                destBlock = destination.Next[blockName];
            }
            catch (Exception) { }

            if (block != null && !overwrite)
            {
                log.Error($"Could not import {blockName} from library: " +
                          $"block with the same name exists.");
                return -1;
            }
            else if (block != null && overwrite)
            {
                log.Debug($"{blockName} already exists. Overwriting.");
                try
                {
                    destination.Next.Remove(blockName);
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not remove existing block {blockName}");
                    return -1;
                }
            }

            try
            {
                var item = block.Copy(destination);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import block {blockName} ({blockType}) from library: ");
                return -1;
            }

            log.Debug($"Imported block {blockName} ({blockType}) from library");
            return 0;
        }

        /// <summary>
        /// Imports blocks from library into project 
        /// </summary>
        /// <param name="libBlocks">Source library container from which to copy block</param>
        /// <param name="projBlocks">Target project container onto which to copy block</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportLibBlocks(S7Context ctx,
            S7Container libBlocks, S7Container projBlocks, bool overwrite = true)
        {
            var log = ctx.Log;
            foreach (S7Block libBlock in libBlocks.Next)
            {
                // Note: "System data" blocks to not have SymbolicName attribute
                if (libBlock.Name == "System data")
                {
                    log.Debug("Cannot copy System data block. Skipping."); 
                    continue;
                }
                if (CopyBlock(ctx, libBlock, projBlocks, overwrite) != 0)
                    return -1;
            }
            return 0;
        }
    }
}
