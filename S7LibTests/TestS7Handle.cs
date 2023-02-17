using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using S7Lib;


namespace S7LibTests
{
    [TestClass]
    public class TestS7Handle
    {
        static readonly string WorkspaceDir = Path.Combine(Path.GetTempPath(), "UnitTestS7");
        static readonly string CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string ResourcesDir = Path.GetFullPath(CurrentDirectory + @"\..\..\resources\");
        static readonly string SourcesDir = Path.Combine(ResourcesDir, @"sources\");

        [ClassInitialize]
        public static void ClassInitialize(TestContext testCtx)
        {
            var _ = Directory.CreateDirectory(WorkspaceDir);
            using (var api = new S7Handle())
            {
                try { api.RemoveProject("testProj"); } catch { }
                try { api.RemoveProject("testProject"); } catch { }
                try { api.RemoveProject("testLib"); } catch { }

                api.CreateProject("testProj", WorkspaceDir);
                api.CreateProgram("testProj", "testProgram");
                api.CreateLibrary("testLib", WorkspaceDir);
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var api = new S7Handle())
            {
                try { api.RemoveProject("testProj"); } catch { }
                try { api.RemoveProject("testProject"); } catch { }
                try { api.RemoveProject("testLib"); } catch { }
            }
        }

        [TestMethod]
        public void TestListProjects()
        {
            using (var api = new S7Handle())
            {
                var projectDict = api.ListProjects();
            }
        }

        [TestMethod]
        public void TestListPrograms()
        {
            using (var api = new S7Handle())
            {
                var programList = api.ListPrograms("AWP_Demo07");
            }
        }

        [TestMethod]
        public void TestListContainers()
        {
            using (var api = new S7Handle())
            {
                var containerList = api.ListContainers("AWP_Demo07");
            }
        }

        [TestMethod]
        public void TestListStations()
        {
            using (var api = new S7Handle())
            {
                var stationList = api.ListStations("AWP_Demo07");
            }
        }

        [TestMethod]
        public void TestListModules()
        {
            using (var api = new S7Handle())
            {
                var stationList = api.ListModules("AWP_Demo07");
            }
        }

        [TestMethod]
        public void TestRegisterProject()
        {
            using (var api = new S7Handle())
            {
                var s7ProjFilePath = Path.Combine(WorkspaceDir, @"testProj\testProj.s7p");
                api.RegisterProject(s7ProjFilePath);
            }
        }

        [TestMethod]
        public void TestCreateProjectLongName()
        {
            using (var api = new S7Handle())
            {
                var projectName = "testProject";
                api.CreateProject("testProject", WorkspaceDir);
                var shortName = projectName.Substring(0, 8);
                var projectPath = Path.Combine(WorkspaceDir, $"{shortName}\\{shortName}.s7p");
                Assert.IsTrue(File.Exists(projectPath));
            }
        }

        [TestMethod]
        public void TestCreateProjectTwice()
        {
            using (var api = new S7Handle())
            {
                var projects = api.ListProjects();
                Assert.IsTrue(projects.ContainsValue("testProj"));

                Assert.ThrowsException<ArgumentException>(
                    () => api.CreateProject("testProj", WorkspaceDir));
            }
        }

        [TestMethod]
        public void TestRegisterInvalidProject()
        {
            using (var api = new S7Handle())
            {
                Assert.ThrowsException<COMException>(
                    () => api.RegisterProject(".!"));
            }
        }

        [TestMethod]
        public void TestRemoveInvalidProject()
        {
            using (var api = new S7Handle())
            {
                Assert.ThrowsException<KeyNotFoundException>(
                    () => api.RemoveProject(".!"));
            }
        }

        [TestMethod]
        public void TestImportSclSource()
        {
            using (var api = new S7Handle())
            {
                api.ImportSourcesDir("testProj", "testProgram", SourcesDir);
                // Import existing sources fails if overwrite is set to false
                Assert.ThrowsException<ArgumentException>(
                    () => api.ImportSourcesDir("testProj", "testProgram", SourcesDir, overwrite: false));
                // Import existing sources succeeds if overwrite is set to true
                api.ImportSourcesDir("testProj", "testProgram", SourcesDir, overwrite: true);
            }
        }

        [TestMethod]
        public void TestImportLibSources()
        {
            using (var api = new S7Handle())
            {
                api.ImportLibSources(library: "AWP_Demo01", libProgram: "S7-Programm",
                                     project: "testProj", program: "testProgram");
            }
        }

        [TestMethod]
        public void TestImportLibBlocks()
        {
            using (var api = new S7Handle())
            {
                api.CompileSource("AWP_Demo01", "S7-Programm", "AWP_DB333.AWL");
                // Specify program by name
                api.ImportLibBlocks(library: "AWP_Demo01", libProgram: "S7-Programm",
                                    project: "testProj", program: "testProgram");
                // Specify program by name logical path
                api.ImportLibBlocks(library: "AWP_Demo01", libProgram: "SIMATIC 300(1)\\CPU 319-3 PN/DP\\S7-Programm",
                    project: "testProj", program: "testProgram");
            }
        }

        [TestMethod]
        public void TestExportSymbols()
        {
            using (var api = new S7Handle())
            {
                var symbolFile = Path.Combine(WorkspaceDir, "awp_demo01.sdf");
                api.ExportSymbols("AWP_Demo01", "SIMATIC 300(1)\\CPU 319-3 PN/DP\\S7-Programm", symbolFile, overwrite: true);
                var symbolTableExists = File.Exists(symbolFile);
                Assert.IsTrue(symbolTableExists);
            }
        }

        [TestMethod]
        public void TestExportAllSources()
        {
            using (var api = new S7Handle())
            {
                api.ExportAllSources("AWP_Demo01", "S7-Programm", WorkspaceDir);
                var sourceExists = File.Exists(Path.Combine(WorkspaceDir, "AWP_DB333.AWL"));
                Assert.IsTrue(sourceExists);
            }
        }

        [TestMethod]
        public void TestImportSymbols()
        {
            using (var api = new S7Handle())
            {
                var symbolFile = Path.Combine(WorkspaceDir, "awp_demo01.sdf");
                // Specify program by name
                api.ExportSymbols("AWP_Demo01", "S7-Programm", symbolFile, overwrite: true);
                api.ImportSymbols("testProj", "testProgram", symbolFile, overwrite: true);
                // Specify program by name logical path
                api.ExportSymbols("AWP_Demo01", "SIMATIC 300(1)\\CPU 319-3 PN/DP\\S7-Programm", symbolFile, overwrite: true);
                api.ImportSymbols("testProj", "testProgram", symbolFile, overwrite: true);
            }
        }

        [TestMethod]
        public void TestCompileAwlSource()
        {
            using (var api = new S7Handle())
            {
                api.CompileSource("AWP_Demo01", "S7-Programm", "AWP_DB333.AWL");
            }
        }

        [TestMethod]
        public void TestCompileSclSource()
        {
            using (var api = new S7Handle())
            {
                api.CompileSource("ZEN05_01_S7SCL__Measv06", "S7 Program", "Measv06");
            }
        }

        [TestMethod]
        public void TestCompileAllStations()
        {
            using (var api = new S7Handle())
            {
                api.CompileAllStations("AWP_Demo01");
            }
        }
    }
}
