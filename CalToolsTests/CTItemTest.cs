using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalTools_WPF;

namespace CalToolsTests
{
    [TestClass]
    public class CTItemTest
    {
        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            string sn = "TestSN";
            CTItem item = new CTItem(sn);

            Assert.AreEqual(item.SerialNumber, sn);
            Assert.AreEqual(item.Location, "");
            Assert.AreEqual(item.Manufacturer, "");
            Assert.AreEqual(item.Directory, "");
            Assert.AreEqual(item.Description, "");
            Assert.IsTrue(item.InService);
            Assert.IsNull(item.InServiceDate);
            Assert.AreEqual(item.InServiceDateString, "");
            Assert.AreEqual(item.Model, "");
            Assert.AreEqual(item.Remarks, "");
            Assert.IsNull(item.TimeStamp);
            Assert.AreEqual(item.TimeStampString, "");
            Assert.AreEqual(item.ItemGroup, "");
            Assert.AreEqual(item.CertificateNumber, "");
            Assert.IsFalse(item.StandardEquipment);
            Assert.IsFalse(item.ChangesMade);
        }

        [TestMethod]
        public void TestDatabaseColumns()
        {
            Assert.AreEqual((int)CTItem.DatabaseColumns.SerialNumber, 0);
            Assert.AreEqual((int)CTItem.DatabaseColumns.Location, 1);
            Assert.AreEqual((int)CTItem.DatabaseColumns.Manufacturer, 2);
            Assert.AreEqual((int)CTItem.DatabaseColumns.Directory, 3);
            Assert.AreEqual((int)CTItem.DatabaseColumns.Description, 4);
            Assert.AreEqual((int)CTItem.DatabaseColumns.InService, 5);
            Assert.AreEqual((int)CTItem.DatabaseColumns.InServiceDate, 6);
            Assert.AreEqual((int)CTItem.DatabaseColumns.Model, 7);
            Assert.AreEqual((int)CTItem.DatabaseColumns.Comments, 8);
            Assert.AreEqual((int)CTItem.DatabaseColumns.Timestamp, 9);
            Assert.AreEqual((int)CTItem.DatabaseColumns.ItemGroup, 10);
            Assert.AreEqual((int)CTItem.DatabaseColumns.StandardEquipment, 11);
            Assert.AreEqual((int)CTItem.DatabaseColumns.CertificateNumber, 12);
        }


    }
}
