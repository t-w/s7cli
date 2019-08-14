using System;
using System.Reflection;
using System.Collections.Generic;
using CommandLine;


namespace S7_cli
{
    /// <summary>
    /// Generic options shared by all verb commands
    /// </summary>
    class Options
    {
        [Option('d', "debug", HelpText = "Debug level (0-3)")]
        public int debug { get; set; }
    }

    /// <summary>
    /// Options for every command that requires a project to be opened
    /// </summary>
    class ProjectOptions : Options
    {
        [Option('p', "project", Required = true, HelpText = "Project name or path to project")]
        public string project { get; set; }
    }

    /// <summary>
    /// Options for every command that requires a program to be specified
    /// </summary>
    class ProgramOptions : ProjectOptions
    {
        [Option("program", Required = true, HelpText = "Project name or path to project")]
        public string program { get; set; }
    }

    /// <summary>
    /// Options for every command that requires a station to be specified
    /// </summary>
    class StationOptions :ProjectOptions
    {
        [Option("stationname", HelpText = "Name of target station", SetName = "target")]
        public string stationName { get; set; }
        [Option("stationtype", HelpText = "Type of target stations", SetName = "target")]
        public string stationType { get; set; }
        [Option("allstations", HelpText = "Target all stations (y/n)", SetName = "target")]
        public string allStations { get; set; }
        [Option("modulename", HelpText = "Name of target module")]
        public string moduleName { get; set; }
    }

    /* Commands */

    [Verb("listProjects", HelpText = "List available Simatic projects")]
    class ListProjectsOptions : Options { }

    [Verb("createProject", HelpText = "Create new, empty project in specified location")]
    class CreateProjectOptions : Options
    {
        [Option("projname", Required = true, HelpText = "Project name")]
        public string projectName { get; set; }
        [Option("projdir", Required = true, HelpText = "Path to project directory")]
        public string projectDir { get; set; }
    }

    [Verb("createLib", HelpText = "Create new, empty library in specified location")]
    class CreateLibOptions : Options
    {
        [Option("libname", Required = true, HelpText = "Library name")]
        public string libName { get; set; }
        [Option("libdir", Required = true, HelpText = "Path to library directory")]
        public string libDir { get; set; }
    }

    /* Commands that require --project option */

    [Verb("listPrograms", HelpText = "List available programs in Simatic project/library")]
    class ListProgramsOptions : ProjectOptions { }

    [Verb("listStations", HelpText = "List available stations in Simatic project/library")]
    class ListStationsOptions : ProjectOptions { }

    [Verb("listContainers", HelpText = "List available containers in Simatic project/library")]
    class ListContainersOptions : ProjectOptions { }

    [Verb("importConfig", HelpText = "List available programs in Simatic project/library")]
    class ImportConfigOptions : ProjectOptions
    {
        [Option('c', "config", Required = true, HelpText = "Path to station (hardware) configuration file")]
        public string config { get; set; }
    }

    [Verb("exportConfig", HelpText = "List available programs in Simatic project/library")]
    class ExportConfigOptions : ProjectOptions
    {
        [Option('c', "config", Required = true, HelpText = "Path to station (hardware) configuration file")]
        public string config { get; set; }
        [Option("station", Required = true, HelpText = "Name of target station")]
        public string station { get; set; }
    }

    [Verb("compileAllStations", HelpText = "Compiles all stations' HW configuration")]
    class CompileAllStationsOptions : ProjectOptions { }

    [Verb("compileAllConnections", HelpText = "Compiles all stations' connections")]
    class CompileAllConnectionsOptions : ProjectOptions { }

    /* Commands that require --project and --program options */

    [Verb("importSymbols", HelpText = "Import program symbols from a file")]
    class ImportSymbolsOptions : ProgramOptions
    {
        [Option('s', "symbols", Required = true, HelpText = "Path to file with symbols")]
        public string symbols { get; set; }
        [Option("conflictok", HelpText = "Do not treat conflicts as errors (y/n)")]
        public string conflictOk { get; set; }
    }

    [Verb("exportSymbols", HelpText = "Export program symbols to a file")]
    class ExportSymbolsOptions : ProgramOptions
    {
        [Option('o', "output", Required = true, HelpText = "Path to file with symbols")]
        public string output { get; set; }
        [Option("force", HelpText = "Force overwrite (replace) existing sources in project (y/n)")]
        public string force { get; set; }
    }

    [Verb("listSources", HelpText = "List of source code modules in a program")]
    class ListSourcesOptions : ProgramOptions { }

    [Verb("listBlocks", HelpText = "List of blocks in specified program")]
    class ListBlocksOptions : ProgramOptions { }

    [Verb("importLibSources", HelpText = "Import all sources from a library to a project")]
    class ImportLibSourcesOptions : ProgramOptions
    {
        [Option('l', "library", Required = true, HelpText = "Library name (check listProjects if unsure")]
        public string library { get; set; }
        [Option("libprg", Required = true, HelpText = "Target program in library project")]
        public string libraryProgram { get; set; }
        [Option("force", HelpText = "Force overwrite (replace) existing sources in project (y/n)")]
        public string force { get; set; }
    }

