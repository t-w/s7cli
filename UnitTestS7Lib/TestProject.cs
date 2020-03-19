using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using S7Lib;

namespace UnitTestS7Lib
{
    [TestClass]
    public class TestProject
    {
        static string workspaceDir = Path.Combine(Path.GetTempPath(), "UnitTestS7");

        [TestMethod]
        public void TestCreateRemoveProject()
        {
            var rv = Api.CreateProject("testProj", workspaceDir);
            Assert.AreEqual(0, rv);
            var s7ProjFilePath = Path.Combine(workspaceDir, @"testProj\testProj.s7p");
            var projectExists = File.Exists(s7ProjFilePath);
            Assert.IsTrue(projectExists);
            rv = Api.RemoveProject("testProj");
            Assert.AreEqual(0, rv);
            projectExists = File.Exists(s7ProjFilePath);
            Assert.IsFalse(projectExists);
        }

        [TestMethod]
        public void TestRegisterProject()
        {
            var rv = Api.CreateProject("testProj", workspaceDir);
            Assert.AreEqual(0, rv);
            var s7ProjFilePath = Path.Combine(workspaceDir, @"testProj\testProj.s7p");
            rv = Api.RegisterProject(s7ProjFilePath);
            Assert.AreEqual(0, rv);
            Api.RemoveProject("testProj");
        }

        [TestMethod]
        public void TestCreateInvalidProject()
        {
            var rv = Api.CreateProject("testProject", workspaceDir);
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestCreateProjectTwice()
        {
            var rv = Api.CreateProject("testProj", workspaceDir);
            Assert.AreEqual(0, rv);
            rv = Api.CreateProject("testProj", workspaceDir);
            Assert.AreEqual(-1, rv);
            Api.RemoveProject("testProj");
        }

        [TestMethod]
        public void TestRegisterInvalidProject()
        {
            var rv = Api.RegisterProject(".!");
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestRemoveInvalidProject()
        {
            var rv = Api.RemoveProject(".!");
            Assert.AreEqual(-1, rv);
        }
    }
}
