using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using SimaticLib;


namespace S7Lib
{
    /// <summary>
    /// Contains methods related to S7 program sources
    /// </summary>
    public static class S7ProgramSource
    {
        internal static S7Container GetContainer(S7Handle s7Handle, S7Project projectObj, string program, S7ContainerType type)
        {
            var log = s7Handle.Log;
            IS7Program programObj;

            using (var wrapper = new ReleaseWrapper())
            {
                var programs = projectObj.Programs;
                wrapper.Add(() => programs);
                try
                {
                    programObj = wrapper.Add(() => programs[program]);
                    //programObj = programs[program];
                    //wrapper.Add(() => programObj);
                }
                catch (COMException exc)
                {
                    throw new KeyNotFoundException($"Could not access program {projectObj.Name}:{program}", exc);
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
            throw new KeyNotFoundException($"Could not find container of type {type} in {projectObj.Name}:{program}");
        }

        internal static S7Container GetSources(S7Handle s7Handle, S7Project projectObj, string program)
        {
            return GetContainer(s7Handle, projectObj, program, S7ContainerType.S7SourceContainer);
        }

        internal static S7Container GetBlocks(S7Handle s7Handle, S7Project projectObj, string program)
        {
            return GetContainer(s7Handle, projectObj, program, S7ContainerType.S7BlockContainer);
        }


        /// <summary>
        /// Gets a S7Source object from a program
        /// </summary>
        /// <remarks>The source container may not be named "Sources"</remarks>
        /// <param name="projectObj">S7Project object</param>
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <returns>S7 Source</returns>
        internal static S7Source GetSource(S7Handle s7Handle, S7Project projectObj, string program, string source)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var container = wrapper.Add(() => GetSources(s7Handle, projectObj, program));
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
        internal static S7Block GetBlock(S7Handle s7Handle, S7Project projectObj, string program, string block)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var container = wrapper.Add(() => GetBlocks(s7Handle, projectObj, program));
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

        // Helper function to search for a source in a container.
        // If not found, simply return. If found and !remove it throws and error.
        // If found and remove then it removes the existing source.
        private static void ImportSourceHelper(S7Handle s7Handle, S7Container container, string sourceName, bool overwrite = true)
        {
            var log = s7Handle.Log;

            IS7SWItem source;
            S7SWItems swItems = null;

            try
            {
                swItems = container.Next;
                source = swItems[sourceName];
            }
            catch (COMException)
            {
                log.Debug($"Source {sourceName} not found in container {container.Name}");
                return;
            }
            finally
            {
                Marshal.FinalReleaseComObject(swItems);
            }

            if (source != null && !overwrite)
            {
                throw new ArgumentException($"Source {sourceName} already exists and overwrite is disabled.",
                                            nameof(overwrite));
            }
            else if (source != null && overwrite)
            {
                log.Debug($"{sourceName} already exists. Overwriting.");
                try
                {
                    swItems = container.Next;
                    swItems.Remove(sourceName);
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not remove existing source {sourceName}");
                    throw;
                }
                finally
                {
                    Marshal.FinalReleaseComObject(swItems);
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
        internal static void ImportSource(S7Handle s7Handle,
            S7Container container, string sourceFilePath, S7SWObjType sourceType = S7SWObjType.S7Source,
            bool overwrite = true)
        {
            var log = s7Handle.Log;
            string sourceName = Path.GetFileNameWithoutExtension(sourceFilePath);
            S7SWItems swItems = null;

            log.Debug($"Imported source {sourceName} ({sourceType}) from {sourceFilePath}");

            ImportSourceHelper(s7Handle, container, sourceName, overwrite);

            try
            {
                swItems = container.Next;
                swItems.Add(sourceName, sourceType, sourceFilePath);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import source {sourceName} ({sourceType}) from {sourceFilePath}");
                throw;
            }
            finally
            {
                Marshal.FinalReleaseComObject(swItems);
            }
        }

        /// <summary>
        /// Copies S7Source to destination S7SWItems container
        /// </summary>
        /// <param name="source">Target source to copy</param>
        /// <param name="destination">Target container onto which to copy source</param>
        /// <param name="overwrite">Overwrite existing source if present</param>
        private static void CopySource(S7Handle s7Handle,
            S7Source source, S7Container destination, bool overwrite = true)
        {
            var log = s7Handle.Log;
            var sourceName = source.Name;
            var sourceType = source.ConcreteType;

            log.Debug($"Importing source {sourceName} ({sourceType}) from library");

            ImportSourceHelper(s7Handle, destination, sourceName, overwrite);

            try
            {
                source.Copy(destination);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import source {sourceName} ({sourceType}) from library: ");
                throw;
            }
        }

        /// See S7Handle.ImportLibSources
        internal static void ImportLibSources(S7Handle s7Handle,
            S7Container libSources, S7Container projSources, bool overwrite = true)
        {
            foreach (S7Source libSource in libSources.Next)
            {
                CopySource(s7Handle, libSource, projSources, overwrite);
            }
        }

        /// See S7Handle.ExportSource
        internal static void ExportSource(S7Handle s7Handle, S7Source source, string exportDir)
        {
            var log = s7Handle.Log;
            var sourceType = source.ConcreteType;
            string outputFile = Path.Combine(exportDir, source.Name);

            try
            {
                source.Export(outputFile);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not export source {source.Name} ({sourceType}) to {outputFile}");
                throw;
            }

            log.Debug($"Exported {source.Name} ({sourceType}) to {outputFile}");
        }

        /// See S7Handle.ExportAllSources
        internal static void ExportSources(S7Handle s7Handle, S7Container sources, string exportDir)
        {
            foreach (S7Source source in sources.Next)
            {
                ExportSource(s7Handle, source, exportDir);
            }
        }

        /// <summary>
        /// Compiles source
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="program">Program name</param>
        /// <param name="sourceName">Source name</param>
        public static void CompileSource(S7Handle s7Handle, string project, string program, string sourceName)
        {
            var log = s7Handle.Log;

            var projectObj = s7Handle.GetProject(project);
            var source = GetSource(s7Handle, projectObj, program, sourceName);
            var sourceType = source.ConcreteType;

            try
            {
                if (sourceType == S7SourceType.S7SCL || sourceType == S7SourceType.S7SCLMake)
                {
                    CompileSclSource(s7Handle, source);
                }
                else if (sourceType == S7SourceType.S7AWL)
                {
                    CompileAwlSource(s7Handle, source);
                }
                else
                {
                    try
                    {
                        source.Compile();
                    }
                    catch (Exception exc)
                    {
                        log.Error(exc, $"Could not compile source {sourceName} in project {project} program {program}");
                        throw;
                    }
                }
            }
            finally
            {

            }
        }

        /// <summary>
        /// Compiles .SCL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        /// TODO Maybe extract error and warning count?
        private static void CompileSclSource(S7Handle s7Handle, S7Source src)
        {
            var log = s7Handle.Log;
            IS7SWItems items = null;

            try
            {
                items = src.Compile();
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Error compiling {src.Name}");
                throw;
            }
            finally
            {
                if (items != null)
                    Marshal.FinalReleaseComObject(items);
            }

            // get status and close the SCL compiler
            S7CompilerSCL compiler = new S7CompilerSCL(s7Handle);
            log.Debug($"SCL status buffer:\n{compiler.getSclStatusBuffer()}");
            //string statusLine = compiler.getSclStatusLine();
            int errors = compiler.getErrorCount();
            int warnings = compiler.getWarningCount();
            compiler.closeSclWindow();

            if (errors > 0)
            {
                throw new Exception($"Could not compile {src.Name}: {errors} error(s)");
            }
            else if (warnings > 0)
            {
                log.Warning($"Compiled {src.Name} with {warnings} warning(s)");
            }
        }

        /// <summary>
        /// Compiles .AWL source
        /// </summary>
        /// <param name="src">STEP 7 source object</param>
        public static void CompileAwlSource(S7Handle s7Handle, S7Source src)
        {
            var log = s7Handle.Log;
            var api = s7Handle.Api;

            // special setting for STL compilation, CRG-1417
            // ("quiet" compilation with status written to a log file)
            var verbLogFile = Path.GetTempFileName();
            api.VerbLogFile = verbLogFile;
            log.Debug($"Set Simatic.VerbLogFile to {api.VerbLogFile}");

            // truncate log file
            FileStream oStream = new FileStream(verbLogFile, FileMode.Open, FileAccess.ReadWrite);
            oStream.SetLength(0);
            oStream.Close();

            IS7SWItems items = null;

            try
            {
                items = src.Compile();
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Error compiling {src.Name}");
                if (!File.Exists(verbLogFile))
                {
                    log.Error($"Compilation log file not found {verbLogFile}");
                }
                throw;
            }
            finally
            {
                if (items != null)
                    Marshal.FinalReleaseComObject(items);
            }

            // read and show the log file
            string[] logfile = File.ReadAllLines(verbLogFile);
            Array.ForEach<string>(logfile, s => log.Debug(s));
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
                throw new Exception($"Could not retrieve compilation result from {verbLogFile}");
            }

            if (errors > 0)
            {
                // TODO Improve exception?
                throw new Exception($"Could not compile {src.Name}: {errors} error(s)");
            }
            else if (warnings > 0)
            {
                log.Warning($"Compiled {src.Name} with {warnings} warning(s)");
            }
        }

        /// <summary>
        /// Copies S7Block to destination S7SWItems container
        /// </summary>
        /// <param name="block">Target block to copy</param>
        /// <param name="destination">Target container onto which to copy block</param>
        /// <param name="overwrite">Overwrite existing block if present</param>
        internal static void CopyBlock(S7Handle s7Handle,
            S7Block block, S7Container destination, bool overwrite = true)
        {
            var log = s7Handle.Log;
            var blockName = block.Name;
            var blockType = block.ConcreteType;

            if (blockType == S7BlockType.S7SDBs)
            {
                log.Warning($"Block {blockName} is a system data block: skipping.");
                return;
            }

            IS7SWItem destBlock = null;
            // Check if block is already present
            try
            {
                destBlock = destination.Next[blockName];
            }
            catch (Exception) { }

            if (destBlock != null && !overwrite)
            {
                throw new ArgumentException($"Block {blockName} already exists and overwrite is disabled.",
                                            nameof(overwrite));
            }
            else if (destBlock != null && overwrite)
            {
                log.Debug($"{blockName} already exists. Overwriting.");
                try
                {
                    destination.Next.Remove(blockName);
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not remove existing block {blockName}");
                    throw exc;
                }
            }

            try
            {
                var item = block.Copy(destination);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import block {blockName} ({blockType}) from library: ");
                throw;
            }

            log.Debug($"Imported block {blockName} ({blockType}) from library");
        }
    }
}
