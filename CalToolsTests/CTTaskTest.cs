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
        public void TestDatabaseColumns()
        {
            Assert.AreEqual((int)CTTask.DatabaseColumns.TaskID, 0);
            Assert.AreEqual((int)CTTask.DatabaseColumns.SerialNumber, 1);
            Assert.AreEqual((int)CTTask.DatabaseColumns.TaskTitle, 2);
            Assert.AreEqual((int)CTTask.DatabaseColumns.ServiceVendor, 3);
            Assert.AreEqual((int)CTTask.DatabaseColumns.Mandatory, 4);
            Assert.AreEqual((int)CTTask.DatabaseColumns.Interval, 5);
            Assert.AreEqual((int)CTTask.DatabaseColumns.CompleteDate, 6);
            Assert.AreEqual((int)CTTask.DatabaseColumns.DueDate, 7);
            Assert.AreEqual((int)CTTask.DatabaseColumns.Due, 8);
            Assert.AreEqual((int)CTTask.DatabaseColumns.ActionType, 9);
            Assert.AreEqual((int)CTTask.DatabaseColumns.Directory, 10);
            Assert.AreEqual((int)CTTask.DatabaseColumns.Comments, 11);
            Assert.AreEqual((int)CTTask.DatabaseColumns.ManualFlag, 12);
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
