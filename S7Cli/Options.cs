using System;
using System.Reflection;
using System.Collections.Generic;
using CommandLine;


namespace S7Cli
{
    /// <summary>
    /// Generic options shared by all verb commands
    /// </summary>
    class Options
    {
        [Option('v', "verbose", HelpText = "Output additional information to stdout")]
        public bool Verbose { get; set; }
    }

    /// <summary>
    /// Options for every command that requires a project to be opened
    /// </summary>
    class ProjectOptions : Options
    {
        [Option('p', "project", Required = true, HelpText = "Path to .s7p project file or project name")]
        public string Project { get; set; }
    }

    /// <summary>
    /// Options for every command that requires a program to be specified
    /// </summary>
    class ProgramOptions : ProjectOptions
    {
        [Option("program", Required = true, HelpText = "Program name")]
        public string Program { get; set; }
    }

    /// <summary>
    /// Options for every online command that requires a program to be specified
    /// </summary>
    class OnlineProgramOptions : ProjectOptions
    {
        [Option("station", Required = true, HelpText = "Station name")]
        public string Station { get; set; }
        [Option("rack", Required = true, HelpText = "Rack name")]
        public string Rack { get; set; }
        [Option("module", Required = true, HelpText = "Child module name")]
        public string Module { get; set; }
        [Option('f', "force", HelpText = "Run command without confirmation")]
        public bool Force { get; set; }
    }

    /// <summary>
    /// Options for import sources/blocks from library commands
    /// </summary>
    class ImportFromLibraryOptions : Options
    {
        [Option("project", Required = true, HelpText = "Path to .s7p project file or project name")]
        public string Project { get; set; }
        [Option("library", Required = true, HelpText = "Path to .s7p library project file or library name")]
        public string Library { get; set; }
        [Option("projProgram", Required = true, HelpText = "Destination program name")]
        public string ProjProgram { get; set; }
        [Option("libProgram", Required = true, HelpText = "Source library program name")]
        public string LibProgram { get; set; }
    }


    // Commands

    [Verb("createProject", HelpText = "Create new, empty project in specified location.")]
    class CreateProjectOptions : Options
    {
        [Option("name", Required = true, HelpText = "Project name (max 8 characters)")]
        public string ProjectName { get; set; }
        [Option("dir", Required = true, HelpText = "Path to project's parent directory")]
        public string ProjectDir { get; set; }
    }

    [Verb("createLibrary", HelpText = "Create new, empty library in specified location.")]
    class CreateLibraryOptions : Options
    {
        [Option("name", Required = true, HelpText = "Library name (max 8 characters)")]
        public string ProjectName { get; set; }
        [Option("dir", Required = true, HelpText = "Path to library's parent directory")]
        public string ProjectDir { get; set; }
    }

    [Verb("registerProject", HelpText = "Register existing project.")]
    class RegisterProjectOptions : Options
    {
        [Option("projectFilePath", Required = true, HelpText = "Path to STEP 7 project .s7p file")]
        public string ProjectFilePath { get; set; }
    }

    [Verb("removeProject", HelpText = "Remove project and delete all of its files.")]
    class RemoveProjectOptions : ProjectOptions
    {
        [Option('f', "force", HelpText = "Force removal without confirmation")]
        public bool Force { get; set; }
    }

    [Verb("listProjects", HelpText = "List registered Simatic projects.")]
    class ListProjectsOptions : Options { }

    [Verb("listPrograms", HelpText = "List available programs in Simatic project/library.")]
    class ListProgramsOptions : ProjectOptions { }

    [Verb("listStations", HelpText = "List available stations in Simatic project/library.")]
    class ListStationsOptions : ProjectOptions { }

    [Verb("listContainers", HelpText = "List available containers in Simatic project/library.")]
    class ListContainersOptions : ProjectOptions { }

    [Verb("importSource", HelpText = "Import source into a program.")]
    class ImportSourceOptions : ProgramOptions
    {
        [Option("source", Required = true, HelpText = "Path to source file")]
        public string Source { get; set; }
        [Option("overwrite", HelpText = "Force overwrite existing source in project.")]
        public bool Overwrite { get; set; }
    }

