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

    }
}
