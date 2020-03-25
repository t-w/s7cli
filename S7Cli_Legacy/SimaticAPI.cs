/************************************************************************
 * SimaticAPI.cs - SimaticAPI class                                     *
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
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Windows.Automation;
using System.Collections.Generic;

using SimaticLib;
using S7HCOM_XLib;

namespace S7_cli
{

    //////////////////////////////////////////////////////////////////////////
    /// class SimaticAPI
    /// <summary>
    /// main interface for accessing Simatic Step7 environment
    /// </summary>
    ///
    /// singleton based on:
    ///  https://csharpindepth.com/Articles/Singleton
    ///  
    public sealed class SimaticAPI
    {
        private static SimaticAPI instance = null;
        private static readonly object padlock = new object();

        private static Simatic simatic = null;

        /*
         * Constructor
         */
        public SimaticAPI()
        {
            simatic = new Simatic();

            if (simatic == null)
            {
                Logger.log_error("SimaticAPI(): cannot initialize Simatic");
            }

            Logger.log_debug("AutomaticSave: " + simatic.AutomaticSave.ToString());
        }

        public void enableUnattendedServerMode()
        {
            if (simatic != null)
            {
                simatic.UnattendedServerMode = true;
                Logger.log_debug("UnattendedServerMode: " + simatic.UnattendedServerMode.ToString());
            }
            else
            {
                Logger.log_error("Cannot set \"UnattendedServerMode\" to true! Simatic variable is null!");
            }
        }

        public void disableUnattendedServerMode()
        {
            if (simatic != null)
            {
                simatic.UnattendedServerMode = false;
                Logger.log_debug("UnattendedServerMode: " + simatic.UnattendedServerMode.ToString());
            }
            else
            {
                Logger.log_error("Cannot set \"UnattendedServerMode\" to false! Simatic variable is null!");
            }
        }

        public void setAutomaticSave(bool enabled)
        {
            if (simatic != null)
            {
                simatic.AutomaticSave = enabled ? 1 : 0;
            }
            else
            {
                Logger.log_error("Cannot set Automatic Save! Simatic variable is null!");
            }
        }

        public void save()
        {
            if (simatic != null)
            {
                simatic.Save();
                Logger.log_debug("Saved project");
            }
            else
            {
                Logger.log_error("Cannot save project! Simatic variable is null!");
            }
        }

        public static SimaticAPI Instance
        {
            get
            { 
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new SimaticAPI();
                    }
                    return instance;
                }
            }
        }

        /// <summary>
        /// Returns list of available projects (at least once opened in SIMATIC).
        /// </summary>
        /// <returns>A Dictionary of { "project name", "project path" }</returns>
        //public Dictionary<string, string> getListOfAvailableProjects()
        public IList<KeyValuePair<string, string>> getListOfAvailableProjects()
        {
            //Dictionary<string, string> availableProjects = new Dictionary<string, string>();
            List<KeyValuePair<string, string>> availableProjects = new List<KeyValuePair<string, string>>();

            foreach (IS7Project project in simatic.Projects)
            {
                availableProjects.Add( new KeyValuePair<string, string>( project.Name, project.LogPath ) );
            }
            return availableProjects;
        }

        public string getListOfAvailableProjectsAsString()
        {
            string availableProjects = "";
            foreach (KeyValuePair<string, string> project in getListOfAvailableProjects())
                availableProjects += ( "- " + project.Key + ", " + project.Value + "\n" );
            return availableProjects;
        }

        /// <summary>
        /// Returns IS7Project instance for specified name (can be ambiguous!) or path (safer!)
        /// </summary>
        /// <param name="pathOrName">The name or path of the project</param>
        /// <returns>IS7Project instance</returns>
        public IS7Project getProject(string pathOrName)
        {
            IS7Project simaticProject = null;
            foreach (IS7Project project in simatic.Projects)
            {
                //System.Console.Write("Project creator: \n" + project.Creator simatic.Projects.Count + "\n");
                /*System.Console.Write("Project name: " + project.Name + "\n");
                System.Console.Write("Project creator: " + project.Creator + "\n");
                System.Console.Write("Project comment: " + project.Comment + "\n");
                System.Console.Write("Project LogPath: " + project.LogPath + "\n");
                System.Console.Write("stations count: " + project.Stations.Count + "\n");
                System.Console.Write("path from command: " + Path + "\n");*/

                if ( //project.Name == "ARC_LSS" &&
                    //project.LogPath == "D:\\controls\\apps\\sector56\\plc\\mirror56")
                    //project.LogPath == Path)
                    project.LogPath.ToLower() == pathOrName.ToLower() ||
                    project.Name.ToLower() == pathOrName.ToLower())
                {
                    //Logger.log_debug("S7Project(): Found project: " + project.Name + ", " + project.LogPath);
                    simaticProject = project;
                    break;
                }
            }
            return simaticProject;
        }


        /// <summary>
        /// Returns the only existing instance (singleton)
        /// </summary>
        /// <returns>Simatic instance</returns>
        public Simatic getSimatic()
        {
            return simatic;
        }


        /// <summary>
        /// Destructor
        /// </summary>
        ~SimaticAPI()
        {
            //Logger.log_debug("AutomaticSave: " + simatic.AutomaticSave.ToString());
            //Logger.log_debug("UnattendedServerMode: " + simatic.UnattendedServerMode.ToString());

            // make sure of saving all changes
            if (simatic != null)
            {
                Logger.log_debug("Saving changes.");
                simatic.Save();
            }
        }


        /*
         * Methods below are for getting STL (AWL) compilation result
         * (for details see the documentation for the Simatic API)
         * 
         * Notes:
         * - file directory for the file must exist (otherwise no log file appears)
         * - the Compile() method called on an STL source is appending the results 
         *   (status buffer) to the log file
         */

        /// <summary>
        /// Set compilation log file and enable GUI-less compilation (only STL...)
        /// </summary>
        /// <param name="filePath">Path to the compilation log file</param>
        public void setCompilationLogfile(string filePath)
        {
            simatic.VerbLogFile = filePath;
        }

        /// <summary>
        /// Set compilation log file to a random temp. file.
        /// </summary>
        public void setCompilationLogfile()
        {
            //setCompilationLogfile("C:\\Temp\\STL_compilation_log.txt");
            setCompilationLogfile( System.IO.Path.GetTempFileName() );
        }

        /// <summary>
        /// Returns the path to the compilation log file.
        /// </summary>
        /// <returns></returns>
        public string getCompilationLogfile()
        {
            return simatic.VerbLogFile;
        }
    }
}
