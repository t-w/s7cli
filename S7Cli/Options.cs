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

    [Verb("listProjects", HelpText = "List available Simatic projects")]
    class ListProjectsOptions : Options { }

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
