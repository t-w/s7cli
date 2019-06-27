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
}
