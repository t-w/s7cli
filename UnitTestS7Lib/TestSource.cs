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

        [TestMethod]
        public void TestImportSclSource()
        {
            Api.CreateProject("testProj", workspaceDir);
            Api.CreateProgram("testProj", "testProgram");
            var rv = Api.ImportSourcesDir("testProj", "testProgram", sourcesDir);
            Assert.AreEqual(0, rv);
            Api.RemoveProject("testProj");
        }

        [TestMethod]
        public void TestCompileAwlSource()
        {
            Api.CreateProject("testProj", workspaceDir);
            Api.CreateProgram("testProj", "testProgram");
            // TODO: Only import .awl source
            Api.ImportSourcesDir("testProj", "testProgram", sourcesDir);
            var rv = Api.CompileSource("testProj", "testProgram", awlSource);
            // Assert.AreEqual(0, rv);
            Api.RemoveProject("testProj");
        }

        [TestMethod]
        public void TestCompileSclSource()
        {
            Api.CreateProject("testProj", workspaceDir);
            Api.CreateProgram("testProj", "testProgram");
            // TODO: Only import .awl source
            Api.ImportSourcesDir("testProj", "testProgram", sourcesDir);
            var rv = Api.CompileSource("testProj", "testProgram", sclSource);
            // Assert.AreEqual(0, rv);
            Api.RemoveProject("testProj");
        }

    }
}
