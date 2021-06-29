using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using CalTools_WPF;
using Moq;

namespace CalToolsTests
{
    [TestClass]
    public class CTConfigTest
    {
        private CTConfig config;

        [TestInitialize]
        public void SetUp()
        {
            config = new();
        }

        [TestMethod]
        public void TestInitValues()
        {
            Assert.AreEqual("", config.DbName);
            Assert.AreEqual("", config.DbPath);
            Assert.AreEqual("", config.ListDir);
            Assert.AreEqual("", config.ItemScansDir);
            Assert.AreEqual(30, config.MarkDueDays);

            Assert.IsInstanceOfType(config.Folders, typeof(List<string>));
            Assert.AreEqual(0, config.Folders.Count);

            Assert.IsInstanceOfType(config.Procedures, typeof(List<string>));
            Assert.AreEqual(0, config.Folders.Count);
        }

        [TestMethod]
        public void TestConfigFormat()
        {
            //The expected format and values of the config file
            Assert.AreEqual(5, config.lines.Length);
            Assert.AreEqual(
                "<CalTools_Config Folders = \"PRODUCTION EQUIPMENT,ENGINEERING EQUIPMENT," +
                "QUALITY EQUIPMENT,Ref Only,Removed from Service,Debug Items\"" +
                " Procedures = \"019-0065\">",
                config.lines[0]);
            Assert.AreEqual("\t<Database DbName = \"debug_Test Equipment Calibration List.db\"/>",
                config.lines[1]);
            Assert.AreEqual("\t<Directories ListDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\" " +
                "ItemScansDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Item Scans\"/>",
                config.lines[2]);
            Assert.AreEqual("\t<DueItems MarkDueDays = \"30\"/>", config.lines[3]);
            Assert.AreEqual("</CalTools_Config>", config.lines[4]);
        }
    }
}
