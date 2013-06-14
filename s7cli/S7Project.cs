using System;
//using System.Runtime.InteropServices;
//using System.Windows.Automation;

using SimaticLib;

namespace CryoAutomation
{


    public class S7Project
    {
        private Simatic simatic;
        private IS7Project simaticProject = null;

        //public plcProject(string Name)
        public S7Project(string Path)
        {
            simatic = new SimaticLib.Simatic();

            foreach (IS7Project project in simatic.Projects)
            {
                //System.Console.Write("Project creator: \n" + project.Creator simatic.Projects.Count + "\n");
                /*System.Console.Write("Project name: " + project.Name + "\n");
                System.Console.Write("Project creator: " + project.Creator + "\n");
                System.Console.Write("Project comment: " + project.Comment + "\n");
                System.Console.Write("Project LogPath: " + project.LogPath + "\n");
                System.Console.Write("stations count: " + project.Stations.Count + "\n"); */

                if ( //project.Name == "ARC_LSS" and 
                    //project.LogPath == "D:\\controls\\apps\\sector56\\plc\\mirror56")
                    //project.LogPath == Path)
                    project.LogPath.ToLower() == Path.ToLower() )
                {
                    simaticProject = project;
                    break;
                }
            }

            if (simaticProject == null)
            {
                System.Console.Write("Project with path: " + Path + " not found on list of available project!!!\nAvailable projects:\n");
                foreach (IS7Project project in simatic.Projects)
                {
                    System.Console.Write("- " + project.Name + ", " + project.LogPath + "\n");
                }
            }
        }


        private void addProject(string Name, string Path)
        {
            //IS7Project project = simatic.Projects.Add("ARC_LSS", "D:\\controls\\apps\\sector56\\plc\\mirror56");
            IS7Project project = simatic.Projects.Add(Name, Path);
        }

        public IS7Project getSimaticProject()
        {
            return simaticProject;
        }


        public IS7Program getProgram(string Name)
        {
            /*
             *  Notice: Name has to be UNIQUE for each application, for each module.
             *          safest way is to make names completely unique like "ARC56_program"
             */
            foreach (IS7Program program in simaticProject.Programs)
            {
                //System.Console.Write("Program name: " + program.Name + "\n");
                //System.Console.Write("Program LogPath: " + program.LogPath + "\n");
                if (//program.Parent == sPr)
                    program.Name == Name)
                {
                    //System.Console.Write("Program OK: " + program.Name + "\n");
                    return program;
                }
            }
            return null;
        }

        public S7SWItem getSoftwareItem(string programName, string itemName)
        {
            IS7Program prg = getProgram(programName);

            S7SWItems items = prg.Next;
            //System.Console.Write("\nSoftware items count: " + items.Count + "\n");
            //S7SWItems items = prg.Next;
            foreach (S7SWItem item in items)
            {
                //System.Console.Write("\nitem: " + item.Name + "\n");
                if (item.Name == itemName)
                    return item;
            }
            return null;
        }

        public S7SWItem getSources(string programName)
        {
            return getSoftwareItem(programName, "Sources");
        }

        public S7SWItem getBlocks(string programName)
        {
            return getSoftwareItem(programName, "Blocks");
        }


        public S7SWItems getSourceModules(string programName)
        {
            S7SWItem sources = getSources(programName);
            //System.Console.Write("\nsources LogPath: " + sources.LogPath + "\n");
            S7SWItems src_modules = sources.Next;

            /*System.Console.Write("\nitems2 count: " + src_modules.Count + "\n");
            foreach (S7SWItem src_module in src_modules)
            {
                System.Console.Write("\nsrc name: " + src_module.Name + "\n");
            }*/
            return src_modules;
        }

        public S7SWItem getSourceModule(string programName, string moduleName)
        {
            foreach (S7SWItem src_module in getSourceModules(programName))
            {
                //System.Console.Write("\nsrc name: " + src_module.Name + "\n");
                if (src_module.Name == moduleName)
                    return src_module;
            }
            return null;
        }

        private IS7SWItem addSourceModuleWithType(string programName, SimaticLib.S7SWObjType sourceType, string filename)
        {
            S7SWItems src_modules = getSourceModules(programName);
            IS7SWItem item;
            try
            {
                item = src_modules.Add(System.IO.Path.GetFileNameWithoutExtension(filename), sourceType, filename);
            }
            catch( System.Exception exc )
            {
                System.Console.Write("\n** Error importing '" + filename + "':\n" + exc.Message + "\n");
                item = null;
            }
            return item;
        }

        public IS7SWItem addSourceModule(string programName, string filenameFullPath)
        {
            string filename = System.IO.Path.GetFileName(filenameFullPath);
            string extension = System.IO.Path.GetExtension(filename);
            if (extension.ToLower() == ".scl" || extension.ToLower() == ".awl" || extension.ToLower() == ".inp")
            //if (Array.IndexOf ( string [] Array = {".scl", ".awl"}, extension.ToLower() > -1 )
                return addSourceModuleWithType(programName, SimaticLib.S7SWObjType.S7Source, filenameFullPath);
            else
            {
                Console.Write("Unknown source extension '" + extension + "' (file: " + filename + ")\n");
                return null;
            }
        }


        public S7Source getS7SourceModule(string programName, string moduleName)
        {
            //S7SWItem item = getSourceModule(programName, moduleName);
            //SimaticLib.S7Source source = (SimaticLib.S7Source) item.Program.;

            //IS7Source src = getSources("ARC56_program").get_Child("test11"); //get_Collection("test11");

            foreach (S7Source src in simaticProject.Programs[programName].Next["Sources"].Next)
            {
                if (src.Name == moduleName)
                    return src;
            }
            return null;
        }


        public int compileSource(string programName, string sourceName)
        {
            S7Source src = getS7SourceModule(programName, sourceName);
            if (src == null)
            {
                return -1;
            }
            try
            {
                IS7SWItems items = src.Compile();  // this thing returns nothing useful -> bug in SimaticLib(!)
            }
            catch (System.Exception exc)
            {
                System.Console.Write("\n** Error compiling '" + sourceName + "':\n" + exc.Message + "\n");
                return -1;
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