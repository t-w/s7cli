/************************************************************************
 * SimaticProgram.cs - SimaticProgram class                             *
 *                                                                      *
 * Copyright (C) 2013-2019 CERN                                         *
 *                                                                      *
 * This program is free software: you can redistribute it and/or modify *
 * it under the terms of the GNU General Public License as published by *
 * the Free Software Foundation, either version 3 of the License, or    *
 * (at your option) any later version.                                  *
 *                                                                      *
 * This program is distributed in the hope that it will be useful,      *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of       *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the        *
 * GNU General Public License for more details.                         *
 *                                                                      *
 * You should have received a copy of the GNU General Public License    *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.*
 ************************************************************************/

using System;
using System.IO;
//using System.Runtime.InteropServices;
//using System.Windows.Automation;
using System.Collections.Generic;

using SimaticLib;
using S7HCOM_XLib;

namespace S7_cli
{

    //////////////////////////////////////////////////////////////////////////
    /// class SimaticProgram
    /// <summary>
    /// class to manage all actions with an S7Program (a COM object representing
    /// a single program in a SIMATIC project)
    /// </summary>
    ///
    public class SimaticProgram
    {
        private string name;
        private S7Program s7program;
        private S7ProgramSources s7sources;

        // constructor
        public SimaticProgram(string programName, S7Program program)
        {
            name = programName;
            s7program = program;
            s7sources = new S7ProgramSources( (S7SWItem)s7program.Next["Sources"]);
        }


        public string getName()
        {
            return this.name;
        }


        public IS7Program getProgram()
        {
            return this.s7program;
        }

        // returns IS7SymbolTable COM object with interface to access the symbol table of the program
        private IS7SymbolTable getSymbolTable()
        {
            try
            {
                return this.s7program.SymbolTable;
            }
            catch (System.Exception exc)
            {
                Logger.log_error("\n** getSymbolTable(): Error accessing the symbol table for the program: '" +
                                 name + "':\n" + exc.Message + "\n");
                return null;
            }
        }

       
        /// <summary>
        /// Imports symbols from a file
        /// </summary>
        /// <param name="symbolsPath"></param>
        /// <returns>number of imported symbols (or -1 on error)</returns>
        public int importSymbols( string symbolsPath )
        {
            int nrOfSymbols = -1;

            try
            {
                S7SymbolTable symbolTable = ( S7SymbolTable )getSymbolTable();
                nrOfSymbols = symbolTable.Import( symbolsPath );
            }
            catch ( SystemException exc )
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                return -1;
            }
            return nrOfSymbols;
        }


        /* exports symbols to a file 
         *   returned value:
         *   0    - success
         *   != 0 - failure
         */
        public int exportSymbols(string symbolsOutputFile)
        {
            IS7SymbolTable symbol_table = this.getSymbolTable();
            if (symbol_table == null)
                return -1;
            try
            {
                symbol_table.Export(symbolsOutputFile);
            }
            catch (SystemException e)
            {
                Logger.log_debug("\n** exportSymbols(): Error exporting the symbol table for the program: '" +
                     this.name + "':\n" + e.Message + "\n");
                return -1;
            }
            return 0;
        }


        // returns the list of names of the sources of the program
        public string[] getSourcesList()
        {
            return s7sources.getSourcesList();
            /*
            List<string> srcs = new List<string>();
            S7SWItems src_modules = this.getSourceModules();
            if (src_modules == null)
                return null;   // failure
            foreach (S7SWItem src_module in src_modules)
            {
                //Logger.log_debug("\nsrc name: " + src_module.Name + "\n");
                srcs.Add(src_module.Name);
            }
            return srcs.ToArray(); */
        }



        public bool sourceExists(string sourceName)
        {
            return s7sources.sourceExists(sourceName);
        }


        public IS7SWItem importSource(string filenameFullPath, bool forceOverwrite = false)
        {
            return s7sources.importSource(filenameFullPath, forceOverwrite);
        }


        public void removeSource(string sourceName)
        {
            s7sources.removeSource(sourceName);
        }

        /*
         *  Block management
         */

