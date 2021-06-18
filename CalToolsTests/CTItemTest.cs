using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalTools_WPF;
using System.Collections.Generic;
using System.Globalization;

namespace CalToolsTests
{
    [TestClass]
    public class CTItemTest
    {
        private const string testItemName = "TestSN";
        private CTItem testItem;
        [TestInitialize]
        public void SetUp()
        {
            testItem = new(testItemName);
        }

        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            Assert.AreEqual(testItemName, testItem.SerialNumber);
            Assert.AreEqual("", testItem.Location);
            Assert.AreEqual("", testItem.Manufacturer);
            Assert.AreEqual("", testItem.Directory);
            Assert.AreEqual("", testItem.Description);
            Assert.IsTrue(testItem.InService);
            Assert.AreEqual("", testItem.Model);
            Assert.AreEqual("", testItem.Remarks);
            Assert.IsNull(testItem.TimeStamp);
            Assert.AreEqual("", testItem.ItemGroup);
            Assert.AreEqual("", testItem.CertificateNumber);
            Assert.IsFalse(testItem.ReplacementAvailable);
            Assert.IsFalse(testItem.IsStandardEquipment);
            Assert.IsFalse(testItem.ChangesMade);
        }

        [TestMethod]
        public void TestDictionaryConstructor()
        {
            Dictionary<string, string> testItemParameters = new() {
                { "serial_number", "TestSn" },
                { "location", "Test Location" },
                { "manufacturer", "Test Manufacturer" },
                { "directory", "Test Directory" },
                { "description", "Test Description" },
                { "in_service", "1" },
                { "model", "Test model" },
                { "item_group", "Test item group" },
                { "remarks", "Test" },
                { "is_standard_equipment", "1" },
                { "timestamp", "2021-01-01-12-00-00-500000" }
            };
            testItem = new(testItemParameters);
            Assert.IsTrue(testItem.InService);
            Assert.IsTrue(testItem.IsStandardEquipment);
            Assert.AreEqual(testItem.SerialNumber, testItemParameters["serial_number"]);
            Assert.AreEqual(testItem.Location, testItemParameters["location"]);
            Assert.AreEqual(testItem.Manufacturer, testItemParameters["manufacturer"]);
            Assert.AreEqual(testItem.Directory, testItemParameters["directory"]);
            Assert.AreEqual(testItem.Description, testItemParameters["description"]);
            Assert.AreEqual(testItem.Model, testItemParameters["model"]);
            Assert.AreEqual(testItem.ItemGroup, testItemParameters["item_group"]);
            Assert.AreEqual(testItem.Remarks, testItemParameters["remarks"]);
            Assert.AreEqual(testItem.TimeStamp, System.DateTime.ParseExact(
                testItemParameters["timestamp"],
                "yyyy-MM-dd-HH-mm-ss-ffffff",
                CultureInfo.InvariantCulture
                ));
            Assert.IsFalse(testItem.ChangesMade);
        }

        [TestMethod]
        public void TestToStandardEquipment()
        {
            //These values should match in both objects
            testItem.Manufacturer = "Test Manufacturer";
            testItem.Description = "Test Item";
            testItem.Model = "Test Model";
            testItem.Remarks = "Test Remarks";
            testItem.ItemGroup = "Test Group";
            testItem.CertificateNumber = "Test Certificate";

            CTStandardEquipment testEquipment = testItem.ToStandardEquipment(System.DateTime.MaxValue);

            Assert.AreEqual(testItem.Manufacturer, testEquipment.Manufacturer);
            Assert.AreEqual(testItem.Description, testEquipment.Description);
            Assert.AreEqual(testItem.Model, testEquipment.Model);
            Assert.AreEqual(testItem.Remarks, testEquipment.Remarks);
            Assert.AreEqual(testItem.ItemGroup, testEquipment.ItemGroup);
            Assert.AreEqual(testItem.CertificateNumber, testEquipment.CertificateNumber);
        }
    }
}
