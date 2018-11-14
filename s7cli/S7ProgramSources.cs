/************************************************************************
 * S7ProgramSources.cs - S7ProgramSources class                         *
 *                                                                      *
 * Copyright (C) 2013-2018 CERN                                         *
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
    /// class S7ProgramSources
    /// <summary>
    /// class to manage all actions with S7 program sources (a COM object representing
    /// a single program in a SIMATIC project)
    /// </summary>
    ///
    public class S7ProgramSources
    {
        private S7SWItem s7SourceItemsParent;
        private S7SWItems s7SourceItems;
        private IDictionary<string, S7SWItem> sources = null;

        // constructor
        public S7ProgramSources(S7SWItem srcItems)
        {
            s7SourceItemsParent = srcItems;
            s7SourceItems = (S7SWItems)s7SourceItemsParent.Next;
            updateSources();
        }


        // add a source to local "cache" (dictionary)
        private void addSourceToCache(string s7sourceName, S7SWItem s7source)
        {
            this.sources.Add(s7sourceName, s7source);
        }


        // remove a source from local "cache" (dictionary)
        private void removeSourceFromCache(string s7sourceName)
        {
            this.sources.Remove(s7sourceName);
        }


        // update sources information (local 'cache' - dictionary with references to all S7 source objects / S7SWItem)
        private void updateSources()
        {
            // create new dictionary with the list of sources indexed by name (to avoid accessing COM to retrieve it)
            this.sources = new Dictionary<string, S7SWItem>();
            foreach (S7SWItem s7source in s7SourceItems)
            {
                Logger.log_debug("Found source: " + s7source.Name + " type: " + s7source.Type);
                if (this.sources.Keys.Contains(s7source.Name))
                {
                    Logger.log_error("Error: multiple sources named '" + s7source.Name + "' in the project - aborting...");
                    Environment.Exit(1);
                }
                addSourceToCache(s7source.Name, s7source);
            }
        }

        // returns an array with a list of names of sources code modules
        public string[] getSourcesList()
        {
            //string [] availablePrograms;
            List<string> sourcesList = new List<string>();
            /*            foreach (IS7Program program in this.simaticProject.Programs)     {
                            availablePrograms.Add (program.Name);
                        }
             */
            foreach (string srcName in this.sources.Keys)
            {
                sourcesList.Add(srcName);
            }
            return sourcesList.ToArray();
        }


        private S7SWItem getS7SourceItem(string sourceName)
        {
            S7SWItem src_module = null;
            try
            {
                src_module = this.sources[sourceName];
            }
            catch (System.Exception exc)
            {
                Logger.log_debug("\n** getSourceModule(): Error getting the source '" + sourceName + "':\n" + exc.Message + "\n");
                return null;
            }
            return src_module;
        }

        private S7Source getS7Source(string sourceName)
        {
            return (S7Source)getS7SourceItem(sourceName);
        }

        public bool sourceExists(string sourceName)
        {
            return this.sources.Keys.Contains(sourceName);
            /*if (this.getSourceModule(sourceName) != null)
                return true;
            else
                return false; */
        }


        // import a source file to the program (with source type specified; internal/private method)
        private IS7SWItem importSourceWithType(S7SWObjType sourceType, string filename, bool forceOverwrite = false)
        {
            IS7SWItem item;

            string sourceName = System.IO.Path.GetFileNameWithoutExtension(filename);

            if (forceOverwrite && this.sourceExists(sourceName))
            {
                Logger.log("The source '" + sourceName + "' already exists - removing it (overwriting forced!)...");
                removeSource(sourceName);
                Logger.log("... and importing the new one.");
            }
            try
            {
                item = s7SourceItems.Add(sourceName, sourceType, filename);
            }
            catch (System.Exception exc)
            {
                Logger.log("\n** Error importing '" + filename + "':\n" + exc.Message + "\n");
                item = null;
            }

            if (item != null)
            {
                //updateSources();
                addSourceToCache(sourceName, (S7SWItem) item);  // add to 'cache'
            }

            return item;
        }


        // import a source file to the program
        public IS7SWItem importSource(string filenameFullPath, bool forceOverwrite = false)
        {
            string filename = System.IO.Path.GetFileName(filenameFullPath);
            string extension = System.IO.Path.GetExtension(filename);
            if (extension.ToLower() == ".scl" ||
                extension.ToLower() == ".awl" ||
                extension.ToLower() == ".inp" ||
                extension.ToLower() == ".gr7")
                //if (Array.IndexOf ( string [] Array = {".scl", ".awl"}, extension.ToLower() > -1 )
                return importSourceWithType(S7SWObjType.S7Source, filenameFullPath, forceOverwrite);
            else
            {
                Logger.log("addSource(): Error - unknown source extension '" + extension + "' (file: " + filename + ")\n");
                return null;
            }
        }

        // removes an S7 source from the program in the SIMATIC project
        public void removeSource(string sourceName)
        {
            S7SWItem src = this.getS7SourceItem(sourceName);
            try
            {
                src.Remove();
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: removeBlock()" + exc.Message + "\n");
                //return false;
            }

            //updateSources();
            removeSourceFromCache(sourceName);  // remove also from 'cache'
        }


        /// <summary>
        /// Import S7 sources from specified library
        /// </summary>
        /// <param name="librarySources"></param>
        /// <param name="forceOverwrite"></param>
        /// 
        /// IMPORTATNT NOTE: at the moment there is no way to distinguish in which program
        /// of the library a source belongs. This means that if there are 2 sources
        /// with the same name in 2 different program THEN THE METHOD HAS UNCERTAINS RESULT(!)
        /// THE FILE CAN BE IMPORTED FROM ANY OF THEM(!).
        /// Therefore it is advised to create/use libraries with only one program inside(!).
        /// <returns></returns>
        public bool importLibSources(S7SWItems librarySources,
                                      bool forceOverwrite = false)
        {
            try
            {
                foreach (S7Source source in librarySources)
                {
                    Logger.log("\nCopying the source: " + source.Name);
                    if (this.sourceExists(source.Name))
                    {
                        Logger.log("This source already exists in this program!");

                        if (forceOverwrite)
                        {
                            Logger.log("Overwrite forced! (removing and copying the source)");
                            //continue;
                            this.removeSource(source.Name);
                        }
                        else
                        {
                            Logger.log("Skipping the source!");
                            continue;
                        }
                    }
                    //source.Copy(s7program.Next["Sources"]. );
                    source.Copy(s7SourceItemsParent);
                }
            }
            catch (SystemException exc)
            {
                Logger.log_error("Error: " + exc.Message + "\n");
                return false;
            }

            return true;
        }


        // return the type of the specified s7 source
        private S7SourceType getSourceType(string sourceName)
        {
            S7Source src = getS7Source(sourceName);
            Logger.log_debug("getSourceType(" + sourceName + ")\n\n");
            Logger.log_debug("returns: " + src.ConcreteType.GetType() + "\n\n");
            Logger.log_debug("returns: " + src.ConcreteType + "\n\n");
            return //(S7SourceType)
                src.ConcreteType;
        }


        // return name of the type of the specified s7 source
        public string getSourceTypeString(string sourceName)
        {
            /*S7SourceType srcType = this.getSourceType(programName, sourceName);
            if      (srcType == S7SourceType.S7AWL)     return "S7AWL";
            else if (srcType == S7SourceType.S7GG)      return "S7GG";
            else if (srcType == S7SourceType.S7GR7)     return "S7GR7";
            else if (srcType == S7SourceType.S7NET)     return "S7NET";
            else if (srcType == S7SourceType.S7SCL)     return "S7SCL";
            else if (srcType == S7SourceType.S7SCLMake) return "S7SCLMake";
            else if (srcType == S7SourceType.S7ZG)      return "S7ZG";
            else                                        return "Unknown"; */
            S7Source src = getS7Source(sourceName);
            Logger.log_debug("getSourceType(" + sourceName + ")\n\n");
            if (src != null)
            {
                Logger.log_debug("returned: " + src.ConcreteType.GetType() + "\n\n");
                Logger.log_debug("returned: " + src.ConcreteType + "\n\n");
                return //(S7SourceType)
                    src.ConcreteType.ToString();
            }
            else
            {
                Logger.log_debug("returned: null(!)\n\n");
                return null;
            }

        }


        /* Export source module to specified filename (full path).
         * 
         * returned value:
         * 0 - success
         * !0 - error
         */
        public int exportSource(string sourceName, string ExportFileName)
        {
            S7Source src = getS7Source(sourceName);
            if (src == null)
                return -1;
            try
            {
                //Logger.log_debug("Nous sommes la");
                src.Export(ExportFileName);
            }
            catch (System.Exception exc)
            {
                Logger.log("\n** Error exporting '" + sourceName + "':\n" + exc.Message + "\n");
                return -1;
            }
            return 0;
        }


        /* Compile an S7 source.
         * 
         * return values:
         *    0 success
         *   <0 error
         *   -1 source not found
         *   -2 exception during compilation
         *    1 unknown
         */
        public int compileSource(string sourceName)
        {
            S7Source src = getS7Source(sourceName);
            if (src == null)
                return -1;
            try
            {
                IS7SWItems items = src.Compile();  // this thing returns nothing useful -> bug in SimaticLib(!)
            }
            catch (System.Exception exc)
            {
                Logger.log("\n** compileSource(): Error compiling '" + sourceName + "':\n" + exc.Message + "\n");
                return -2;
            }
            //IntPtr ptr = new IntPtr((Void *) items. );
            /*Console.Write("\n" + items. ToString() + "\n" + items.Count + "\n");
            Type itype = items.GetType();
            Console.Write("\n" + itype + "\n");
            
            int i=0;
            foreach (S7SWItem item in items)
            {
                i++;
            }
            Console.Write("\ni = " + i + "\n");

            int a = src.AppWindowHandle; */
            //Console.Write("\n" + src.AppWindowHandle + "\n");
            //src.AppWindowHandle
            //src.AppWindowHandle
            //try{
            //  AutomationElement s7scl = AutomationElement.FromHandle(new IntPtr(src.AppWindowHandle));
            //}
            //catch (System.Exception e)
            //{
            //    e.HResult.get();
            //}
            //System.HR
            //s7scl.SetFocus();
            return 1;
        }
    }
}