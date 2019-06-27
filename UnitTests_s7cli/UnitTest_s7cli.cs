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

            args = new string [] { "whatever" };
            Assert.AreNotEqual(0, S7_cli.s7cli.Main(args));

            args = new string[] { "--help" };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));

            args = new string[] { "-h" };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));


            args = new string[] { "whatever", "--help" };
            Assert.AreEqual(0, S7_cli.s7cli.Main(args));

            args = new string[] { "whatever", "-h" };
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

            // empty name
            args = new string[] { "createProject", "--projname", "", "--projdir", "test" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());

            // empty dir
            args = new string[] { "createProject", "--projname", "test", "--projdir", "" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(false, parser.optionsOK());


            // OK
            args = new string[] { "createProject", "--projname", "testprj", "--projdir", "testdir", "--debug", "3" };
            S7_cli.Logger.setLevel(3);
            parser = new S7_cli.Option_parser( args );
            Assert.AreEqual( true, parser.optionsOK() );
            Assert.AreEqual( "testprj", parser.getOption( "--projname" ) );
            Assert.AreEqual( "testdir", parser.getOption( "--projdir" ) );
            //System.Console.WriteLine("debug option:" + )
            Assert.AreEqual( "3", parser.getOption("--debug") );

            // OK
            args = new string[] { "createProject", "--projname", "testprj", "--projdir", "testdir", "-d", "3" };
            S7_cli.Logger.setLevel(3);
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(true, parser.optionsOK());
            Assert.AreEqual("testprj", parser.getOption("--projname"));
            Assert.AreEqual("testdir", parser.getOption("--projdir"));
            Assert.AreEqual("3", parser.getOption("--debug"));


            args = new string[] { "listProjects" };
            parser = new S7_cli.Option_parser(args);
            Assert.AreEqual(true, parser.optionsOK());

        }
    }
}
