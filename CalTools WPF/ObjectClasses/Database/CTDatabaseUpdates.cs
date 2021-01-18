using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
//using System.Diagnostics;

namespace CalTools_WPF
{
    //Contains the CTDatabase methods related to checking and updating the structure and version of the SQLite database.
    partial class CTDatabase
    {
        private void AssignItemValuesLegacy(ref CalibrationItemV4 item)
        {
            item.Location = reader.GetString((int)CalibrationItemV4.DatabaseColumns.location);
            item.Interval = reader.GetInt32((int)CalibrationItemV4.DatabaseColumns.interval);
            item.CalVendor = reader.GetString((int)CalibrationItemV4.DatabaseColumns.cal_vendor);
            item.Manufacturer = reader.GetString((int)CalibrationItemV4.DatabaseColumns.manufacturer);
            if (reader.GetString(5).Length > 0) { item.LastCal = DateTime.ParseExact(reader.GetString((int)CalibrationItemV4.DatabaseColumns.lastcal), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString(6).Length > 0) { item.NextCal = DateTime.ParseExact(reader.GetString((int)CalibrationItemV4.DatabaseColumns.nextcal), dateFormat, CultureInfo.InvariantCulture); }
            item.Mandatory = reader.GetString((int)CalibrationItemV4.DatabaseColumns.mandatory) == "1";
            item.Directory = reader.GetString((int)CalibrationItemV4.DatabaseColumns.directory);
            item.Description = reader.GetString((int)CalibrationItemV4.DatabaseColumns.description);
            item.InService = reader.GetString((int)CalibrationItemV4.DatabaseColumns.inservice) == "1";
            if (reader.GetString(11).Length > 0) { item.InServiceDate = DateTime.ParseExact(reader.GetString((int)CalibrationItemV4.DatabaseColumns.inservicedate), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString(12).Length > 0) { item.OutOfServiceDate = DateTime.ParseExact(reader.GetString((int)CalibrationItemV4.DatabaseColumns.outofservicedate), dateFormat, CultureInfo.InvariantCulture); }
            item.CalDue = reader.GetString((int)CalibrationItemV4.DatabaseColumns.caldue) == "1";
            item.Model = reader.GetString((int)CalibrationItemV4.DatabaseColumns.model);
            item.Comment = reader.GetString((int)CalibrationItemV4.DatabaseColumns.comments);
            if (reader.GetString(16).Length > 0) { item.TimeStamp = DateTime.ParseExact(reader.GetString((int)CalibrationItemV4.DatabaseColumns.timestamp), timestampFormat, CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString((int)CalibrationItemV4.DatabaseColumns.item_group);
            item.VerifyOrCalibrate = reader.GetString((int)CalibrationItemV4.DatabaseColumns.verify_or_calibrate);
            item.StandardEquipment = reader.GetString((int)CalibrationItemV4.DatabaseColumns.standard_equipment) == "1";
            item.CertificateNumber = reader.GetString((int)CalibrationItemV4.DatabaseColumns.certificate_number);
        }
        private void AssignDataValuesLegacy(ref CalibrationDataV4 data)
        {
            data.ID = reader.GetInt32((int)CalibrationDataV4.DatabaseColumns.ColID);
            data.SerialNumber = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColSerialNumber);
            data.StateBefore = JsonConvert.DeserializeObject<State>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColStateBeforeAction));
            data.StateAfter = JsonConvert.DeserializeObject<State>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColStateAfterAction));
            data.ActionTaken = JsonConvert.DeserializeObject<ActionTaken>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColActionTaken));
            if (reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColCalibrationDate).Length > 0)
            { data.CalibrationDate = DateTime.ParseExact(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColCalibrationDate), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColDueDate).Length > 0)
            { data.DueDate = DateTime.ParseExact(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColDueDate), dateFormat, CultureInfo.InvariantCulture); }
            data.Procedure = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColProcedure);
            data.StandardEquipment = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColStandardEquipment);
            data.findings = JsonConvert.DeserializeObject<Findings>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColFindings));
            data.Remarks = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColRemarks);
            data.Technician = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColTechnician);
            data.Timestamp = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColEntryTimestamp);
        }
        private void ConvertFileStructure() //Converts the file structure from V4 to V5, adding folders for each task
        {
            List<CTItem> allItems = GetAllItems();
            foreach (string folder in Directory.GetDirectories(ItemScansDir))
            {
                foreach (string configFolder in Folders)
                {
                    //If folder in ItemScansDir is a folder specified in config
                    if (folder.Contains(configFolder))
                    {
                        foreach (string itemFolder in Directory.GetDirectories(folder))
                        {
                            bool itemExists = false;
                            //Get DB item that matches current folder
                            foreach (CTItem item in allItems)
                            {
                                if (Path.GetFileName(itemFolder) == item.SerialNumber)
                                {
                                    itemExists = true;
                                    if (itemFolder != item.Directory) { item.Directory = itemFolder; }
                                    foreach (CTTask task in GetTasks("SerialNumber", item.SerialNumber))
                                    {
                                        //Create task folder, then move all files to the task folder.
                                        MoveToTaskFolder(item, task);
                                    }
                                    if (item.ChangesMade) { SaveItem(item); }
                                    break;
                                }
                            }
                            if (itemExists) { continue; }
                            else if (GetItem("SerialNumber", Path.GetFileName(itemFolder)) == null)
                            {
                                CTItem newItem = new CTItem(Path.GetFileName(itemFolder));
                                newItem.Directory = itemFolder;
                                SaveItem(newItem);
                                CTTask newTask = new CTTask();
                                newTask.SerialNumber = newItem.SerialNumber;
                                SaveTask(newTask);
                                MoveToTaskFolder(newItem, GetTasks("SerialNumber", newItem.SerialNumber)[0]);
                            }
                        }
                    }
                }
            }
        }
        private void CreateV5Tables()
        {
            //Assumes open connection, create the current structure if it isn't present.
            string command = "CREATE TABLE IF NOT EXISTS Items (" +
                    "SerialNumber TEXT PRIMARY KEY," +
                    "Location TEXT DEFAULT ''," +
                    "Manufacturer TEXT DEFAULT ''," +
                    "Directory TEXT DEFAULT ''," +
                    "Description TEXT DEFAULT ''," +
                    "InService INTEGER DEFAULT 1," +
                    "InServiceDate TEXT DEFAULT ''," +
                    "Model TEXT DEFAULT ''," +
                    "Comment TEXT DEFAULT ''," +
                    "Timestamp TEXT DEFAULT ''," +
                    "ItemGroup TEXT DEFAULT ''," +
                    "StandardEquipment INTEGER DEFAULT 0," +
                    "CertificateNumber TEXT DEFAULT '')";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS Tasks (" +
                    "TaskID INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "SerialNumber TEXT," +
                    "TaskTitle TEXT DEFAULT ''," +
                    "ServiceVendor TEXT DEFAULT ''," +
                    "Mandatory INTEGER DEFAULT 1," +
                    "Interval INTEGER DEFAULT 12," +
                    "CompleteDate TEXT DEFAULT ''," +
                    "DueDate TEXT DEFAULT ''," +
                    "Due INTEGER DEFAULT 0," +
                    "ActionType TEXT DEFAULT 'CALIBRATION'," +
                    "Directory TEXT DEFAULT ''," +
                    "Comments TEXT DEFAULT ''," +
                    "FOREIGN KEY(SerialNumber) REFERENCES Items(SerialNumber) ON DELETE CASCADE ON UPDATE CASCADE)";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS TaskData (" +
                    "DataID INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "TaskID INTEGER," +
                    "SerialNumber TEXT," +
                    "StateBeforeAction TEXT DEFAULT ''," +
                    "StateAfterAction TEXT DEFAULT ''," +
                    "ActionTaken TEXT DEFAULT ''," +
                    "CompleteDate TEXT DEFAULT ''," +
                    "Procedure TEXT DEFAULT ''," +
                    "StandardEquipment TEXT DEFAULT ''," +
                    "Findings TEXT DEFAULT ''," +
                    "Remarks TEXT DEFAULT ''," +
                    "Technician TEXT DEFAULT ''," +
                    "EntryTimestamp TEXT DEFAULT ''," +
                    "FOREIGN KEY(TaskID) REFERENCES Tasks(TaskID) ON DELETE CASCADE)";
            Execute(command);
            tablesExist = true;
        }
        private void FromVersion4() //Gets all CalibrationItems and CalibrationData, converts to Items, Tasks, and TaskData
        {
            List<CalibrationItemV4> legacyItems = GetAllItemsLegacy();
            if (!IsConnected()) { conn.Open(); }
            //Convert old CalibrationItems to CTItems and CTTasks
            foreach (CalibrationItemV4 calItem in legacyItems)
            {
                CTItem item = new CTItem(calItem.SerialNumber);
                item.Location = calItem.Location;
                item.Manufacturer = calItem.Manufacturer;
                item.Directory = calItem.Directory;
                item.Description = calItem.Description;
                item.InService = calItem.InService;
                item.InServiceDate = calItem.InServiceDate;
                item.Model = calItem.Model;
                item.Comment = calItem.Comment;
                item.ItemGroup = calItem.ItemGroup;
                item.StandardEquipment = calItem.StandardEquipment;
                item.CertificateNumber = calItem.CertificateNumber;

                CTTask task = new CTTask();
                task.SerialNumber = calItem.SerialNumber;
                task.TaskTitle = calItem.VerifyOrCalibrate;
                task.ServiceVendor = calItem.CalVendor;
                task.Mandatory = calItem.Mandatory;
                task.Interval = calItem.Interval;
                task.CompleteDate = calItem.lastCal;
                task.DueDate = calItem.NextCal;
                task.Due = calItem.CalDue;
                task.ActionType = calItem.VerifyOrCalibrate;

                SaveItem(item, false);
                SaveTask(task);
            }
            //Convert old CalibrationData to TaskData
            foreach (CalibrationDataV4 calData in GetAllCalDataLegacy())
            {
                TaskData taskData = new TaskData();
                taskData.TaskID = GetTasks("SerialNumber", calData.SerialNumber, false)[0].TaskID;
                taskData.SerialNumber = calData.SerialNumber;
                taskData.StateBefore = calData.StateBefore;
                taskData.StateAfter = calData.StateAfter;
                taskData.ActionTaken = calData.ActionTaken;
                taskData.CompleteDate = calData.CalibrationDate;
                taskData.Procedure = calData.Procedure;
                taskData.StandardEquipment = calData.StandardEquipment;
                taskData.Findings = calData.findings;
                taskData.Remarks = calData.Remarks;
                taskData.Technician = calData.Technician;
                taskData.Timestamp = calData.Timestamp;

                SaveTaskData(taskData, true);
            }
            ResetConnection();
            string command = "DROP TABLE IF EXISTS calibration_items";
            Execute(command);
            ResetConnection();
            command = "DROP TABLE IF EXISTS calibration_data";
            Execute(command);
            ResetConnection();
            command = "PRAGMA user_version = 5";
            Execute(command);
            ResetConnection();
        }
        public List<CalibrationDataV4> GetAllCalDataLegacy()
        {
            List<CalibrationDataV4> calData = new List<CalibrationDataV4>();
            if (Connect())
            {
                string command = $"SELECT * FROM calibration_data";
                Execute(command);
                while (reader.Read())
                {
                    CalibrationDataV4 data = new CalibrationDataV4();
                    AssignDataValuesLegacy(ref data);
                    calData.Add(data);
                }
            }
            return calData;
        }
        public List<CalibrationItemV4> GetAllItemsLegacy()
        {
            List<CalibrationItemV4> allItems = new List<CalibrationItemV4>();
            string command = "SELECT * FROM calibration_items";
            if (!Connect()) { return allItems; }
            Execute(command);
            while (reader.Read())
            {
                CalibrationItemV4 item = new CalibrationItemV4(reader.GetString(0));
                AssignItemValuesLegacy(ref item);
                allItems.Add(item);
            }
            return allItems;
        }
        private void MoveToTaskFolder(CTItem item, CTTask task) //Move existing files to the new task folder
        {
            if (item == null | task == null) { return; }
            string taskFolder = Path.Combine(item.Directory, $"{task.TaskID}_{task.TaskTitle}");
            if (Directory.Exists(taskFolder)) { return; }
            Directory.CreateDirectory(taskFolder);
            foreach (string file in Directory.GetFiles(item.Directory))
            {
                string newLocation = Path.Combine(taskFolder, Path.GetFileName(file));
                File.Move(file, newLocation);
            }
        }
        private bool UpdateDatabase()
        {
            try
            {
                //Check DB version
                int currentVersion = 6;
                int dbVersion = GetDatabaseVersion();
                if (dbVersion == currentVersion)
                {
                    return true;
                }
                //Reset connection to prevent db table from being locked.
                ResetConnection();
                string command = "DROP TABLE IF EXISTS item_groups";
                Execute(command);
                CreateV5Tables();
                while (dbVersion < currentVersion)
                {
                    if (dbVersion < 4)
                    {
                        ConvertFileStructure();
                        command = "PRAGMA user_version = 5";
                        Execute(command);
                    }
                    else if (dbVersion == 4) { FromVersion4(); ConvertFileStructure(); ResetConnection(); }
                    else if (dbVersion == 5)
                    {
                        Execute("ALTER TABLE Tasks ADD COLUMN ManualFlag TEXT DEFAULT ''");
                        Execute("PRAGMA user_version = 6");
                    }
                    dbVersion = GetDatabaseVersion();
                }
                if (dbVersion > currentVersion)
                {
                    if (MessageBox.Show($"This version of CalTools is outdated and uses database version {currentVersion}. The current database version is {dbVersion}. Some features may be missing or broken. Continue?",
                        "Outdated Version", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                    { return true; }
                    else { return false; }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, " Update Database, SQLite Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
