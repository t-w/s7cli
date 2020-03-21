using System;
using System.IO;
using System.Text.RegularExpressions;

using SimaticLib;


namespace S7Lib
{
    /// <summary>
    /// Contains methods related to Symbol Table
    /// </summary>
    public static class S7Symbols
    {
        // Default path for symbol import report file
        static string reportFilePath = @"C:\ProgramData\Siemens\Automation\Step7\S7Tmp\sym_imp.txt";

        public static string ReadFile(string path)
        {
            var log = Api.CreateLog();
            if (File.Exists(path))
                return File.ReadAllText(path);
            log.Warning($"File {path} not found");
            return "";
        }

        /// <summary>
        /// Returns counted errors, warnings and conflicts by parsing the content
        /// of the symbol importation file.
        /// </summary>
        /// <param name="errors">Number of errors during importation</param>
        /// <param name="warnings">Number of warnings during importation</param>
        /// <param name="conflicts">Number of symbol conflicts during importation</param>
        /// <returns>The total number of critical errors (sum of errors and conflicts)</returns>
        private static string GetImportReport(ref int errors,
                                              ref int warnings,
                                              ref int conflicts)
        {
            string report = ReadFile(reportFilePath);
            string[] split = report.Split('\n');

            int errorIndex = Array.FindIndex<string>(split, s => Regex.IsMatch(s, "^Error:.*"));
            int warningsIndex = errorIndex + 1;
            int conflictsIndex = errorIndex + 2;

            errors = Int32.Parse(split[errorIndex].Split(' ')[1]);
            warnings = Int32.Parse(split[warningsIndex].Split(' ')[1]);
            conflicts = Int32.Parse(split[conflictsIndex].Split(' ')[1]);

            return report;
        }

        /// <summary>
        /// Close the Notepad window with importation log.
        /// </summary>
        private static void CloseSymbolImportationLogWindow()
        {
            var log = Api.CreateLog();
            IntPtr handle = WindowsAPI.FindWindow(null, "sym_imp - Notepad");
            if (handle.Equals(null))
            {
                log.Warning("The Notepad window with importation log not found.");
                return;
            }
            WindowsAPI.SendMessage(handle, WindowsAPI.WM_CLOSE, new IntPtr(0), new IntPtr(0));
        }

        public static int ImportSymbols(string project, string program, string symbolFile,
            int flag = 0, bool allowConflicts = false)
        {
            var api = Api.CreateApi();
            var log = Api.CreateLog();
            var flags = (S7SymImportFlags)flag;

            S7Program target;
            try
            {
                target = (S7Program)api.Projects[project].Programs[program];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access {project}:{program}");
                return -1;
            }

            S7SymbolTable symbolTable;
            try
            {
                symbolTable = (S7SymbolTable)target.SymbolTable;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access symbol table in {project}:{program}");
                return -1;
            }

            int numSymbols = -1;
            try
            {
                numSymbols = symbolTable.Import(symbolFile, Flags: flags);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import symbol table into {project}:{program} " +
                               $"from {symbolFile}");
                return -1;
            }

            int errors = -1, warnings = -1, conflicts = -1;
            string report = GetImportReport(ref errors, ref warnings, ref conflicts);
            CloseSymbolImportationLogWindow();

            log.Debug($"Imported {numSymbols} symbols from {symbolFile} into {project}:{program}\n" +
                      $"Report {errors} error(s), {warnings} warning(s) and {conflicts} conflict(s):\n" +
                      $"{report}");

            if (!allowConflicts && conflicts > 0)
                return -1;
            return 0;
        }

        public static int ExportSymbols(string project, string program, string symbolFile)
        {
            var api = Api.CreateApi();
            var log = Api.CreateLog();

            S7Program target;
            try
            {
                target = (S7Program)api.Projects[project].Programs[program];
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access {project}:{program}");
                return -1;
            }

            S7SymbolTable symbolTable;
            try
            {
                symbolTable = (S7SymbolTable)target.SymbolTable;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access symbol table in {project}:{program}");
                return -1;
            }

            try
            {
                symbolTable.Export(symbolFile);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not export symbols from {project}:{program} " +
                               $"to {symbolFile}");
                return -1;
            }

            log.Debug($"Imported symbols from {project}:{program} to {symbolFile}");
            return 0;
        }
    }
}
