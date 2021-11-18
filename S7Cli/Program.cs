using System;

namespace S7Cli
{
    class Program
    {
        /// <summary>
        /// Main function for launching S7Cli
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>0 on success, 1 otherwise</returns>
        static int Main(string[] args)
        {
            var parser = new OptionParser();
            try
            {
                parser.Parse(args);
            }
            catch (Exception)
            {
                return 1;
            }
            return 0;
        }
    }
}
