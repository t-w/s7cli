using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//using S7_cli;

namespace UnitTests_s7cli
//namespace S7_cli
{
    [TestClass]
    public class UnitTest_s7cli
    {
        [TestMethod]
        public void TestMethod_main()
        {
            string [] args = { };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));

        }
    }


    [TestClass]
    public class UnitTest_OptionParser
    {
        [TestMethod]
        public void TestMethod_constructor()
        {
            S7_cli.Option_parser parser = new S7_cli.Option_parser(null);
            Assert.AreEqual(false, parser.optionsOK());

            string[] args = { };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            args = new string[] { "non-existent-cmd"};
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            args = new string[] { "createProject" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            args = new string[] { "createProject", "--an_incorrect_option" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            // missing option arg
            args = new string[] { "createProject", "--projname" }; 
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            // missing 2nd option
            args = new string[] { "createProject", "--projname", "test" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual( false, parser.optionsOK() );

            // incorrect 2nd option
            args = new string[] { "createProject", "--projname", "test", "--not_an_option" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            // missing 2nd option's arg.
            args = new string[] { "createProject", "--projname", "test", "--projname" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            // incorrect 1st option
            args = new string[] { "createProject", "--projname_bad", "test", "--projdir", "test" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            // incorrect 2nd option
            args = new string[] { "createProject", "--projname", "test", "--projdir_bad", "test" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            // OK
            args = new string[] { "createProject", "--projname", "test", "--projdir", "test" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(true, parser.optionsOK());


            args = new string[] { "listProjects" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(true, parser.optionsOK());

        }
    }
}
