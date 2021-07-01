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
    public class CTTaskTest
    {
        private CTTask task;
        private Mock<IDirectoryWrapper> mockDirectory;
        private string testTaskFolder = "C:\\101_Test";
        private string[] mockFilePaths = { "C:\\101_Test\\2021-01-01_TestItem.txt", "C:\\101_Test\\TestItem_2021-02-01.txt" };
        private string[] mockSubDirectories = { "C:\\101_Test\\2021-03-01_TestItem" };
        private List<TaskData> mockTaskData;

        [TestInitialize]
        public void Init()
        {
            mockTaskData = new();
            mockDirectory = new();
            mockDirectory.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(mockFilePaths);
            mockDirectory.Setup(x => x.GetDirectories(It.IsAny<string>())).Returns(mockSubDirectories);
            mockDirectory.Setup(x => x.GetParent(It.IsAny<string>())).Returns(new DirectoryInfo("(C:\\TestItem"));

            task = new CTTask(mockDirectory.Object);
        }
        [TestMethod]
        public void TestDefaultValues()
        {
            //Default values for a CTItem with no added information
            Assert.AreEqual(-1, task.TaskId);
            Assert.AreEqual("", task.SerialNumber);
            Assert.AreEqual("CALIBRATION", task.TaskTitle);
            Assert.AreEqual("", task.ServiceVendor);
            Assert.IsTrue(task.IsMandatory);
            Assert.AreEqual(12, task.Interval);
            Assert.IsNull(task.CompleteDate);
            Assert.IsNull(task.DueDate);
            Assert.IsTrue(task.IsDue);
            Assert.AreEqual("CALIBRATION", task.ActionType);
            Assert.AreEqual("", task.TaskDirectory);
            Assert.AreEqual("", task.Remarks);
            Assert.IsNull(task.DateOverride);
            Assert.IsFalse(task.ChangesMade);
            Assert.IsFalse(task.CompleteDateChanged);
            Assert.IsInstanceOfType(task.ServiceVendorList, typeof(List<string>));
            Assert.AreEqual(0, task.ServiceVendorList.Count);
        }
        [TestMethod]
        public void TestParseParameters()
        {
            Dictionary<string, string> testParameters = new()
            {
                { "id", "101" },
                { "serial_number", "TestSN" },
                { "task_title", "Test" },
                { "service_vendor", "Test Vendor" },
                { "is_mandatory", "0" },
                { "interval", "6" },
                { "complete_date", DateTime.Today.ToString("yyyy-MM-dd") },
                { "due_date", DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd") },
                { "is_due", "0" },
                { "action_type", "TEST" },
                { "directory", "Test directory" },
                { "remarks", "Test Remarks"},
                { "date_override", ""}
            };

            task.ParseParameters(testParameters);

            Assert.AreEqual(int.Parse(testParameters["id"]), task.TaskId);
            Assert.AreEqual(testParameters["serial_number"], task.SerialNumber);
            Assert.AreEqual(testParameters["task_title"], task.TaskTitle);
            Assert.AreEqual(testParameters["service_vendor"], task.ServiceVendor);
            Assert.IsFalse(task.IsMandatory);
            Assert.AreEqual(int.Parse(testParameters["interval"]), task.Interval);
            Assert.AreEqual(
                DateTime.ParseExact(testParameters["complete_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                task.CompleteDate);
            Assert.AreEqual(
                DateTime.ParseExact(testParameters["due_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                task.DueDate);
            Assert.IsFalse(task.IsDue);
            Assert.AreEqual(testParameters["action_type"], task.ActionType);
            Assert.AreEqual(testParameters["directory"], task.TaskDirectory);
            Assert.AreEqual(testParameters["remarks"], task.Remarks);
            Assert.IsNull(task.DateOverride);
            Assert.IsFalse(task.ChangesMade);

            testParameters.Remove("id");
            task = new(testParameters);
            Assert.AreEqual(-1, task.TaskId);
        }

        [TestMethod, TestCategory("CTTaskSetters")]
        public void TestCompleteDateUpdatesDueDate()
        {
            task.CompleteDate = DateTime.Today;
            Assert.AreEqual(DateTime.Today.AddMonths(task.Interval), task.DueDate);

            task.CompleteDate = null;
            Assert.IsNull(task.DueDate);
        }

        [TestMethod, TestCategory("CTTaskSetters")]
        public void TestIntervalUpdatesDueDate()
        {
            int newInterval = 6;
            task.CompleteDate = DateTime.Today;
            Assert.AreEqual(DateTime.Today.AddMonths(task.Interval), (DateTime)task.DueDate);
            Assert.AreNotEqual(DateTime.Today.AddMonths(newInterval), (DateTime)task.DueDate);

            task.Interval = newInterval;

            Assert.AreEqual(DateTime.Today.AddMonths(newInterval), task.DueDate);
        }

        [TestMethod]
        public void TestIsTaskDueWithinDays()
        {
            Assert.IsTrue(task.IsTaskDueWithinDays(0, DateTime.Today)); //Null due date

            task.CompleteDate = DateTime.Today; //Due in 12 months
            Assert.IsFalse(task.IsTaskDueWithinDays(30, DateTime.Today));

            task.CompleteDate = DateTime.Today.AddMonths(-11).AddDays(-10); //Less than one month from due date
            Assert.IsTrue(task.IsTaskDueWithinDays(30, DateTime.Today));
        }

        [TestMethod, TestCategory("CTTaskSetCompleteDate")]
        public void TestSetCompleteDateTaskDataIsLatest()
        {
            task.TaskId = 101;
            task.SerialNumber = "TestItem";
            DateTime latestDate = DateTime.ParseExact("2021-04-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            mockTaskData = new();
            mockTaskData.AddRange(new TaskData[] {
                new TaskData() { CompleteDate = null },
                new TaskData() { CompleteDate =  latestDate}
            });

            task.SetCompleteDateFromData(testTaskFolder, mockTaskData); //First parameter is mocked

            Assert.AreEqual(latestDate, task.CompleteDate);
        }
        [TestMethod, TestCategory("CTTaskSetCompleteDate")]
        public void TestSetCompleteDateFolderIsLatest()
        {
            task.TaskId = 101;
            task.SerialNumber = "TestItem";
            DateTime latestDate = DateTime.ParseExact("2021-03-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            task.SetCompleteDateFromData(testTaskFolder, mockTaskData);
            Assert.AreEqual(latestDate, task.CompleteDate);
        }
        [TestMethod, TestCategory("CTTaskSetCompleteDate")]
        public void TestSetCompleteDateFileIsLatest()
        {
            task.TaskId = 101;
            task.SerialNumber = "TestItem";
            DateTime latestDate = DateTime.ParseExact("2021-02-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);

            mockSubDirectories = new string[] { "C:\\101_Test\\2021-03-01_TestItem1" };
            mockDirectory.Setup(x => x.GetDirectories(It.IsAny<string>())).Returns(mockSubDirectories);

            task.SetCompleteDateFromData(testTaskFolder, mockTaskData);

            Assert.AreEqual(latestDate, task.CompleteDate);
        }
        [TestMethod, TestCategory("CTTaskSetCompleteDate")]
        public void TestSetCompleteDateNoData()
        {
            task.TaskId = 101;
            task.SerialNumber = "TestItem";
            mockFilePaths = new string[]{ "C:\\101_Test\\TestItem.txt", "C:\\101_Test\\TestItem2_2021-02-01.txt" };
            mockSubDirectories = new string[]{ "C:\\101_Test\\2021-03-01_TestItem1" };
            mockDirectory.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(mockFilePaths);
            mockDirectory.Setup(x => x.GetDirectories(It.IsAny<string>())).Returns(mockSubDirectories);

            task.SetCompleteDateFromData(testTaskFolder, mockTaskData);

            Assert.IsNull(task.CompleteDate);
        }

        [TestMethod]
        public void TestGetFolderIfExists()
        {
            task.TaskId = 101;
            string[] mockTaskDirectories = { "C:\\TestItem\\101_TestTask", "C:\\TestItem\\102_TestTask1", "C:\\TestItem\\103_TestTask2" };
            mockDirectory.Setup(x => x.GetDirectories(It.IsAny<string>())).Returns(mockTaskDirectories);
            mockDirectory.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

            Assert.AreEqual(mockTaskDirectories[0], task.GetTaskFolderIfExists());

            mockTaskDirectories = new string[]{ "C:\\TestItem\\104_TestTask3", "C:\\TestItem\\102_TestTask1", "C:\\TestItem\\103_TestTask2" };
            mockDirectory.Setup(x => x.GetDirectories(It.IsAny<string>())).Returns(mockTaskDirectories);
            Assert.AreEqual("", task.GetTaskFolderIfExists());
        }
    }
}
