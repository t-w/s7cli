using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using S7Lib;
using Serilog;

namespace UnitTestS7Lib
{
    [TestClass]
    public class TestProject
    {
        static string workspaceDir = Path.Combine(Path.GetTempPath(), "UnitTestS7");

        [ClassCleanup]
        public static void RemoveTestProject()
        {
            var ctx = new S7Context();
            Api.RemoveProject(ctx, "testProj");
        }

        [TestMethod]
        public void TestListProjects()
        {
            var ctx = new S7Context();
            var output = new List<KeyValuePair<string, string>>();
            var rv = Api.ListProjects(ctx, ref output);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void TestCreateRemoveProject()
        {
            var ctx = new S7Context();
            var rv = Api.CreateProject(ctx, "testProj", workspaceDir);
            Assert.AreEqual(0, rv);
            var s7ProjFilePath = Path.Combine(workspaceDir, @"testProj\testProj.s7p");
            var projectExists = File.Exists(s7ProjFilePath);
            Assert.IsTrue(projectExists);
            rv = Api.RemoveProject(ctx, "testProj");
            Assert.AreEqual(0, rv);
            projectExists = File.Exists(s7ProjFilePath);
            Assert.IsFalse(projectExists);
        }

        [TestMethod]
        public void TestCreateRemoveLibrary()
        {
            var ctx = new S7Context();
            var rv = Api.CreateProject(ctx, "testLib", workspaceDir);
            Assert.AreEqual(0, rv);
            var s7ProjFilePath = Path.Combine(workspaceDir, @"testLib\testLib.s7p");
            var projectExists = File.Exists(s7ProjFilePath);
            Assert.IsTrue(projectExists);
            rv = Api.RemoveProject(ctx, "testLib");
            Assert.AreEqual(0, rv);
            projectExists = File.Exists(s7ProjFilePath);
            Assert.IsFalse(projectExists);
        }

        [TestMethod]
        public void TestRegisterProject()
        {
            var ctx = new S7Context();
            var rv = Api.CreateProject(ctx, "testProj", workspaceDir);
            Assert.AreEqual(0, rv);
            var s7ProjFilePath = Path.Combine(workspaceDir, @"testProj\testProj.s7p");
            rv = Api.RegisterProject(ctx, s7ProjFilePath);
            Assert.AreEqual(0, rv);
            Api.RemoveProject(ctx, "testProj");
        }

        [TestMethod]
        public void TestCreateInvalidProject()
        {
            var ctx = new S7Context();
            var rv = Api.CreateProject(ctx, "testProject", workspaceDir);
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestCreateProjectTwice()
        {
            var ctx = new S7Context();
            var rv = Api.CreateProject(ctx, "testProj", workspaceDir);
            Assert.AreEqual(0, rv);
            rv = Api.CreateProject(ctx, "testProj", workspaceDir);
            Assert.AreEqual(-1, rv);
            Api.RemoveProject(ctx, "testProj");
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
    }
}
