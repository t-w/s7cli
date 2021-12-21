using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using S7Lib;
using S7Server;
using S7Service;

namespace S7ServerTests
{
    [TestClass]
    public class UnitTestS7Impl
    {
        static readonly S7Impl S7 = new();

        [TestMethod]
        public void TestCreateProject()
        {
            var handle = new Mock<IS7Handle>();
            var req = new CreateProjectRequest
            {
                ProjectName = "ProjFoo",
                ProjectDir = "Path/to/foo.s7p",
            };

            S7.RunCommand(handle.Object, req);
            handle.Verify(mock => mock.CreateProject(req.ProjectName, req.ProjectDir), Times.Once);
        }

        // TODO Implement remaining commands
    }
}