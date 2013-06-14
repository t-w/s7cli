using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Runtime.InteropServices;

using SimaticLib;

namespace CryoAutomation
{
    public abstract class SimaticCommand
    {
        public virtual void exec() { }
    }

    public class SimaticImport : SimaticCommand 
    {
        string projectDirectory;
        string program;
        string [] sourceFiles;
        S7Project project;

        public SimaticImport(string projectDir, string programName, string CSVFileList)
        {
            projectDirectory = projectDir;
            program = programName;
            string sourceFilesCSV = CSVFileList;
            sourceFiles = sourceFilesCSV.Split(',');

            //System.Console.Write("\nprojectDirectory: " + projectDirectory + "\n");
            //System.Console.Write("\nprogram: " + programName + "\n");
            //System.Console.Write("\nsourceFiles: " + sourceFilesCSV + "\n");

            project = new S7Project(projectDirectory);            
        }

        public SimaticImport(string projectDir, string programName, string [] fileList)
        {
            projectDirectory = projectDir;
            program = programName;
            sourceFiles = fileList;

            project = new S7Project(projectDirectory);
        }

        public override void exec()
        {
            System.Console.Write("\nImporting to program: " + program + "\n\n");
            foreach (string srcfile in sourceFiles)
            {
                System.Console.Write("\nImporting file: " + srcfile);
                project.addSourceModule(program, srcfile);
            }
        }   
    }

    
    public class SimaticCompile : SimaticCommand 
    {
        string projectDirectory;
        string program;
        string [] sources;
        S7Project project;

        public SimaticCompile(string projectDir, string programName, string CSVSourceList)
        {
            projectDirectory = projectDir;
            program = programName;
            string sourceListCSV = CSVSourceList;

            //System.Console.Write("\nprojectDirectory: " + projectDirectory + "\n");
            //System.Console.Write("\nprogram: " + programName + "\n");
            System.Console.Write("\nsourceFiles: " + sourceListCSV + "\n");

            project = new S7Project(projectDirectory);

            sources = sourceListCSV.Split(',');
        }

        public override void exec()
        {
            System.Console.Write("\nBuilding program: " + program + "\n\n");
            foreach (string src in sources)
            {
                System.Console.Write("\nCompiling file: " + src);
                project.compileSource(program, src);
            }
            
        }   
    }



    class s7cli
    {
        static readonly string [] implemented_commands = { "import", "importdir", "compile" };

        static readonly string logo = @"
                                      
                  _|_|_|_|_|            _|  _|
          _|_|_|          _|    _|_|_|  _|    
        _|_|            _|    _|        _|  _|
            _|_|      _|      _|        _|  _|
        _|_|_|      _|          _|_|_|  _|  _|

        Simatic Step7 command-line interface, v0.1
        (C) 2013 CERN, TE-CRG-CE Controls

        Authors: Michal Dudek, Tomasz Wolak
";


        static public void usage()
        {
            Console.Write("\n\nUsage: s7cli <command> [command args]\n\nAvailable commands: ");
            foreach (string cmd in implemented_commands)
                Console.Write(cmd + ", ");
            Console.Write("\n\n");
        }

        static void Main(string[] args)
        {
            Console.Write(logo);

            if (args.Length < 4)
            {
                usage();
                return;
            }
            string command;
            command = args[0];
            //System.Console.Write("\ncommand: " + command + "\n");
            //WinAPI winAPI = new WinAPI();
            //winAPI.test();
            //return;
            // 
            if (command == "import")
            {
                string projectDir = args[1];
                string program = args[2];
                string srclist = args[3];
                /*System.Console.Write("\nImporting source files\n\n");
                System.Console.Write("\nProject: " + projectDir + "\n");
                System.Console.Write("\nProgram: " + program + "\n");
                System.Console.Write("\nsources to import: " + srclist + "\n"); */

                SimaticImport import = new SimaticImport(projectDir, program, srclist);
                import.exec();
            }
            else if (command == "importdir")
            {
                string projectDir = args[1];
                string program = args[2];
                string srcdir = args[3];
                /*System.Console.Write("\nImporting source files\n\n");
                System.Console.Write("\nProject: " + projectDir + "\n");
                System.Console.Write("\nProgram: " + program + "\n");
                System.Console.Write("\ndirectory with sources to import: " + srcdir + "\n"); */
                List<string> srcfileslist = new List<string>();
                srcfileslist = new List<string>();
                string[] ext2import = { "*.SCL", "*.AWL", "*.INP" };
                foreach (string ext in ext2import)
                    srcfileslist.AddRange(                    
                        System.IO.Directory.GetFiles( srcdir, ext, 
                            System.IO.SearchOption.TopDirectoryOnly ));
                string [] srcfiles = srcfileslist.ToArray();

                SimaticImport import = new SimaticImport(projectDir, program, srcfiles);
                import.exec();
            }
            else if (command == "compile")
            {
                string projectDirectory = args[1];
                string program = args[2];
                string srclist = args[3];
                S7Project project = new S7Project(projectDirectory);

                //S7SWItem src_module = project.getSourceModule("ARC56_program", "4_Compilation_OB");

                SimaticCompile compile = new SimaticCompile(projectDirectory, program, srclist);
                compile.exec();

            }
            else
            {
                System.Console.WriteLine("Unknown command: " + command + "\n\n");
                usage();
            }


            //siemensPLCProject project = new siemensPLCProject("D:\\controls\\apps\\sector56\\plc\\mirror56");

            //System.Console.Write("\nsources LogPath: " + sources.LogPath + "\n");
            //S7SWItems src_modules = project.getSourceModules("ARC56_program");
            //System.Console.Write("\nsrouce modules count: " + src_modules.Count + "\n");

            //S7SWItem src_module = project.getSourceModule("ARC56_program", "4_Compilation_OB");
            //System.Console.Write(src_module.NameToString());
            //System.Console.Write(src_module.Name);
            //src_modules.Add("Test1", SimaticLib.S7SWObjType.S7Source ,"D:\\test1.scl");
            //project.addSourceModuleSCL("ARC56_program", "D:\\test1.scl");
        }
    }
}
