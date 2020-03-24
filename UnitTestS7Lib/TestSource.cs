using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using S7Lib;

namespace UnitTestS7Lib
{
    [TestClass]
    public class TestSource
    {
        static string workspaceDir = Path.Combine(Path.GetTempPath(), "UnitTestS7");
        static string sourcesDir = Path.GetFullPath(@"..\..\..\resources\sources\");
        static string awlSource = "FB1_Flowchart1";
        static string sclSource = "FC20_BIC_Signal_Reset";

        [ClassCleanup]
        public static void RemoveTestProject()
        {
            var ctx = new S7Context();
            Api.RemoveProject(ctx, "testProj");
        }

        [TestMethod]
        public void TestImportSclSource()
        {
            var ctx = new S7Context();
            Api.CreateProject(ctx, "testProj", workspaceDir);
            Api.CreateProgram(ctx, "testProj", "testProgram");
            var rv = Api.ImportSourcesDir(ctx, "testProj", "testProgram", sourcesDir);
            Assert.AreEqual(0, rv);
            // Import existing sources fails if overwrite is set to false
            rv = Api.ImportSourcesDir(ctx, "testProj", "testProgram", sourcesDir, overwrite: false);
            Assert.AreEqual(-1, rv);
            // Import existing sources succeeds if overwrite is set to true
            rv = Api.ImportSourcesDir(ctx, "testProj", "testProgram", sourcesDir, overwrite: true);
            Assert.AreEqual(0, rv);
            Api.RemoveProject(ctx, "testProj");
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

    }
}
