using CalTools_WPF.ObjectClasses;
using IronXL.Xml.Dml;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CalTools_WPF
{
    class CTDatabase
    {
        public readonly string dateFormat = "yyyy-MM-dd";
        public readonly string timestampFormat = "yyyy-MM-dd-HH-mm-ss-ffffff";
        public bool tablesExist = false;
        private SqliteConnection conn;
        private SqliteDataReader reader;
        public string DbPath { get; set; }
        public CTDatabase(string dbPath)
        {
            this.DbPath = dbPath;
            conn = new SqliteConnection($"Data Source={DbPath}");
        }
        public bool DatabaseReady()
        {
            if (Connect())
            { if (Disconnect()) { return true; } else { return false; } }
            else { return false; }
        }
        public bool IsConnected()
        {
            if (conn.State == System.Data.ConnectionState.Open) { return true; }
            else { return false; }
        }
        public void Execute(string com)
        {
            SqliteCommand command = new SqliteCommand(com, conn);
            reader = command.ExecuteReader();
        }

        //Data Retrieval-------------------------------------------------------------------------------------------------------------------
        public List<CTItem> GetAllItems()
        {
            List<CTItem> allItems = new List<CTItem>();
            string command = "SELECT * FROM Items";
            if (!Connect()) { return allItems; }
            Execute(command);
            while (reader.Read())
            {
                CTItem item = new CTItem(reader.GetString(0));
                AssignItemValues(ref item);
                allItems.Add(item);
            }
            Disconnect();
            return allItems;
        }
        public List<CTTask> GetAllTasks()
        {
            List<CTTask> allTasks = new List<CTTask>();
            string command = "SELECT * FROM Tasks";
            if (!Connect()) { return allTasks; }
            Execute(command);
            while (reader.Read())
            {
                CTTask task = new CTTask();
                AssignTaskValues(ref task);
                allTasks.Add(task);
            }
            Disconnect();
            return allTasks;
        }
        public List<TaskData> GetTaskData(string taskID)
        {
            List<TaskData> calData = new List<TaskData>();
            if (Connect())
            {
                string command = $"SELECT * FROM TaskData WHERE TaskID='{taskID}'";
                Execute(command);
                while (reader.Read())
                {
                    TaskData data = new TaskData();
                    AssignDataValues(ref data);
                    calData.Add(data);
                }
                Disconnect();
            }
            return calData;
        }
        public List<TaskData> GetAllTaskData()
        {
            List<TaskData> calData = new List<TaskData>();
            if (Connect())
            {
                string command = $"SELECT * FROM TaskData";
                Execute(command);
                while (reader.Read())
                {
                    TaskData data = new TaskData();
                    AssignDataValues(ref data);
                    calData.Add(data);
                }
                Disconnect();
            }
            return calData;
        }
#nullable enable
        public CTItem? GetItem(string col, string item)
        {
            string command = $" SELECT * FROM Items WHERE {col}='{item}'";
            if (!Connect()) return null;
            Execute(command);
            if (reader.Read())
            {
                CTItem returnItem = new CTItem(reader.GetString(0));
                AssignItemValues(ref returnItem);
                Disconnect();
                return returnItem;
            }
            Disconnect();
            return null;
        }
        public CTItem? GetItemFromTask(CTTask task)
        {
            string command = $"SELECT * FROM Items WHERE SerialNumber='{task.SerialNumber}'";
            if (!Connect()) return null;
            Execute(command);
            if (reader.Read())
            {
                CTItem returnItem = new CTItem(reader.GetString(0));
                AssignItemValues(ref returnItem);
                Disconnect();
                return returnItem;
            }
            Disconnect();
            return null;
        }
        public List<CTTask> GetTasks(string col, string item, bool disconnect = true)
        {
            List<CTTask> tasks = new List<CTTask>();
            string command = $"SELECT * FROM Tasks WHERE {col}='{item}'";
            if (!Connect()) return tasks;
            Execute(command);
            while (reader.Read())
            {
                CTTask task = new CTTask();
                AssignTaskValues(ref task);
                tasks.Add(task);
            }
            if (disconnect) { Disconnect(); }
            return tasks;
        }
        public TaskData? GetData(string col, string item)
        {
            string command = $" SELECT * FROM TaskData WHERE {col}='{item}'";
            if (!Connect()) return null;
            Execute(command);
            if (reader.Read())
            {
                TaskData returnItem = new TaskData();
                AssignDataValues(ref returnItem);
                Disconnect();
                return returnItem;
            }
            Disconnect();
            return null;
        }
#nullable disable
        //Save data------------------------------------------------------------------------------------------------------------------------
        public bool CreateItem(string sn)
        {
            try
            {
                if (Connect())
                {
                    string command = $"INSERT OR IGNORE INTO Items (SerialNumber) VALUES ('{sn}')";
                    Execute(command);
                    Disconnect();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool SaveItem(CTItem item, bool disconnect = true)
        {
            try
            {
                if (Connect())
                {
                    string command = $"INSERT OR IGNORE INTO Items (SerialNumber) VALUES ('{item.SerialNumber.Replace("'", "''")}')";
                    Execute(command);
                    command = $"UPDATE Items SET SerialNumber='{item.SerialNumber.Replace("'", "''")}'," +
                        $"Model='{item.Model.Replace("'", "''")}'," +
                        $"Description='{item.Description.Replace("'", "''")}'," +
                        $"Location='{item.Location.Replace("'", "''")}'," +
                        $"Manufacturer='{item.Manufacturer.Replace("'", "''")}'," +
                        $"Directory='{item.Directory.Replace("'", "''")}'," +
                        $"InService='{(item.InService == true ? 1 : 0)}'," +
                        $"InServiceDate='{(item.InServiceDate == null ? "" : item.InServiceDate.Value.ToString(dateFormat, CultureInfo.InvariantCulture))}'," +
                        $"Comment='{item.Comment.Replace("'", "''")}'," +
                        $"Timestamp='{DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture)}'," +
                        $"ItemGroup='{item.ItemGroup.Replace("'", "''")}'," +
                        $"CertificateNumber='{item.CertificateNumber.Replace("'", "''")}'," +
                        $"StandardEquipment='{(item.StandardEquipment == true ? 1 : 0)}' " +
                        $"WHERE SerialNumber='{item.SerialNumber.Replace("'", "''")}'";
                    Execute(command);
                    if (disconnect) { Disconnect(); }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool UpdateColumn(string sn, string column, string newValue)
        {
            try
            {
                if (Connect())
                {
                    string command = $"UPDATE Items SET {column}='{newValue}' WHERE SerialNumber='{sn}'";
                    Execute(command);
                    Disconnect();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool SaveTask(CTTask task, bool disconnect = false)
        {
            try 
            {
                if (Connect())
                {
                    string command = $"INSERT INTO Tasks (SerialNumber,TaskTitle,ServiceVendor,Mandatory,Interval,CompleteDate,DueDate,Due,ActionType,Comments) " +
                        $"VALUES ('{task.SerialNumber}','{task.TaskTitle}','{task.ServiceVendor}','{task.Mandatory}','{task.Interval}','{task.CompleteDateString}'," +
                        $"'{task.DueDateString}','{(task.Due == true ? 1 : 0)}','{task.ActionType}','{task.Comment}')";
                    Execute(command);
                    if (disconnect) { Disconnect(); }
                    return true;
                }
            else { return false; }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
}
        public bool SaveTaskData(TaskData data, bool timestampOverride = false, bool disconnect = false)
        {
            try
            {
                if (Connect())
                {
                    string command = $"INSERT INTO TaskData (TaskID,SerialNumber,StateBeforeAction,StateAfterAction,ActionTaken,CompleteDate,Procedure,StandardEquipment," +
                        $"Findings,Remarks,Technician,EntryTimeStamp) " +
                        $"VALUES ('{data.TaskID}','{data.SerialNumber.Replace("'", "''")}','{JsonConvert.SerializeObject(data.StateBefore)}','{JsonConvert.SerializeObject(data.StateAfter)}'," +
                        $"'{JsonConvert.SerializeObject(data.ActionTaken)}','{data.CompleteDate.Value.ToString(dateFormat)}'," +
                        $"'{data.Procedure.Replace("'", "''")}','{data.StandardEquipment.Replace("'", "''")}','{JsonConvert.SerializeObject(data.Findings).Replace("'", "''")}','{data.Remarks.Replace("'", "''")}','{data.Technician.Replace("'", "''")}'," +
                        $"'{(timestampOverride ? data.Timestamp : DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture))}')";
                    Execute(command);
                    if (disconnect) { Disconnect(); }
                    return true;
                }
                else { return false; }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        //Remove data----------------------------------------------------------------------------------------------------------------------
        public bool RemoveCalItem(string sn)
        {
            try
            {
                if (Connect())
                {
                    string command = $"DELETE FROM Items WHERE SerialNumber='{sn}'";
                    Execute(command);
                    Disconnect();
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error while removing item from the database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool RemoveCalData(string id)
        {
            try
            {
                if(Connect())
                {
                    string command = $"DELETE FROM TaskData WHERE DataID='{id}'";
                    Execute(command);
                    Disconnect();
                    return true;
                }
                return false;
            }
            catch(System.Exception ex)
            {
                MessageBox.Show($"Error while removing item from the database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        //Private members
        private bool Connect()
        {
            try
            {
                if (!IsConnected())
                {
                    conn.Open();
                    //Assumes blank DB. Should only run CreateTables() once.
                    if (!tablesExist)
                    {
                        if (UpdateDatabase())
                        { tablesExist = true; }
                        else { Disconnect(); return false; }
                    }
                }
                return true;
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private bool Disconnect() //True if disconnected successfully, false if error
        {
            if (IsConnected())
            {
                try
                {
                    conn.Close();
                    return true;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error disconnecting from database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            else { return true; }
        }
        private bool UpdateDatabase()
        {
            try
            {
                //Check DB version
                int currentVersion = 5;
                int dbVersion = GetDatabaseVersion();
                if (dbVersion == 5)
                {
                    return true;
                }
                //Reset connection to prevent db table from being locked.
                ResetConnection();
                string command = "DROP TABLE IF EXISTS item_groups";
                Execute(command);
                CreateCurrentTables();
                while (dbVersion < currentVersion)
                {
                    if (dbVersion == 3) { FromVersion3(); }
                    if (dbVersion == 4) { FromVersion4(); }
                    ResetConnection();
                    dbVersion = GetDatabaseVersion();
                }
                if (dbVersion > currentVersion)
                {
                    MessageBox.Show($"This version of CalTools is outdated and uses database version {currentVersion}. The current database version is {dbVersion}");
                    return false;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "SQLite Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private void ResetConnection()
        {
            conn.Close();
            conn.Open();
        }
        //Data parsing---------------------------------------------------------------------------------------------------------------------
        private void AssignItemValues(ref CTItem item)
        {
            item.Location = reader.GetString((int)CTItem.DatabaseColumns.Location);
            item.Manufacturer = reader.GetString((int)CTItem.DatabaseColumns.Manufacturer);
            item.Directory = reader.GetString((int)CTItem.DatabaseColumns.Directory);
            item.Description = reader.GetString((int)CTItem.DatabaseColumns.Description);
            item.InService = reader.GetString((int)CTItem.DatabaseColumns.InService) == "1";
            if (reader.GetString((int)CTItem.DatabaseColumns.InServiceDate).Length > 0)
            { item.InServiceDate = DateTime.ParseExact(reader.GetString((int)CTItem.DatabaseColumns.InServiceDate), dateFormat, CultureInfo.InvariantCulture); }
            item.Model = reader.GetString((int)CTItem.DatabaseColumns.Model);
            item.Comment = reader.GetString((int)CTItem.DatabaseColumns.Comments);
            if (reader.GetString((int)CTItem.DatabaseColumns.Timestamp).Length > 0) 
            { item.TimeStamp = DateTime.ParseExact(reader.GetString((int)CTItem.DatabaseColumns.Timestamp), timestampFormat, CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString((int)CTItem.DatabaseColumns.ItemGroup);
            item.StandardEquipment = reader.GetString((int)CTItem.DatabaseColumns.StandardEquipment) == "1";
            item.CertificateNumber = reader.GetString((int)CTItem.DatabaseColumns.CertificateNumber);
            item.ChangesMade = false;
        }
        //Parse DB columns to CTTask Object
        private void AssignTaskValues(ref CTTask task)
        {
            task.TaskID = reader.GetInt32((int)CTTask.DatabaseColumns.TaskID);
            task.SerialNumber = reader.GetString((int)CTTask.DatabaseColumns.SerialNumber);
            task.TaskTitle = reader.GetString((int)CTTask.DatabaseColumns.TaskTitle);
            task.ServiceVendor = reader.GetString((int)CTTask.DatabaseColumns.ServiceVendor);
            task.Mandatory = reader.GetInt32((int)CTTask.DatabaseColumns.Mandatory) == 1;
            task.Interval = reader.GetInt32((int)CTTask.DatabaseColumns.Interval);
            if (reader.GetString((int)CTTask.DatabaseColumns.CompleteDate).Length > 0) 
            { task.CompleteDate = DateTime.ParseExact(reader.GetString((int)CTTask.DatabaseColumns.CompleteDate), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString((int)CTTask.DatabaseColumns.DueDate).Length > 0)
            { task.DueDate = DateTime.ParseExact(reader.GetString((int)CTTask.DatabaseColumns.DueDate), dateFormat, CultureInfo.InvariantCulture); }
            task.Due = reader.GetInt32((int)CTTask.DatabaseColumns.Due) == 1;
            task.ActionType = reader.GetString((int)CTTask.DatabaseColumns.ActionType);
            task.Comment = reader.GetString((int)CTTask.DatabaseColumns.Comments);
            task.ChangesMade = false;
        }
        //Parse DB columns to TaskData object
        private void AssignDataValues(ref TaskData data)
        {
            data.DataID = reader.GetInt32((int)TaskData.DatabaseColumns.ColDataID);
            data.TaskID = reader.GetInt32((int)TaskData.DatabaseColumns.ColTaskID);
            data.SerialNumber = reader.GetString((int)TaskData.DatabaseColumns.ColSerialNumber);
            data.StateBefore = JsonConvert.DeserializeObject<State>(reader.GetString((int)TaskData.DatabaseColumns.ColStateBeforeAction));
            data.StateAfter = JsonConvert.DeserializeObject<State>(reader.GetString((int)TaskData.DatabaseColumns.ColStateAfterAction));
            data.ActionTaken = JsonConvert.DeserializeObject<ActionTaken>(reader.GetString((int)TaskData.DatabaseColumns.ColActionTaken));
            if (reader.GetString((int)TaskData.DatabaseColumns.ColCompleteDate).Length > 0)
            { data.CompleteDate = DateTime.ParseExact(reader.GetString((int)TaskData.DatabaseColumns.ColCompleteDate), dateFormat, CultureInfo.InvariantCulture); }
            data.Procedure = reader.GetString((int)TaskData.DatabaseColumns.ColProcedure);
            data.StandardEquipment = reader.GetString((int)TaskData.DatabaseColumns.ColStandardEquipment);
            data.Findings = JsonConvert.DeserializeObject<Findings>(reader.GetString((int)TaskData.DatabaseColumns.ColFindings));
            data.Remarks = reader.GetString((int)TaskData.DatabaseColumns.ColRemarks);
            data.Technician = reader.GetString((int)TaskData.DatabaseColumns.ColTechnician);
            data.ChangesMade = false;
        }

        //Methods for forming and checking the current database structure. The methods assume an open connection.
        private int GetDatabaseVersion()
        {
            string command = "PRAGMA user_version";
            Execute(command);
            if (reader.Read())
            { return reader.GetInt32(0); }
            else
            { return 0; }
        }
        private void CreateCurrentTables()
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
                    "Comments TEXT DEFAULT ''," +
                    "FOREIGN KEY(SerialNumber) REFERENCES Items(SerialNumber))";
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
                    "FOREIGN KEY(TaskID) REFERENCES Tasks(TaskID))";
            Execute(command);
        }
        private void FromVersion3()
        {
            string command = "ALTER TABLE calibration_items ADD standard_equipment INTEGER DEFAULT 0";
            Execute(command);
            command = "ALTER TABLE calibration_items ADD certificate_number TEXT DEFAULT ''";
            Execute(command);
            command = "PRAGMA user_version = 4";
            Execute(command);
        }
        private void FromVersion4()
        {
            List<CalibrationItemV4> legacyItems = GetAllItemsLegacy();
            if (!IsConnected()) { conn.Open(); }
            foreach (CalibrationItemV4 calItem in legacyItems)
            {
                CTItem item = new CTItem(calItem.SerialNumber);
                item.Location = calItem.Location;
                item.Manufacturer = calItem.Manufacturer;
                item.Directory = calItem.Directory;
                item.Description = calItem.Description;
                item.InService = calItem.InService;
                item.InServiceDate = calItem.InServiceDate;
                item.TaskDue = calItem.CalDue;
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
            foreach (CalibrationDataV4 calData in GetAllCalDataLegacy())
            {
                TaskData taskData = new TaskData();
                taskData.TaskID = GetTasks("SerialNumber", calData.SerialNumber,false)[0].TaskID;
                taskData.SerialNumber = calData.SerialNumber;
                taskData.StateBefore = calData.StateBefore;
                taskData.StateAfter = calData.StateAfter;
                taskData.ActionTaken = calData.ActionTaken;
                taskData.CompleteDate = calData.CalibrationDate;
                taskData.Procedure = calData.Procedure;
                taskData.StandardEquipment = calData.Procedure;
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
    }
}
