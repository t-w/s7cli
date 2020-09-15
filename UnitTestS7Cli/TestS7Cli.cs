using Microsoft.VisualStudio.TestTools.UnitTesting;
using S7Cli;

namespace UnitTestS7Cli
{
    [TestClass]
    public class TestS7Cli
    {
        [TestMethod]
        public void TestMain()
        {
            var parser = new OptionParser(run: false);
            
            // No verb
            string[] args = { };
            Assert.AreEqual(0, parser.Parse(args));
            // Invalid verb
            args = new string [] { "invalidVerb" };
            Assert.AreEqual(-1, parser.Parse(args));
            // Valid help flag
            args = new string[] { "--help" };
            Assert.AreEqual(0, parser.Parse(args));
            // Invalid help flag
            args = new string[] { "-h" };
            Assert.AreEqual(-1, parser.Parse(args));
            // Invalid verb
            args = new string[] { "invalidVerb", "--help" };
            Assert.AreEqual(-1, parser.Parse(args));
            // Invalid verb, invalid help flag
            args = new string[] { "invalidVerb", "-h" };
            Assert.AreEqual(-1, parser.Parse(args));
            // Valid verb, valid help flag, missing arguments
            args = new string[] { "createProject", "--help" };
            Assert.AreEqual(0, parser.Parse(args));
            // Valid verb, invalid help flag, missing arguments
            args = new string[] { "createProject", "-h" };
            Assert.AreEqual(-1, parser.Parse(args));
            // Valid version flag
            args = new string[] { "--version" };
            Assert.AreEqual(0, parser.Parse(args));
        }
    }
}
