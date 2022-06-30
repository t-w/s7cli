using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using S7Lib;


namespace S7LibTests
{
    [TestClass]
    public class TestS7HandleInitialize
    {
        static readonly string WorkspaceDir = Path.Combine(Path.GetTempPath(), "UnitTestS7");

        [ClassInitialize]
        public static void ClassInitialize(TestContext testCtx)
        {
            var _ = Directory.CreateDirectory(WorkspaceDir);
            using (var api = new S7Handle())
            {
                try { api.RemoveProject("S7Api300"); } catch { }
                try { api.RemoveProject("S7Api400"); } catch { }
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var api = new S7Handle())
            {
                try { api.RemoveProject("S7Api300"); } catch { }
                try { api.RemoveProject("S7Api400"); } catch { }
            }
        }

        [TestMethod]
        public void TestCreateAndInitS7300Project()
        {
            // TODO Hotfix
            //  System.InvalidCastException: Unable to cast COM object of type 'System.__ComObject' to interface type 'S7HCOM_XLib.*'
            Thread.Sleep(3000);
            using (var api = new S7Handle())
            {
                var projectName = "S7Api300";
                var plcName = "PLCCOIS20";
                var plcType = "S7-300";
                var cpuName = "S7-315-2PN/DP";
                var cpuOrderNumber = "6ES7 315-2EH14-0AB0";
                var cpuFirmwareVersion = "V3.2";
                var cpuIpAddress = "172.26.2.46";
                var cpuSubnetMask = "255.255.254.0";
                var cpuRouterAddress = "172.26.2.1";
                var wccIpAddress = "192.0.0.0";

                api.CreateProject(projectName, @"C:\Users\jpechirr\Downloads");
                api.InitializeProject(projectName, plcName, plcType, cpuName, cpuOrderNumber, cpuFirmwareVersion,
                    cpuIpAddress, cpuSubnetMask, cpuRouterAddress, wccIpAddress);
            }
        }

        [TestMethod]
        public void TestCreateAndInitS7400Project()
        {
            // TODO Hotfix
            //  System.InvalidCastException: Unable to cast COM object of type 'System.__ComObject' to interface type 'S7HCOM_XLib.*'
            Thread.Sleep(3000);
            using (var api = new S7Handle())
            {
                var projectName = "S7Api400";
                var plcName = "PLCCOIS20";
                var plcType = "S7-400";
                var cpuName = "CPU 416F-3 PN/DP";
                var cpuOrderNumber = "6ES7 416-3FS07-0AB0";
                var cpuFirmwareVersion = "V7.0";
                var cpuIpAddress = "172.26.2.46";
                var cpuSubnetMask = "255.255.254.0";
                var cpuRouterAddress = "172.26.2.1";
                var wccIpAddress = "192.0.0.0";

                api.CreateProject(projectName, @"C:\Users\jpechirr\Downloads");
                api.InitializeProject(projectName, plcName, plcType, cpuName, cpuOrderNumber, cpuFirmwareVersion,
                    cpuIpAddress, cpuSubnetMask, cpuRouterAddress, wccIpAddress);
            }
        }

        [TestMethod]
        public void TestEditModule()
        {
            // TODO Rely on S7300 project fixture?
            // TODO Hotfix
            //  System.InvalidCastException: Unable to cast COM object of type 'System.__ComObject' to interface type 'S7HCOM_XLib.*'
            Thread.Sleep(3000);

            var properties = new Dictionary<string, object>()
            {
                {"IPAddress", "137.138.25.92"},
                {"SubnetMask", "255.255.255.128"},
                {"RouterAddress", "137.138.25.65"},
                {"RouterActive", true }
            };

            using (var api = new S7Handle())
            {
                api.EditModule("S7Api300", "PLCCOIS20", "UR", "S7-315-2PN/DP", properties);
            };
        }

        [TestMethod]
        public void TestEditModuleInvalidProperty()
        {
            // TODO Rely on S7300 project fixture?
            // TODO Hotfix
            //  System.InvalidCastException: Unable to cast COM object of type 'System.__ComObject' to interface type 'S7HCOM_XLib.*'
            Thread.Sleep(3000);

            var properties = new Dictionary<string, object>()
            {
                {"InvalidProperty", ""},
                {"RouterActive", true }
            };

            using (var api = new S7Handle())
            {
                Assert.ThrowsException<ArgumentException>(
                    () => api.EditModule("S7Api300", "PLCCOIS20", "UR", "S7-315-2PN/DP", properties));
            };
        }

    }
}
