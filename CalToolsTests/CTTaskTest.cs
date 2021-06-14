using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalTools_WPF;
using System;

namespace CalToolsTests
{
    [TestClass]
    public class CTTaskTest
    {
        CTTask task;

        [TestInitialize]
        public void TestInit()
        {
            task = new CTTask();
        }
        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            Assert.AreEqual(task.TaskID, -1);
            Assert.AreEqual(task.SerialNumber, "");
            Assert.AreEqual(task.TaskTitle, "CALIBRATION");
            Assert.AreEqual(task.ServiceVendor, "");
            Assert.IsTrue(task.Mandatory);
            Assert.AreEqual(task.Interval, 12);
            Assert.IsNull(task.CompleteDate);
            Assert.AreEqual(task.CompleteDateString, "");
            Assert.IsNull(task.DueDate);
            Assert.IsTrue(task.Due);
            Assert.AreEqual(task.ActionType, "CALIBRATION");
            Assert.AreEqual(task.TaskDirectory, "");
            Assert.AreEqual(task.Comment, "");
            Assert.IsNull(task.DateOverride);
            Assert.IsFalse(task.ChangesMade);
        }

        [TestMethod]
        public void TestIsTaskDue()
        {
            DateTime checkDate = DateTime.UtcNow.AddDays(30);
            Assert.IsTrue(task.IsTaskDue(30, checkDate)); //Task has no complete date, so it is due.

            task.CompleteDate = DateTime.UtcNow;
            task.DueDate = task.CompleteDate.Value.AddDays(31); //Due date outside of 30 day window. Task should not be marked as due
            Assert.IsFalse(task.IsTaskDue(30, DateTime.UtcNow));

            task.DueDate = task.CompleteDate.Value.AddDays(-15); //Due date is within 15 days. Should be due.
            Assert.IsTrue(task.IsTaskDue(30, DateTime.UtcNow));
        }
    }
}
