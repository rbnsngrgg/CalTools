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
    public class TaskDataTest
    {
        private TaskData data;

        [TestInitialize]
        public void Init()
        {
            data = new();
        }
        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            Assert.AreEqual(-1, data.DataId);
            Assert.IsNull(data.TaskId);
            Assert.AreEqual("", data.SerialNumber);
            Assert.IsNull(data.StateBefore);
            Assert.IsNull(data.StateAfter);
            Assert.IsNull(data.Actions);
            Assert.IsNull(data.CompleteDate);
            Assert.AreEqual("", data.CompleteDateString);
            Assert.AreEqual("", data.Procedure);
            Assert.IsInstanceOfType(data.Findings, typeof(List<Findings>));
            Assert.AreEqual("", data.Remarks);
            Assert.AreEqual("", data.Technician);
            Assert.AreEqual("", data.Timestamp);
            Assert.IsInstanceOfType(data.DataFiles, typeof(List<TaskDataFile>));
            Assert.IsFalse(data.ChangesMade);
        }
        [TestMethod]
        public void TestParseParameters()
        {
            Dictionary<string, string> testParameters = new()
            {
                { "id", "101" },
                { "task_id", "102" },
                { "serial_number", "TestSN" },
                { "in_tolerance_before", "0" },
                { "operational_before", "0" },
                { "in_tolerance_after", "1" },
                { "operational_after", "1" },
                { "calibrated", "1" },
                { "verified", "0" },
                { "adjusted", "1" },
                { "repaired", "1" },
                { "maintenance", "0" },
                { "complete_date", "2021-01-01" },
                { "procedure", "999-9999" },
                { "remarks", "Test Remarks" },
                { "technician", "TestTech" },
                { "timestamp", "2021-01-01-13-12-11-123456" }
            };

            data.ParseParameters(testParameters);
            Assert.AreEqual(int.Parse(testParameters["id"]), data.DataId);
            Assert.AreEqual(int.Parse(testParameters["task_id"]), data.TaskId);
            Assert.AreEqual(testParameters["serial_number"], data.SerialNumber);
            Assert.AreEqual(new State()
            {
                InTolerance = false,
                Operational = false
            },
            data.StateBefore);
            Assert.AreEqual(new State()
            {
                InTolerance = true,
                Operational = true
            },
            data.StateAfter);
            Assert.AreEqual(new ActionTaken()
            {
                Calibration = true,
                Verification = false,
                Adjusted = true,
                Repaired = true,
                Maintenance = false
            },
            data.Actions);
            Assert.AreEqual(DateTime.ParseExact(testParameters["complete_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                data.CompleteDate);
            Assert.AreEqual(testParameters["complete_date"], data.CompleteDateString);
            Assert.AreEqual(testParameters["remarks"], data.Remarks);
            Assert.AreEqual(testParameters["technician"], data.Technician);
            Assert.AreEqual(testParameters["timestamp"], data.Timestamp);
            Assert.IsFalse(data.ChangesMade);
        }
    }
}