        public S7Block getBlock(string blockName)
        {
            foreach (S7Block block in s7program.Next["Blocks"].Next)
            {
                if (block.Name == blockName)
                {
                    Logger.log_debug("getBlock(): found block: " + blockName);
                    return block;
                }
            }
            Logger.log_debug("getBlock(): the block " + blockName + " not found");
            return null;
        }


        public bool blockExists(string blockName)
        {
            if (this.getBlock(blockName) != null)
            {
                Logger.log_debug("blockExists(): found block: " + blockName);
                return true;
            }

            Logger.log_debug("blockExists(): the block " + blockName + " not found");
            return false;
        }


        public void removeBlock(string blockName)
        {
            S7Block block = this.getBlock(blockName);
            try
            {
                block.Remove();
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: removeBlock()" + exc.Message + "\n");
                //return false;
            }
        }


        public S7Source getS7SourceModule(string moduleName)
        {
            //S7SWItem item = getSourceModule(programName, moduleName);
            //S7Source source = (S7Source) item.Program.;

            //IS7Source src = getSources("ARC56_program").get_Child("test11"); //get_Collection("test11");
            S7Source src;
            /*try
            {
                src = (S7Source)this.s7program.Next["Sources"].Next[moduleName];
            }
            catch (Exception exc)
            {
                Logger.log_error("Warning: " + exc.Message + "\n");
                src = null;
            }*/

            try
            {
                src = (S7Source)this.s7program.Next["Sources"].Next[moduleName];
            }
            catch (Exception exc)
            {
                Logger.log_error("Warning: " + exc.Message + "\n");
                src = null;
            }

            return src;
        }

        /* compileSource()
         */
        public int compileSource(string sourceName)
        {
            return s7sources.compileSource(sourceName);
        }


        public string getSourceTypeString(string sourceName)
        {
            return s7sources.getSourceTypeString(sourceName);
        }

        //public string getSourceFileExtension(string programName, string sourceName)
        //{
        //S7SourceType srcType = this.getSourceType(programName, sourceName);
        /*if (srcType == S7SourceType.S7AWL)          return ".AWL";
        else if (srcType == S7SourceType.S7GG)      return ".S7GG";    // to check
        else if (srcType == S7SourceType.S7GR7)     return ".S7GR7";   // to check
        else if (srcType == S7SourceType.S7NET)     return ".S7NET";   // to check
        //else if (srcType == S7SourceType.S7SCL)     return ".SCL";
        else if ((string)srcType == "S7SCL") return ".SCL";
        else if (srcType == S7SourceType.S7SCLMake) return ".INP";
        else if (srcType == S7SourceType.S7ZG)      return ".S7ZG";    // to check
        else return ".UnknownSourceType";*/
        //    Logger.log_debug("getSourceFileExtension() returns: " + this.getSourceTypeString(programName, sourceName) + "\n\n");
        //    return this.getSourceTypeString(programName, sourceName);
        //}

        /*
         * returned value:
         * 0 - success
         * !0 - error
         */
        public int exportSource(string sourceName, string ExportFileName)
        {
            return s7sources.exportSource(sourceName, ExportFileName);
        }

        // export program structure
        public int exportProgramStructure(string ExportFileName,
                                           bool ExportDuplicateCalls = true,
                                           int ColumnFlags = 0)
        {            // ExportProgramStructure(ByVal ExportFileName As String, ByVal ExportDuplicateCalls As Boolean, ByVal ColumnFlags As Long)
            /*if (simaticProject == null)
            {
                Logger.log_debug("exportProgramStructure(): Error: The project is not opened! Aborting operation!\n");
                return 1;
            }*/

            //S7Program program = (S7Program) this.getProgram(programName);
            //S7Program program = (S7Program)simaticProject.Programs[programName];
            this.s7program.ExportProgramStructure(ExportFileName, ExportDuplicateCalls, ColumnFlags);
            return 0;
        }



        /// <summary>
        /// Import sources from specified library sources
        /// </summary>
        /// <param name="librarySources"></param>
        /// <param name="forceOverwrite"></param>
        /// <returns></returns>
        public bool importLibSources(S7SWItems librarySources,
                                      bool forceOverwrite = false)
        {
            return this.s7sources.importLibSources(librarySources, forceOverwrite);
        }

    }
}