    [Verb("importLibBlocks", HelpText = "Import all blocks from a library to a project")]
    class ImportLibBlocksOptions : ProgramOptions
    {
        [Option('l', "library", Required = true, HelpText = "Library name (check listProjects if unsure")]
        public string library { get; set; }
        [Option("libprg", Required = true, HelpText = "Target program in library project")]
        public string libraryProgram { get; set; }
        [Option("force", HelpText = "Force overwrite (replace) existing blocks in project (y/n)")]
        public string force { get; set; }
    }

    [Verb("importSources", HelpText = "Import specified source code files (supports .SCL, .AWL, .INP, .GR7)")]
    class ImportSourcesOptions : ProgramOptions
    {
        [Option("sources", Required = true, HelpText = "List of files with source code (CSV)")]
        public string sources { get; set; }
        [Option("force", HelpText = "Force overwrite (replace) existing sources in project (y/n)")]
        public string force { get; set; }
    }

    [Verb("importSourcesDir", HelpText = "Import all source code files from specified directory (supports .SCL, .AWL, .INP, .GR7)")]
    class ImportSourcesDirOptions : ProgramOptions
    {
        [Option("srcdir", Required = true, HelpText = "Path to directory with source code files")]
        public string sourcesDir { get; set; }
        [Option("force", HelpText = "Force overwrite (replace) existing sources in project (y/n)")]
        public string force { get; set; }
    }

    [Verb("compileSources", HelpText = "Compile specified source code module(s)")]
    class CompileSourcesOptions : ProgramOptions
    {
        [Option("sources", Required = true, HelpText = "List of files with source code (CSV)")]
        public string sources { get; set; }
    }

    [Verb("exportSources", HelpText = "Export specified source code module(s)")]
    class ExportSourcesOptions : ProgramOptions
    {
        [Option("sources", Required = true, HelpText = "List of files with source code (CSV)")]
        public string sources { get; set; }
        [Option("outputdir", Required = true, HelpText = "Output directory")]
        public string outputDir { get; set; }
    }

    [Verb("exportAllSources", HelpText = "Export all source code module(s) from a program")]
    class ExportAllSourcesOptions : ProgramOptions
    {
        [Option("outputdir", Required = true, HelpText = "Output directory")]
        public string outputDir { get; set; }
    }

    /* Start, stop and download commands */

    [Verb("downloadSystemData", HelpText = "Downloads \"System data\" to the PLC")]
    class DownloadSystemDataOptions : ProgramOptions
    {
        [Option("force", HelpText = "Force overwrite (replace) existing system data in PLC (y/n)")]
        public string force { get; set; }
    }

    [Verb("downloadBlocks", HelpText = "SLOW: Downloads Blocks to the PLC, one at a time (check download command)")]
    class DownloadBlocksOptions : ProgramOptions
    {
        [Option("force", HelpText = "Force overwrite (replace) existing blocks in PLC (y/n)")]
        public string force { get; set; }
    }

    [Verb("download", HelpText = "Downloads all blocks and \"System data\" for a given program to the PLC")]
    class DownloadOptions : ProgramOptions { }

    [Verb("startCPU", HelpText = "Starts (new start) PLC")]
    class StartCpuOptions : ProgramOptions { }

    [Verb("stopCPU", HelpText = "Stops PLC")]
    class StopCpuOptions : ProgramOptions { }

    /* Commands that require --project, --program and the --station* options */

    [Verb("downloadStation", HelpText = "Downloads programs to a given station")]
    class DownloadStationOptions : StationOptions
    {
        [Option('f', "force", HelpText = "Force overwrite station data")]
        public string force { get; set; }
    }

    [Verb("startStation", HelpText = "Starts programs in a given station")]
    class StartStationOptions : StationOptions { }

    [Verb("stopStation", HelpText = "Stops programs in a given station")]
    class StopStationOptions : StationOptions { }

    /* Experimental features */

    [Verb("exportProgramStructure", HelpText = "EXPERIMENTAL: Exports the block calling structure into a DIF file")]
    class ExportProgramStructureOptions : ProgramOptions
    {
        [Option('o', "output", Required = true, HelpText = "Output file")]
        public string output { get; set; }
    }

    [Verb("compileStation", HelpText = "EXPERIMENTAL: Compiles station hardware and connections")]
    class CompileStationOptions : ProjectOptions
    {
        [Option("station", Required = true, HelpText = "Target station name")]
        public string station { get; set; }
    }

    /// <summary>
    /// Class for obtaining the types of each options class
    /// </summary>
    static class OptionTypes
    {
        public static Type[] get()
        {
            List<Type> types = new List<Type>();
            Assembly optionsAssembly = Assembly.GetExecutingAssembly();
            foreach (Type type in optionsAssembly.GetTypes())
            {
                types.Add(type);
            }
            
            return types.ToArray();
        }
    }
}
