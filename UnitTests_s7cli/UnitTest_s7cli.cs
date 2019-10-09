using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests_s7cli
{
    [TestClass]
    public class UnitTest_s7cli
    {
        [TestMethod]
        public void TestMain()
        {
            string [] args = { };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));

            // Invalid verb
            args = new string [] { "invalidVerb" };
            Assert.AreEqual(1, S7_cli.s7cli.Main(args));
            // Valid help flag
            args = new string[] { "--help" };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));
            // Invalid help flag
            args = new string[] { "-h" };
            Assert.AreEqual(1, S7_cli.s7cli.Main(args));
            // Invalid verb
            args = new string[] { "invalidVerb", "--help" };
            Assert.AreEqual(1, S7_cli.s7cli.Main(args));
            // Invalid verb, invalid help flag
            args = new string[] { "invalidVerb", "-h" };
            Assert.AreEqual(1, S7_cli.s7cli.Main(args));
            // Valid verb, valid help flag, missing arguments
            args = new string[] { "createProject", "--help" };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));
            // Valid verb, invalid help flag
            args = new string[] { "createProject", "-h" };
            Assert.AreEqual(1, S7_cli.s7cli.Main(args));
            // Valid version flag
            args = new string[] { "--version" };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));
        }
    }


    [TestClass]
    public class OptionParserTests
    {
        [TestMethod]
        public void TestParseOptions()
        {
            string[] args = { };
            // No verb
            args = new string[] {};
            Assert.AreEqual(0, S7_cli.OptionParser.parse(args, run: false));
            // Invalid verb
            args = new string[] { "invalidVerb" };
            Assert.AreEqual(1, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, missing required option
            args = new string[] { "createProject" };
            Assert.AreEqual(1, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, unknown option
            args = new string[] { "createProject", "--incorrect_option"};
            Assert.AreEqual(1, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, incomplete option
            args = new string[] { "createProject", "--projname" };
            Assert.AreEqual(1, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, incorrect option
            args = new string[] { "createProject", "--projname", "NAME", "--incorrect_option" };
            Assert.AreEqual(1, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, incomplete option
            args = new string[] { "createProject", "--projname", "NAME", "--projdir" };
            Assert.AreEqual(1, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, empty option
            args = new string[] { "createProject", "--projname", "--projdir" };
            Assert.AreEqual(1, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, valid options
            args = new string[] { "createProject", "--projname", "NAME", "--projdir", "DIR" };
            Assert.AreEqual(0, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, valid options, optional flags
            args = new string[] { "createProject", "--projname", "NAME", "--projdir", "DIR", "--debug", "3"};
            Assert.AreEqual(0, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, valid options, optional flags
            args = new string[] { "createProject", "--projname", "NAME", "--projdir", "DIR", "-d", "3" };
            Assert.AreEqual(0, S7_cli.OptionParser.parse(args, run: false));
            // Valid verb, no options needed
            args = new string[] { "listProjects" };
            Assert.AreEqual(0, S7_cli.OptionParser.parse(args, run: false));
        }
    }
}
