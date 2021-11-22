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
        /// <param name="program">Program name</param>
        /// <param name="source">Source name</param>
        /// <returns>S7 Source</returns>
        internal static S7Source GetSource(S7Handle s7Handle, string project, string program, string source)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var projectObj = wrapper.Add(() => s7Handle.GetProject(project));
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
        // If not found, simply return. If found and !overwrite it throws and error.
        // If found and remove then it removes the existing source.
        // Does not release container
        private static void SearchRemoveSwItem(S7Handle s7Handle, S7Container container, string swItemName, bool remove = true)
        {
            var log = s7Handle.Log;
            using (var wrapper = new ReleaseWrapper())
            {
                var swItems = container.Next;
                wrapper.Add(() => swItems);
                try
                {
                    wrapper.Add(() => swItems[swItemName]);
                    log.Debug($"SWItem {swItemName} found in container {container.Name}.");
                }
                catch (COMException)
                {
                    return;
                }

                if (!remove)
                {
                    throw new ArgumentException($"SWItem {swItemName} already exists and overwrite is disabled.",
                                                nameof(remove));
                }

                log.Debug($"SWItem {swItemName} already exists. Overwriting.");
                try
                {
                    swItems.Remove(swItemName);
                }
                catch (COMException exc)
                {
                    throw new ArgumentException($"Could not remove existing SWItem {swItemName}", exc);
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

            log.Debug($"Importing source {sourceName} ({sourceType}) from {sourceFilePath}");

            SearchRemoveSwItem(s7Handle, container, sourceName, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                var swItems = container.Next;
                try
                {
                    wrapper.Add(() => swItems.Add(sourceName, sourceType, sourceFilePath));
                }
                catch (Exception exc)
                {
                    log.Error(exc, $"Could not import source {sourceName} ({sourceType}) from {sourceFilePath}");
                    throw;
                }
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
            log.Debug($"Importing source {source.Name} ({source.ConcreteType}) to {destination.Name}");

            SearchRemoveSwItem(s7Handle, destination, source.Name, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var copy = source.Copy(destination);
                    wrapper.Add(() => copy);
                }
                catch (COMException exc)
                {
                    log.Error(exc, $"Could not copy source {source.Name} ({source.ConcreteType}) to {destination.Name}");
                    throw;
                }
            }
        }

        /// See S7Handle.ImportLibSources
        internal static void ImportLibSources(S7Handle s7Handle,
            S7Container libSources, S7Container projSources, bool overwrite = true)
        {
            using (var wrapper = new ReleaseWrapper())
            {
                var swItems = libSources.Next;
                wrapper.Add(() => swItems);
                foreach (S7Source libSource in swItems)
                {
                    CopySource(s7Handle, libSource, projSources, overwrite);
                }
            }
        }

        /// See S7Handle.ExportSource
        internal static void ExportSource(S7Handle s7Handle, S7Source source, string exportDir)
        {
            var log = s7Handle.Log;
            var sourceType = source.ConcreteType;
            string outputFile = Path.Combine(exportDir, source.Name);

            log.Debug($"Exporting {source.Name} ({sourceType}) to {outputFile}");

            try
            {
                source.Export(outputFile);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not export source {source.Name} ({sourceType}) to {outputFile}");
                throw;
            }
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
            s7Handle.Log.Debug($"Compiling source {sourceName} in {project}\\{program}");

            using (var wrapper = new ReleaseWrapper())
            {
                var source = wrapper.Add(() => GetSource(s7Handle, project, program, sourceName));
                Console.WriteLine($"Obtained source {source.Name}");
                var sourceType = source.ConcreteType;

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
                        var swItems = source.Compile();
                        wrapper.Add(() => swItems);
                    }
                    catch (COMException exc)
                    {
                        throw new Exception($"Could not compile source {sourceName} in {project}\\{program}", exc);
                    }
                }
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
            
            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var swItems = src.Compile();
                    wrapper.Add(() => swItems);
                }
                catch (COMException exc)
                {
                    log.Error($"Could not compile source {src.Name}.", exc);
                }
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
                throw new Exception($"Could not compile {src.Name}: {errors} error(s).");
            }
            else if (warnings > 0)
            {
                log.Warning($"Compiled {src.Name} with {warnings} warning(s).");
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

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var swItems = src.Compile();
                    wrapper.Add(() => swItems);
                }
                catch (COMException exc)
                {
                    log.Error($"Could not compile source {src.Name}.", exc);
                }
            }

            if (!File.Exists(verbLogFile))
            {
                throw new Exception($"Compilation log file not found {verbLogFile}.");
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
                throw new Exception($"Could not retrieve compilation result from {verbLogFile}.");
            }

            if (errors > 0)
            {
                throw new Exception($"Could not compile {src.Name}: {errors} error(s).");
            }
            else if (warnings > 0)
            {
                log.Warning($"Compiled {src.Name} with {warnings} warning(s).");
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
            log.Debug($"Copying block {block.Name} to container {destination.Name}.");

            if (block.ConcreteType == S7BlockType.S7SDBs)
            {
                log.Warning($"Block {block.Name} is a system data block: skipping.");
                return;
            }

            SearchRemoveSwItem(s7Handle, destination, block.Name, overwrite);

            using (var wrapper = new ReleaseWrapper())
            {
                try
                {
                    var item = block.Copy(destination);
                }
                catch (Exception exc)
                {
                    throw new Exception($"Could not import block {block.Name} ({block.ConcreteType}) to {destination.Name}.", exc);
                }
            }
        }
    }
}
