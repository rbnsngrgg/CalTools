using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalTools_WPF;
using CalTools_WPF.ObjectClasses;
using CalTools_WPF.ObjectClasses.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Moq;

namespace CalToolsTests
{
    [TestClass]
    public class CTDatabaseTest
    {
        private Mock<SqliteConnectionHandler> mockHandler;
        private Mock<CTDatabase> database; //Mocked to verify calls to other database methods
        private readonly string testDbPath = "C:\\Test\\test.db";

        private Dictionary<string, string> taskDataTestParameters;


        [TestInitialize]
        public void Init()
        {
            taskDataTestParameters = new(){
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

            mockHandler = new(testDbPath);
            //CallBase == true for mocking one method while testing another
            database = new(testDbPath, mockHandler.Object) { CallBase = true };
            mockHandler.Setup(x => x.SelectAllFromTable(It.IsAny<string>()))
                .Returns(new List<Dictionary<string, string>>());
            mockHandler.Setup(x => x.SelectFromTableWhere(It.IsAny<string>(), It.IsAny<Dictionary<string,string>>()))
                .Returns(new List<Dictionary<string, string>>());
            mockHandler.Setup(x => x.SelectStandardEquipmentWhere(It.IsAny<Dictionary<string, string>>()))
                .Returns(new List<Dictionary<string, string>>());
            mockHandler.Setup(x => x.InsertIntoTable(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(1);
            mockHandler.Setup(x => x.UpdateTable(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>()));
        }

        [TestMethod]
        public void TestInitValues()
        {
            Assert.AreEqual("yyyy-MM-dd", database.Object.dateFormat);
            Assert.AreEqual("yyyy-MM-dd-HH-mm-ss-ffffff", database.Object.timestampFormat);
            Assert.AreEqual(7, database.Object.currentVersion);
        }

        [TestMethod, TestCategory("DataRetrieval")]
        public void TestGetAll()
        {
            Assert.IsInstanceOfType(database.Object.GetAll<CTItem>(), typeof(List<CTItem>));
            mockHandler.Verify(x => x.SelectAllFromTable(It.IsRegex("^items$")));

            Assert.IsInstanceOfType(database.Object.GetAll<CTTask>(), typeof(List<CTTask>));
            mockHandler.Verify(x => x.SelectAllFromTable(It.IsRegex("^tasks$")));

            Assert.IsInstanceOfType(database.Object.GetAll<TaskData>(), typeof(List<TaskData>));
            mockHandler.Verify(x => x.SelectAllFromTable(It.IsRegex("^task_data$")));
        }
        [TestMethod, TestCategory("DataRetrieval")]
        public void TestGetFromWhere()
        {
            List<CTItem> returnedItems = database.Object.GetFromWhere<CTItem>(new() { { "serial_number", "101" } });
            Assert.AreEqual(0, returnedItems.Count); //Item not found
            mockHandler.Verify(x => x.SelectFromTableWhere(It.IsRegex("^items$"),
                It.Is<Dictionary<string, string>>(x => x["serial_number"] == "101" && x.Count == 1))); //Verify database query with correct parameters

            database.Setup(x => x.AssignValues<CTItem>(It.IsAny<List<Dictionary<string,string>>>()))
                .Returns(new List<CTItem>() { new() { SerialNumber = "101" } }); //Simulate object found
            returnedItems = database.Object.GetFromWhere<CTItem>(new() { { "serial_number", "101" } });

            Assert.AreEqual(1, returnedItems.Count); //Item found
        }
        [TestMethod, TestCategory("DataRetrieval")]
        public void TestGetDataStandardEquipment()
        {
            List<CTStandardEquipment> returnedItems = database.Object.GetDataStandardEquipment(101);
            Assert.AreEqual(0, returnedItems.Count);
            mockHandler.Verify(x => x.SelectStandardEquipmentWhere(
                It.Is<Dictionary<string, string>>(x => x["task_data_id"] == "101" && x.Count == 1)));

            database.Setup(x => x.AssignValues<CTStandardEquipment>(It.IsAny<List<Dictionary<string, string>>>()))
                .Returns(new List<CTStandardEquipment>() { new() { SerialNumber = "102" } }); //Simulate object found
            returnedItems = database.Object.GetDataStandardEquipment(101);
            Assert.AreEqual(1, returnedItems.Count);
        }


        [TestMethod, TestCategory("DataSaving")]
        public void TestSaveItem()
        {
            Dictionary<string, string> testItemParameters = new()
            {
                { "serial_number", "TestSn" },
                { "location", "Test Location" },
                { "manufacturer", "Test Manufacturer" },
                { "directory", "Test Directory" },
                { "description", "Test Description" },
                { "in_service", "1" },
                { "model", "Test model" },
                { "certificate_number", "TestCertificate" },
                { "item_group", "Test item group" },
                { "remarks", "Test" },
                { "is_standard_equipment", "1" },
                { "timestamp", "2021-01-01-12-00-00-500000" }
            };

            CTItem testItem = new(testItemParameters);
            database.Object.SaveItem(testItem);

            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^items$"),
                It.Is<Dictionary<string, string>>(x => x["serial_number"] == "TestSn" && x.Count == 1),
                It.IsAny<bool>()));
            mockHandler.Verify(handler =>
                handler.UpdateTable(
                    It.IsRegex("^items$"),
                    It.Is<Dictionary<string, string>>(
                        colValues => VerifyParams(testItemParameters, colValues, false)),
                    It.Is<Dictionary<string, string>>(
                        whereValues => whereValues["serial_number"] == testItemParameters["serial_number"] &&
                            whereValues.Count == 1)));
        }
        [TestMethod, TestCategory("DataSaving")]
        public void TestSaveDataStandardEquipment()
        {
            //If ID pair exists in database
            database.Object.SaveDataStandardEquipment(101, 102);
            mockHandler.Verify(x => x.SelectFromTableWhere(
                It.IsRegex("^data_standard_equipment$"),
                It.Is<Dictionary<string,string>>(
                    x => x["task_data_id"] == "101" && x["standard_equipment_id"] == "102" && x.Count == 2)));

            //If ID pair doesn't exist yet
            mockHandler.Setup(x => x.SelectFromTableWhere(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(new List<Dictionary<string, string>>() { new(){ { "testKey", "testValue" } } });
            database.Object.SaveDataStandardEquipment(101, 102);
            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^data_standard_equipment$"),
                It.Is<Dictionary<string, string>>(
                    x => x["task_data_id"] == "101" && x["standard_equipment_id"] == "102" && x.Count == 2),
                false));
        }
        [TestMethod, TestCategory("DataSaving")]
        public void TestSaveStandardEquipment()
        {
            Dictionary<string, string> testItemParameters = new()
            {
                { "serial_number", "TestSn" },
                { "manufacturer", "Test Manufacturer" },
                { "description", "Test Description" },
                { "model", "Test model" },
                { "certificate_number", "TestCertificate" },
                { "item_group", "Test item group" },
                { "remarks", "Test" },
                { "timestamp", "2021-01-01-12-00-00-500000" },
                { "action_due_date", "2021-01-02"}
            };
            CTStandardEquipment testEquipment = new(testItemParameters);
            int equipmentId = database.Object.SaveStandardEquipment(testEquipment);

            Assert.AreEqual(1, equipmentId);
            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^standard_equipment$"),
                It.Is<Dictionary<string,string>>(x => VerifyParams(testItemParameters, x, false)),
                false));
        }
        [TestMethod, TestCategory("DataSaving")]
        public void TestSaveTask()
        {
            DateTime testCompleteDate = DateTime.Today.AddMonths(-5);
            DateTime testDueDate = testCompleteDate.AddMonths(6);
            Dictionary<string, string> testParameters = new()
            {
                { "id", "101" },
                { "serial_number", "102" },
                { "task_title", "Test Task" },
                { "service_vendor", "Test vendor" },
                { "is_mandatory", "0" },
                { "interval", "6" },
                { "complete_date", testCompleteDate.ToString("yyyy-MM-dd") },
                { "due_date", testDueDate.ToString("yyyy-MM-dd") },
                { "is_due", "0" },
                { "action_type", "CALIBRATION" },
                { "directory", "Test directory" },
                { "remarks", "Test remarks" },
                { "date_override", "" },
            };

            CTTask testTask = new(testParameters);
            int taskId = database.Object.SaveTask(testTask);

            Assert.AreEqual(101, taskId);
            mockHandler.Verify(x => x.UpdateTable(
                It.IsRegex("^tasks$"),
                It.Is<Dictionary<string,string>>(
                    colValues => VerifyParams(testParameters, colValues, false)),
                It.Is<Dictionary<string,string>>(
                    whereValues => whereValues["id"] == "101" && whereValues.Count == 1)));

            testParameters.Remove("id");
            testTask = new(testParameters);
            taskId = database.Object.SaveTask(testTask);

            Assert.AreEqual(1, taskId);
            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^tasks$"),
                It.Is<Dictionary<string, string>>(
                    colValues => VerifyParams(testParameters, colValues, false)),
                true));
        }

        [TestMethod, TestCategory("DataSaving"), TestCategory("SaveTaskData")]
        public void TestSaveTaskDataArgumentException()
        {
            //Test that no handler functions are invoked upon argument exception
            database.Setup(x => x.CheckStandardEquipment(It.IsAny<List<CTStandardEquipment>>())).Throws(new ArgumentException());
            database.Object.SaveTaskData(new() { TaskId = 101 });
            mockHandler.Verify(x => x.InsertIntoTable(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<bool>()), Times.Never);
        }
        [TestMethod, TestCategory("DataSaving"), TestCategory("SaveTaskData")]
        public void TestSaveTaskDataInsert()
        {
            //Test that handler.InsertIntoTable is called with the correct parameters
            TaskData testData = new(taskDataTestParameters);
            database.Object.SaveTaskData(testData);

            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^task_data$"),
                It.Is<Dictionary<string,string>>(colValues => VerifyParams(taskDataTestParameters, colValues, true)),
                false));

            taskDataTestParameters["id"] = "-1"; //If task's ID == -1, the id should not be included in the dict passed to InsertIntoTable
            testData = new(taskDataTestParameters);
            taskDataTestParameters.Remove("id"); //Should now be equivalent to the dict passed to InsertIntoTable

            database.Object.SaveTaskData(testData);

            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^task_data$"),
                It.Is<Dictionary<string, string>>(colValues => VerifyParams(taskDataTestParameters, colValues, true)),
                false));
        }
        [TestMethod, TestCategory("DataSaving"), TestCategory("SaveTaskData")]
        public void TestSaveTaskDataSaveComponents()
        {
            //Test that saveParameter is invoked for each finding
            //Test that SaveDataStandardEquipment is invoked for each equipment id
            //Test that SaveTaskDataFiles is invoked
            List<Findings> testFindings = new() { new(), new() };
            TaskData testData = new(taskDataTestParameters) { Findings = testFindings };
            List<int> equipmentIds = new() { 1, 2, 3 };
            database.Setup(x => x.CheckStandardEquipment(It.IsAny<List<CTStandardEquipment>>()))
                .Returns(equipmentIds);

            database.Object.SaveTaskData(testData);

            database.Verify(x => x.SaveFindings(
                It.Is<Findings>(f => f.DataId == testData.DataId)), Times.Exactly(testFindings.Count));
            database.Verify(x => x.SaveDataStandardEquipment(
                It.Is<int>(id => id == testData.DataId),
                It.Is<int>(eId => equipmentIds.Contains(eId))),
                Times.Exactly(equipmentIds.Count));
            database.Verify(x => x.SaveTaskDataFiles(It.Is<TaskData>(t => t == testData)));
        }

        [TestMethod, TestCategory("DataSaving")]
        public void TestSaveTaskDataFiles()
        {
            TaskData testData = new(taskDataTestParameters);
            List<TaskDataFile> dataFiles = new() {
                new() { Description = "Test File 1", Location = "Test Location 1" },
                new() { Description = "Test File 2", Location = "Test Location 2" }
            };
            testData.DataFiles = dataFiles;
            List<Dictionary<string, string>> selectReturn = new()
                { new() { { "task_data_id", "101" }, { "descripion", "Test File X" }, { "location", "Test Location X" } } };
            mockHandler.Setup(x => x.SelectFromTableWhere(
                It.IsRegex("^task_data_files$"),
                It.IsAny<Dictionary<string, string>>()))
                .Returns(selectReturn);

            database.Object.SaveTaskDataFiles(testData);

            mockHandler.Verify(x => x.InsertIntoTable(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<bool>()), Times.Never);

            mockHandler.Setup(x => x.SelectFromTableWhere(
                It.IsRegex("^task_data_files$"),
                It.IsAny<Dictionary<string, string>>()))
                .Returns(new List<Dictionary<string, string>>());

            database.Object.SaveTaskDataFiles(testData);
            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^task_data_files$"),
                It.IsAny<Dictionary<string, string>>(), false),
                Times.Exactly(dataFiles.Count));
        }

        [TestMethod, TestCategory("DataSaving")]
        public void TestSaveFindings()
        {
            Dictionary<string, string> testParameters = new() {
                { "id", "101" },
                { "task_data_id", "102"},
                { "name", "Test name" },
                { "tolerance", "10" },
                { "tolerance_is_percent", "1" },
                { "unit_of_measure", "Test UoM" },
                { "measurement_before", "4" },
                { "measurement_after", "5" },
                { "setting", "5" },
            };
            Findings testFindings = new(testParameters);

            database.Object.SaveFindings(testFindings);

            testParameters.Remove("id");
            mockHandler.Verify(x => x.InsertIntoTable(
                It.IsRegex("^findings$"),
                It.Is<Dictionary<string,string>>(colValues => VerifyParams(testParameters, colValues, true)),
                false));

            testParameters.Remove("task_data_id");
            testFindings = new(testParameters);
            Assert.ThrowsException<ArgumentException>(() => database.Object.SaveFindings(testFindings));
        }

        [TestMethod, TestCategory("DataParsing")]
        public void TestAssignValues()
        {
            TaskData testData = new(taskDataTestParameters);
        }

        private bool VerifyParams(Dictionary<string, string> expected, Dictionary<string, string> actual, bool equalTimestamp)
        {
            foreach (string key in expected.Keys)
            {
                if (key != "timestamp" && expected[key] != actual[key])
                {
                    return false;
                }
            }
            if (expected.ContainsKey("timestamp"))
            {
                if (equalTimestamp)
                {
                    return actual["timestamp"] == expected["timestamp"] && expected.Count == actual.Count;
                }
                else
                {
                    return DateTime.ParseExact(actual["timestamp"], database.Object.timestampFormat, CultureInfo.InvariantCulture) >
                        DateTime.ParseExact(expected["timestamp"], database.Object.timestampFormat, CultureInfo.InvariantCulture)
                        && expected.Count == actual.Count;
                }
            }
            return true;
        }
    }
}
