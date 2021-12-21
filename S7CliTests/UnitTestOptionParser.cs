using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using S7Cli;

namespace S7CliTests
{
    [TestClass]
    public class UnitTestOptionParser
    {
        static OptionParser Parser = null;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            Parser = new OptionParser();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Parser.Dispose();
        }

        [TestMethod]
        public void TestNoVerb()
        {
            string[] args = { };
            Assert.ThrowsException<ArgumentException>(() => Parser.Parse(args, run: false));
        }

        [TestMethod]
        public void TestInvalidVerb()
        {
            string[] args = new string[] { "invalidVerb" };
            Assert.ThrowsException<ArgumentException>(() => Parser.Parse(args, run: false));
        }

        [TestMethod]
        public void TestValidHelpFlag()
        {
            string[] args = new string[] { "--help" };
            Parser.Parse(args, run: false);
        }

        [TestMethod]
        public void TestInvalidHelpFlag()
        {
            string[] args = new string[] { "-h" };
            Assert.ThrowsException<ArgumentException>(() => Parser.Parse(args, run: false));
        }

        [TestMethod]
        public void TestInvalidVerbValidHelpFlag()
        {
            string[] args = new string[] { "invalidVerb", "--help" };
            Assert.ThrowsException<ArgumentException>(() => Parser.Parse(args, run: false));
        }

        [TestMethod]
        public void TestInvalidVerbInvalidHelpFlag()
        {
            string[] args = new string[] { "invalidVerb", "-h" };
            Assert.ThrowsException<ArgumentException>(() => Parser.Parse(args, run: false));
        }

        [TestMethod]
        public void TestValidVerbMissingArgs()
        {
            string[] args = new string[] { "registerProject" };
            Assert.ThrowsException<ArgumentException>(() => Parser.Parse(args, run: false));
        }

        [TestMethod]
        public void TestValidVerbValidHelpFlagMissingArgs()
        {
            string[] args = new string[] { "registerProject", "--help" };
            Parser.Parse(args, run: false);
        }

        [TestMethod]
        public void TestValidVerbInvalidHelpFlagMissingArgs()
        {
            string[] args = new string[] { "registerProject", "-h" };
            Assert.ThrowsException<ArgumentException>(() => Parser.Parse(args, run: false));
        }

        [TestMethod]
        public void TestValidVerbValidArgs()
        {
            string[] args = new string[] { "registerProject", "--projectFilePath", "path/To/Project/project.s7p" };
            Parser.Parse(args, run: false);
        }

        [TestMethod]
        public void TestVersionFlag()
        {
            string[] args = new string[] { "--version" };
            Parser.Parse(args, run: false);
        }
    }
}
