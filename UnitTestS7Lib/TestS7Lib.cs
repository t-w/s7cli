using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using S7Lib;


namespace UnitTestS7Lib
{
    [TestClass]
    public class TestS7Lib
    {
        static readonly string WorkspaceDir = Path.Combine(Path.GetTempPath(), "UnitTestS7");
        static readonly string SourcesDir = Path.GetFullPath(@"..\..\..\resources\sources\");

        [ClassInitialize]
        public static void ClassInitialize(TestContext testCtx)
        {
            Directory.CreateDirectory(WorkspaceDir);
            var ctx = new S7Context();
            Api.RemoveProject(ctx, "testProj");
            Api.RemoveProject(ctx, "testLib");
            Api.CreateProject(ctx, "testProj", WorkspaceDir);
            Api.CreateProgram(ctx, "testProj", "testProgram");
            Api.CreateLibrary(ctx, "testLib", WorkspaceDir);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            var ctx = new S7Context();
            //Api.RemoveProject(ctx, "testProj");
            Api.RemoveProject(ctx, "testLib");
        }

        [TestMethod]
        public void TestListProjects()
        {
            var ctx = new S7Context();
            var output = new Dictionary<string, string>();
            var rv = Api.ListProjects(ctx, ref output);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestListPrograms()
        {
            var ctx = new S7Context();
            var output = new List<string>();
            var rv = Api.ListPrograms(ctx, ref output, "AWP_Demo07");
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestListContainers()
        {
            var ctx = new S7Context();
            var output = new List<string>();
            var rv = Api.ListContainers(ctx, ref output, "AWP_Demo07");
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestListStations()
        {
            var ctx = new S7Context();
            var output = new List<string>();
            var rv = Api.ListStations(ctx, ref output, "AWP_Demo07");
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestRegisterProject()
        {
            var ctx = new S7Context();
            var s7ProjFilePath = Path.Combine(WorkspaceDir, @"testProj\testProj.s7p");
            var rv = Api.RegisterProject(ctx, s7ProjFilePath);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestCreateInvalidProject()
        {
            var ctx = new S7Context();
            var rv = Api.CreateProject(ctx, "testProject", WorkspaceDir);
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestCreateProjectTwice()
        {
            var ctx = new S7Context();
            var rv = Api.CreateProject(ctx, "testProj", WorkspaceDir);
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestRegisterInvalidProject()
        {
            var ctx = new S7Context();
            var rv = Api.RegisterProject(ctx, ".!");
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestRemoveInvalidProject()
        {
            var ctx = new S7Context();
            var rv = Api.RemoveProject(ctx, ".!");
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestImportSclSource()
        {
            var ctx = new S7Context();
            var rv = Api.ImportSourcesDir(ctx, "testProj", "testProgram", SourcesDir);
            Assert.AreEqual(0, rv);
            // Import existing sources fails if overwrite is set to false
            rv = Api.ImportSourcesDir(ctx, "testProj", "testProgram", SourcesDir, overwrite: false);
            Assert.AreEqual(-1, rv);
            // Import existing sources succeeds if overwrite is set to true
            rv = Api.ImportSourcesDir(ctx, "testProj", "testProgram", SourcesDir, overwrite: true);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestImportLibSources()
        {
            var ctx = new S7Context();
            var rv = Api.ImportLibSources(ctx,
                library: "AWP_Demo01", libProgram: "S7-Programm",
                project: "testProj", projProgram: "testProgram");
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestImportLibBlocks()
        {
            var ctx = new S7Context();
            Api.CompileSource(ctx, "AWP_Demo01", "S7-Programm", "AWP_DB333.AWL");
            var rv = Api.ImportLibBlocks(ctx,
                library: "AWP_Demo01", libProgram: "S7-Programm",
                project: "testProj", projProgram: "testProgram");
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestExportSymbols()
        {
            var ctx = new S7Context();
            var symbolFile = Path.Combine(WorkspaceDir, "awp_demo01.sdf");
            var rv = Api.ExportSymbols(ctx, "AWP_Demo01", "SIMATIC 300(1)\\CPU 319-3 PN/DP\\S7-Programm", symbolFile, overwrite: true);
            Assert.AreEqual(0, rv);
            var symbolTableExists = File.Exists(symbolFile);
            Assert.IsTrue(symbolTableExists);
        }

        [TestMethod]
        public void TestExportAllSources()
        {
            var ctx = new S7Context();
            var rv = Api.ExportAllSources(ctx, "AWP_Demo01", "S7-Programm", WorkspaceDir);
            Assert.AreEqual(0, rv);
            var sourceExists = File.Exists(Path.Combine(WorkspaceDir, "AWP_DB333.AWL"));
            Assert.IsTrue(sourceExists);
        }

        [TestMethod]
        public void TestImportSymbols()
        {
            var ctx = new S7Context();
            var symbolFile = Path.Combine(WorkspaceDir, "awp_demo01.sdf");
            Api.ExportSymbols(ctx, "AWP_Demo01", "SIMATIC 300(1)\\CPU 319-3 PN/DP\\S7-Programm", symbolFile, overwrite: true);
            var rv = Api.ImportSymbols(ctx, "testProj", "testProgram", symbolFile);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestCompileAwlSource()
        {
            var ctx = new S7Context();
            var rv = Api.CompileSource(ctx, "AWP_Demo01", "S7-Programm", "AWP_DB333.AWL");
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestCompileSclSource()
        {
            var ctx = new S7Context();
            var rv = Api.CompileSource(ctx, "ZEN05_01_S7SCL__Measv06", "S7 Program", "Measv06");
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestCompileAllStations()
        {
            var ctx = new S7Context();
            var rv = Api.CompileAllStations(ctx, "AWP_Demo01");
            Assert.AreEqual(0, rv);
        }
    }
}
