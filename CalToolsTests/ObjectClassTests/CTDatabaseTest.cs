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
        [TestInitialize]
        public void Init()
        {
            mockHandler = new(testDbPath);
            //CallBase == true for mocking one method while testing another
            database = new(testDbPath, mockHandler.Object) { CallBase = true };
            mockHandler.Setup(x => x.SelectAllFromTable(It.IsAny<string>()))
                .Returns(new List<Dictionary<string, string>>());
            mockHandler.Setup(x => x.SelectFromTableWhere(It.IsAny<string>(), It.IsAny<Dictionary<string,string>>()))
                .Returns(new List<Dictionary<string, string>>());
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
                It.Is<Dictionary<string, string>>(x => x["serial_number"] == "101" && x.Keys.Count == 1))); //Verify database query with correct parameters

            database.Setup(x => x.AssignValues<CTItem>(It.IsAny<List<Dictionary<string,string>>>()))
                .Returns(new List<CTItem>() { new() { SerialNumber = "101" } }); //Simulate object found
            returnedItems = database.Object.GetFromWhere<CTItem>(new() { { "serial_number", "101" } });

            Assert.AreEqual(1, returnedItems.Count); //Item found
        }
    }
}
