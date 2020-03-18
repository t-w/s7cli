using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using S7Lib;

namespace UnitTestS7Lib
{
    [TestClass]
    public class Project
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
        public void TestCreateInvalidProject()
        {
            var rv = Api.CreateProject("testProject", workspaceDir);
            Assert.AreEqual(-1, rv);
        }

        [TestMethod]
        public void TestRemoveInvalidProject()
        {
            var rv = Api.RemoveProject("invalid");
            Assert.AreEqual(-1, rv);
        }
    }
}
