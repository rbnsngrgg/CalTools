using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalTools_WPF;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace CalToolsTests
{
    [TestClass]
    public class CTStandardEquipmentTest
    {
        private CTStandardEquipment testEquipment;
        [TestInitialize]
        public void SetUp()
        {
            testEquipment = new();
        }

        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            Assert.AreEqual(-1, testEquipment.Id);
            Assert.AreEqual("", testEquipment.SerialNumber);
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

        [TestMethod]
        public void TestParseParameters()
        {
            Dictionary<string, string> testItemParameters = new()
            {
                { "id", "101"},
                { "serial_number", "TestSn" },
                { "manufacturer", "Test Manufacturer" },
                { "description", "Test Description" },
                { "model", "Test model" },
                { "certificate_number", "TestCertificate" },
                { "item_group", "Test item group" },
                { "remarks", "Test" },
                { "action_due_date", "2022-01-01"},
                { "timestamp", "2021-01-01-12-00-00-500000" }
            };

            testEquipment.ParseParameters(testItemParameters);

            Assert.AreEqual(101, testEquipment.Id);
            Assert.AreEqual(testItemParameters["serial_number"], testEquipment.SerialNumber);
            Assert.AreEqual(testItemParameters["manufacturer"], testEquipment.Manufacturer);
            Assert.AreEqual(testItemParameters["description"], testEquipment.Description);
            Assert.AreEqual(testItemParameters["model"], testEquipment.Model);
            Assert.AreEqual(testItemParameters["item_group"], testEquipment.ItemGroup);
            Assert.AreEqual(testItemParameters["certificate_number"], testEquipment.CertificateNumber);
            Assert.AreEqual(testItemParameters["remarks"], testEquipment.Remarks);
            Assert.AreEqual(
                DateTime.ParseExact(testItemParameters["action_due_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                testEquipment.ActionDueDate);
            Assert.AreEqual(DateTime.ParseExact(
                testItemParameters["timestamp"],
                "yyyy-MM-dd-HH-mm-ss-ffffff",
                CultureInfo.InvariantCulture
                ), testEquipment.TimeStamp);
            Assert.IsFalse(testEquipment.ChangesMade);

            testItemParameters.Remove("id");
            testEquipment = new(testItemParameters);
            Assert.AreEqual(-1, testEquipment.Id);
        }
    }
}