    [Verb("importSourcesDir", HelpText = "Import sources from a directory into a program.")]
    class ImportSourcesDirOptions : ProgramOptions
    {
        [Option("sourcesDir", Required = true, HelpText = "Path to directory from which to import sources")]
        public string SourcesDir { get; set; }
        [Option("overwrite", HelpText = "Force overwrite existing sources in project.")]
        public bool Overwrite { get; set; }
    }

    [Verb("exportAllSources", HelpText = "Export all sources from a program.")]
    class ExportAllSourcesOptions : ProgramOptions
    {
        [Option("sourcesDir", Required = true, HelpText = "Directory to which to export sources")]
        public string SourcesDir { get; set; }
    }

    [Verb("importLibSources", HelpText = "Import sources from a library into a program.")]
    class ImportLibSourcesOptions : ImportFromLibraryOptions
    {
        [Option("overwrite", HelpText = "Force overwrite existing sources in project")]
        public bool Overwrite { get; set; }
    }

    [Verb("importLibBlocks", HelpText = "Import blocks from a library into a program.")]
    class ImportLibBlocksOptions : ImportFromLibraryOptions
    {
        [Option("overwrite", HelpText = "Force overwrite existing blocks in project")]
        public bool Overwrite { get; set; }
    }

    [Verb("importSymbols", HelpText = "Import symbols into a program from a file.")]
    class ImportSymbolsOptions : ProgramOptions
    {
        [Option("symbolFile", Required = true,
            HelpText = "Path to input symbol table file (.sdf, .asc, .dif, .seq)")]
        public string SymbolFile { get; set; }
        [Option("allowConflicts", HelpText = "Succeed even if conflits are detected")]
        public bool AllowConflicts { get; set; }
        [Option("flag", Default = 0,
            HelpText = "Symbol import flag {0,1,2}:\n" +
                       " 0 Symbols are imported even if present; may lead to ambiguities.\n" +
                       " 1 Replace conflicting symbol names with new addresses.\n" +
                       " 2 Replace conflicting addresses with new symbol names.")]
        public int Flag { get; set; }
    }

    [Verb("exportSymbols", HelpText = "Export program symbols to a file.")]
    class ExportSymbolsOptions : ProgramOptions
    {
        [Option("symbolFile", Required = true,
            HelpText = "Path to output symbol table file (.sdf, .asc, .dif, .seq)")]
        public string SymbolFile { get; set; }
    }

    [Verb("compileSource", HelpText = "Compile source.")]
    class CompileSourceOptions : ProgramOptions
    {
        [Option("source", Required = true, HelpText = "Source name")]
        public string Source { get; set; }
    }

    [Verb("compileSources", HelpText = "Compile multiple sources.")]
    class CompileSourcesOptions : ProgramOptions
    {
        [Option("sources", Required = true, Separator = ',',
             HelpText = "Comma-separated source names")]
        public IEnumerable<string> Sources { get; set; }
    }

    [Verb("compileAllStations", HelpText = "Compile the HW configuration for each of the stations in a project.")]
    class CompileAllStationsOptions : ProjectOptions
    {
        [Option("allowFail", HelpText = "Succeed even if unable to compile some station")]
        public bool AllowFail { get; set; }
    }

    [Verb("startProgram", HelpText = "[ONLINE] Start/restart a program.")]
    class StartProgramOptions : OnlineProgramOptions { }

    [Verb("stopProgram", HelpText = "[ONLINE] Stop running a program.")]
    class StopProgramOptions : OnlineProgramOptions { }

    [Verb("downloadProgramBlocks", HelpText = "[ONLINE] Download all the blocks under an S7Program.")]
    class DownloadProgramBlocksOptions : OnlineProgramOptions
    {
        [Option("overwrite", HelpText = "Force overwrite of online blocks")]
        public bool Overwrite { get; set; }
    }

    /// <summary>
    /// Class for obtaining the types of each options class
    /// </summary>
    static class OptionTypes
    {
        public static Type[] Get()
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
