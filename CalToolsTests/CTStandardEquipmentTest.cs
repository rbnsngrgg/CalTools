using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalTools_WPF;

namespace CalToolsTests
{
    [TestClass]
    public class CTStandardEquipmentTest
    {
        private const string testEquipmentName = "TestSN";
        private CTStandardEquipment testEquipment;
        [TestInitialize]
        public void SetUp()
        {
            testEquipment = new(testEquipmentName);
        }

        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            Assert.AreEqual(-1, testEquipment.Id);
            Assert.AreEqual(testEquipmentName, testEquipment.SerialNumber);
            Assert.AreEqual("", testEquipment.Manufacturer);
            Assert.AreEqual("", testEquipment.Description);
            Assert.AreEqual("", testEquipment.Model);
            Assert.AreEqual("", testEquipment.Remarks);
            Assert.IsNull(testEquipment.TimeStamp);
            Assert.AreEqual("", testEquipment.ItemGroup);
            Assert.AreEqual("", testEquipment.CertificateNumber);
            Assert.IsFalse(testEquipment.ChangesMade);
        }

        [TestMethod]
        public void TestConstructor()
        {
            string newSn = "NewSn";
            int newId = 1;

            testEquipment = new(newSn, newId);

            Assert.AreEqual(newSn, testEquipment.SerialNumber);
            Assert.AreEqual(newId, testEquipment.Id);
        }
    }
}
