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

        // TODO: remove? Should always be true anyway
        [Option('s', "serverMode", HelpText = "Enable Unatended Server Mode", Default = "y")]
        public string ServerMode { get; set; }
    }

    /// <summary>
    /// Options for every command that requires a project to be opened
    /// </summary>
    class ProjectOptions : Options
    {
        [Option('p', "project", Required = true, HelpText = "Path to .s7p project file or project name")]
        public string project { get; set; }
    }

    // Commands

    [Verb("listProjects", HelpText = "List registered Simatic projects")]
    class ListProjectsOptions : Options { }

    [Verb("listPrograms", HelpText = "List available programs in Simatic project/library")]
    class ListProgramsOptions : ProjectOptions { }

    [Verb("listStations", HelpText = "List available stations in Simatic project/library")]
    class ListStationsOptions : ProjectOptions { }

    [Verb("listContainers", HelpText = "List available containers in Simatic project/library")]
    class ListContainersOptions : ProjectOptions { }

    /// <summary>
    /// Class for obtaining the types of each of the option classe\\\\\\\\\\y>
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
