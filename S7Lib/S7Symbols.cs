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
        static readonly string ReportFilePath = @"C:\ProgramData\Siemens\Automation\Step7\S7Tmp\sym_imp.txt";
        // Default titles for Notepad window opened on import symbols command
        // The title window may or may not include the .txt extension depending on Explorer settings
        static readonly string[] NotepadWindowTitles = new string[]{
            "sym_imp - Notepad", "sym_imp.txt - Notepad"
        };

        internal static string ReadFile(S7Handle s7Handle, string path)
        {
            var log = s7Handle.Log;
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
        private static string GetImportReport(S7Handle s7Handle,
            out int errors, out int warnings, out int conflicts)
        {
            string report = ReadFile(s7Handle, ReportFilePath);
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
        private static void CloseSymbolImportationLogWindow(S7Handle s7Handle)
        {
            var log = s7Handle.Log;
            IntPtr windowHandle = IntPtr.Zero;
            foreach (var windowTitle in NotepadWindowTitles)
            {
                windowHandle = WindowsAPI.FindWindow(null, windowTitle);
                if (windowHandle != IntPtr.Zero) break;
            }
            if (windowHandle == IntPtr.Zero)
            {
                log.Warning($"Could not find Notepad window with importation log.");
                return;
            }
            WindowsAPI.SendMessage(windowHandle, WindowsAPI.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            log.Debug($"Closed Notepad window with importation log");
        }

        /// <summary>
        /// Import symbol table into target program
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Path to program in S7 project</param>
        /// <param name="symbolFile">Path symbol file to import</param>
        /// <param name="flag">TODO</param>
        /// <param name="allowConflicts">
        /// Whether to allow conflicts. If false, then an exception is raised if conflicts are detected.
        /// </param>
        internal static void ImportSymbols(S7Handle s7Handle,
            string project, string programPath, string symbolFile, int flag = 0, bool allowConflicts = false)
        {
            var log = s7Handle.Log;
            var flags = (S7SymImportFlags)flag;
            S7Program programObj = s7Handle.GetProgram(project, programPath);

            S7SymbolTable symbolTable;
            try
            {
                symbolTable = (S7SymbolTable)programObj.SymbolTable;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access symbol table in {project}:{programObj.LogPath}");
                throw;
            }

            int numSymbols;
            try
            {
                // TODO Wrap import flag with enum!
                numSymbols = symbolTable.Import(symbolFile, Flags: flags);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not import symbol table into {project}:{programObj.LogPath} " +
                               $"from {symbolFile}");
                throw;
            }

            string report = GetImportReport(s7Handle, out int errors, out int warnings, out int conflicts);
            CloseSymbolImportationLogWindow(s7Handle);

            log.Debug($"Imported {numSymbols} symbols from {symbolFile} into {project}:{programObj.LogPath}\n" +
                      $"Report {errors} error(s), {warnings} warning(s) and {conflicts} conflict(s):\n" +
                      $"{report}");

            if (!allowConflicts && conflicts > 0)
                // TODO Improve Exception?
                throw new Exception($"Symbols importation finished with {conflicts} conflict(s)");
        }

        /// <summary>
        /// Export symbol table to file
        /// </summary>
        /// <param name="project">Project identifier, path to .s7p (unique) or project name</param>
        /// <param name="programPath">Path to program in S7 project</param>
        /// <param name="symbolFile">Path to output symbol table file</param>
        internal static void ExportSymbols(S7Handle s7Handle,
            string project, string programPath, string symbolFile)
        {
            var log = s7Handle.Log;
            S7Program programObj = s7Handle.GetProgram(project, programPath);

            S7SymbolTable symbolTable;
            try
            {
                symbolTable = (S7SymbolTable)programObj.SymbolTable;
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not access symbol table in {project}:{programObj.LogPath}");
                throw;
            }

            try
            {
                symbolTable.Export(symbolFile);
            }
            catch (Exception exc)
            {
                log.Error(exc, $"Could not export symbols from {project}:{programObj.LogPath} " +
                               $"to {symbolFile}");
                throw;
            }

            log.Debug($"Exported symbols from {project}:{programObj.LogPath} to {symbolFile}");
        }
    }
}
