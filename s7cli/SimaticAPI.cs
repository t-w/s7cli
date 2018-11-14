/************************************************************************
 * SimaticAPI.cs - SimaticAPI class                                     *
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
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Windows.Automation;
//using System.Collections.Generic;

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
    public class SimaticAPI
    {
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

            // force server mode
            enableUnattendedServerMode();
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

        public string getListOfAvailableProjects()
        {
            string availableProjects = "";
            foreach (IS7Project project in simatic.Projects)
            {
                availableProjects += ("- " + project.Name + ", " + project.LogPath + "\n");
            }
            return availableProjects;
        }

        public Simatic getSimatic()
        {
            return simatic;
        }


        /*
         * Destructor
         */
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

    }
}
