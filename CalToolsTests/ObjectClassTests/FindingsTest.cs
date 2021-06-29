using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalTools_WPF;
using CalTools_WPF.ObjectClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Moq;

namespace CalToolsTests
{
    [TestClass]
    public class FindingsTest
    {
        private Findings findings;

        [TestInitialize]
        public void Init()
        {
            findings = new();
        }
        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            Assert.AreEqual(-1, findings.Id);
            Assert.AreEqual(-1, findings.DataId);
            Assert.AreEqual(default, findings.Name);
            Assert.AreEqual(default, findings.Tolerance);
            Assert.AreEqual(default, findings.ToleranceIsPercent);
            Assert.AreEqual(default, findings.UnitOfMeasure);
            Assert.AreEqual(default, findings.MeasurementBefore);
            Assert.AreEqual(default, findings.MeasurementAfter);
            Assert.AreEqual(default, findings.Setting);
        }
        [TestMethod]
        public void TestParseParameters()
        {
            Dictionary<string, string> testParameters = new()
            {
                { "id", "101" },
                { "task_data_id", "102" },
                { "name", "TestName" },
                { "tolerance", "10" },
                { "tolerance_is_percent", "1" },
                { "unit_of_measure", "Nm" },
                { "measurement_before", "8.3" },
                { "measurement_after", "10.9" },
                { "setting", "11" },
            };

            findings.ParseParameters(testParameters);

            Assert.AreEqual(int.Parse(testParameters["id"]), findings.Id);
            Assert.AreEqual(int.Parse(testParameters["task_data_id"]), findings.DataId);
            Assert.AreEqual(testParameters["name"], findings.Name);
            Assert.AreEqual(float.Parse(testParameters["tolerance"]), findings.Tolerance);
            Assert.IsTrue(findings.ToleranceIsPercent);
            Assert.AreEqual(testParameters["unit_of_measure"], findings.UnitOfMeasure);
            Assert.AreEqual(float.Parse(testParameters["measurement_before"]), findings.MeasurementBefore);
            Assert.AreEqual(float.Parse(testParameters["measurement_after"]), findings.MeasurementAfter);
            Assert.AreEqual(float.Parse(testParameters["setting"]), findings.Setting);
        }
    }
}
