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
        #region V4 Methods
        private void AssignItemValuesV4(ref CalibrationItemV4 item)
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
        private void AssignDataValuesV4(ref CalibrationDataV4 data)
        {
            data.ID = reader.GetInt32((int)CalibrationDataV4.DatabaseColumns.ColID);
            data.SerialNumber = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColSerialNumber);
            data.StateBefore = JsonConvert.DeserializeObject<State>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColStateBeforeAction));
            data.StateAfter = JsonConvert.DeserializeObject<State>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColStateAfterAction));
            data.ActionTaken = JsonConvert.DeserializeObject<ActionTakenV5>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColActionTaken));
            if (reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColCalibrationDate).Length > 0)
            { data.CalibrationDate = DateTime.ParseExact(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColCalibrationDate), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColDueDate).Length > 0)
            { data.DueDate = DateTime.ParseExact(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColDueDate), dateFormat, CultureInfo.InvariantCulture); }
            data.Procedure = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColProcedure);
            data.StandardEquipment = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColStandardEquipment);
            data.findings = JsonConvert.DeserializeObject<FindingsV5>(reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColFindings));
            data.Remarks = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColRemarks);
            data.Technician = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColTechnician);
            data.Timestamp = reader.GetString((int)CalibrationDataV4.DatabaseColumns.ColEntryTimestamp);
        }
        #endregion
        #region V5 Methods
        private void AssignItemValuesV5(ref CTItem item)
        {
            item.Location = reader.GetString((int)ItemsColumnsV5.Location);
            item.Manufacturer = reader.GetString((int)ItemsColumnsV5.Manufacturer);
            item.Directory = reader.GetString((int)ItemsColumnsV5.Directory);
            item.Description = reader.GetString((int)ItemsColumnsV5.Description);
            item.InService = reader.GetString((int)ItemsColumnsV5.InService) == "1";
            item.Model = reader.GetString((int)ItemsColumnsV5.Model);
            item.Remarks = reader.GetString((int)ItemsColumnsV5.Comments);
            if (reader.GetString((int)ItemsColumnsV5.Timestamp).Length > 0)
            { item.TimeStamp = DateTime.ParseExact(reader.GetString((int)ItemsColumnsV5.Timestamp), timestampFormat, CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString((int)ItemsColumnsV5.ItemGroup);
            item.StandardEquipment = reader.GetString((int)ItemsColumnsV5.StandardEquipment) == "1";
            item.CertificateNumber = reader.GetString((int)ItemsColumnsV5.CertificateNumber);
            item.ChangesMade = false;
        }
        private void AssignDataValuesV5(ref TaskDataV5 data)
        {
            data.DataID = reader.GetInt32((int)TaskDataColumnsV5.ColDataID);
            data.TaskID = reader.GetInt32((int)TaskDataColumnsV5.ColTaskID);
            data.SerialNumber = reader.GetString((int)TaskDataColumnsV5.ColSerialNumber);
            data.StateBefore = JsonConvert.DeserializeObject<State>(reader.GetString((int)TaskDataColumnsV5.ColStateBeforeAction));
            data.StateAfter = JsonConvert.DeserializeObject<State>(reader.GetString((int)TaskDataColumnsV5.ColStateAfterAction));
            data.ActionTaken = JsonConvert.DeserializeObject<ActionTakenV5>(reader.GetString((int)TaskDataColumnsV5.ColActionTaken));
            if (reader.GetString((int)TaskDataColumnsV5.ColCompleteDate).Length > 0)
            { data.CompleteDate = DateTime.ParseExact(reader.GetString((int)TaskDataColumnsV5.ColCompleteDate), dateFormat, CultureInfo.InvariantCulture); }
            data.Procedure = reader.GetString((int)TaskDataColumnsV5.ColProcedure);
            data.StandardEquipment = reader.GetString((int)TaskDataColumnsV5.ColStandardEquipment);
            data.Findings = JsonConvert.DeserializeObject<FindingsV5>(reader.GetString((int)TaskDataColumnsV5.ColFindings));
            if(data.Findings == null) { data.Findings = new(); }
            data.Remarks = reader.GetString((int)TaskDataColumnsV5.ColRemarks);
            data.Technician = reader.GetString((int)TaskDataColumnsV5.ColTechnician);
            data.Timestamp = reader.GetString((int)TaskDataColumnsV5.ColEntryTimestamp);
            data.ChangesMade = false;
        }
        private void AssignTaskValuesV5(ref CTTask task)
        {
            task.TaskID = reader.GetInt32((int)TasksColumnsV5.TaskID);
            task.SerialNumber = reader.GetString((int)TasksColumnsV5.SerialNumber);
            task.TaskTitle = reader.GetString((int)TasksColumnsV5.TaskTitle);
            task.ServiceVendor = reader.GetString((int)TasksColumnsV5.ServiceVendor);
            task.Mandatory = reader.GetInt32((int)TasksColumnsV5.Mandatory) == 1;
            task.Interval = reader.GetInt32((int)TasksColumnsV5.Interval);
            if (reader.GetString((int)TasksColumnsV5.CompleteDate).Length > 0)
            { task.CompleteDate = DateTime.ParseExact(reader.GetString((int)TasksColumnsV5.CompleteDate), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString((int)TasksColumnsV5.DueDate).Length > 0)
            { task.DueDate = DateTime.ParseExact(reader.GetString((int)TasksColumnsV5.DueDate), dateFormat, CultureInfo.InvariantCulture); }
            task.Due = reader.GetInt32((int)TasksColumnsV5.Due) == 1;
            task.ActionType = reader.GetString((int)TasksColumnsV5.ActionType);
            task.TaskDirectory = reader.GetString((int)TasksColumnsV5.Directory);
            task.Comment = reader.GetString((int)TasksColumnsV5.Comments);
            if (reader.GetString((int)TasksColumnsV5.ManualFlag).Length > 0)
            { task.DateOverride = DateTime.ParseExact(reader.GetString((int)TasksColumnsV5.ManualFlag), dateFormat, CultureInfo.InvariantCulture); }

            task.ChangesMade = false;
        }
        public List<CTItem> GetAllItemsV5()
        {
            List<CTItem> allItems = new();
            if (!Connect()) { return allItems; }
            Execute("SELECT * FROM old_items");
            while (reader.Read())
            {
                CTItem item = new(reader.GetString(0));
                AssignItemValuesV5(ref item);
                allItems.Add(item);
            }
            Disconnect();
            return allItems;
        }
        public List<CTTask> GetAllTasksV5()
        {
            List<CTTask> allTasks = new();
            if (!Connect()) { return allTasks; }
            Execute("SELECT * FROM old_tasks");
            while (reader.Read())
            {
                CTTask task = new();
                AssignTaskValuesV5(ref task);
                allTasks.Add(task);
            }
            Disconnect();
            return allTasks;
        }
        public List<TaskDataV5> GetAllTaskDataV5()
        {
            List<TaskDataV5> calData = new();
            if (Connect())
            {
                Execute($"SELECT * FROM old_data");
                while (reader.Read())
                {
                    TaskDataV5 data = new();
                    AssignDataValuesV5(ref data);
                    calData.Add(data);
                }
                Disconnect();
            }
            return calData;
        }
        public List<TaskDataV5> GetTaskDataV5(string taskID)
        {
            List<TaskDataV5> calData = new();
            if (Connect())
            {
                Execute($"SELECT * FROM old_data WHERE TaskID='{taskID}'");
                while (reader.Read())
                {
                    TaskDataV5 data = new();
                    AssignDataValuesV5(ref data);
                    calData.Add(data);
                }
                Disconnect();
            }
            return calData;
        }
        public bool SaveTaskDataV5(TaskDataV5 data, bool timestampOverride = false, bool disconnect = false)
        {
            if (data.TaskID == null)
            {
                MessageBox.Show($"Task data TaskID is null.", "Null TaskID", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            try
            {
                if (Connect())
                {
                    string command = $"INSERT INTO TaskData (TaskID,SerialNumber,StateBeforeAction,StateAfterAction,ActionTaken,CompleteDate,Procedure,StandardEquipment," +
                        $"Findings,Remarks,Technician,EntryTimeStamp) " +
                        $"VALUES ('{data.TaskID}'," +
                        $"'{data.SerialNumber.Replace("'", "''")}'," +
                        $"'{JsonConvert.SerializeObject(data.StateBefore)}'," +
                        $"'{JsonConvert.SerializeObject(data.StateAfter)}'," +
                        $"'{JsonConvert.SerializeObject(data.ActionTaken)}'," +
                        $"'{data.CompleteDate.Value.ToString(dateFormat)}'," +
                        $"'{data.Procedure.Replace("'", "''")}'," +
                        $"'{data.StandardEquipment.Replace("'", "''")}'," +
                        $"'{JsonConvert.SerializeObject(data.Findings).Replace("'", "''")}'," +
                        $"'{data.Remarks.Replace("'", "''")}'," +
                        $"'{data.Technician.Replace("'", "''")}'," +
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
        #endregion
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
                                CTItem newItem = new(Path.GetFileName(itemFolder));
                                newItem.Directory = itemFolder;
                                SaveItem(newItem);
                                CTTask newTask = new();
                                newTask.SerialNumber = newItem.SerialNumber;
                                SaveTask(newTask);
                                MoveToTaskFolder(newItem, GetTasks("SerialNumber", newItem.SerialNumber)[0]);
                            }
                        }
                    }
                }
            }
        }
        private void CreateV6Tables()
        {
            if (!Connect()) { return; }
            string command = "CREATE TABLE IF NOT EXISTS items (" +
                    "serial_number TEXT PRIMARY KEY," +
                    "location TEXT DEFAULT ''," +
                    "manufacturer TEXT DEFAULT ''," +
                    "directory TEXT DEFAULT ''," +
                    "description TEXT DEFAULT ''," +
                    "in_service INTEGER DEFAULT 1," +
                    "model TEXT DEFAULT ''," +
                    "item_group TEXT DEFAULT ''," +
                    "remarks TEXT DEFAULT ''," +
                    "is_standard_equipment INTEGER DEFAULT 0," +
                    "certificate_number TEXT DEFAULT ''," +
                    "timestamp DATETIME DEFAULT '')";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS tasks (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "serial_number TEXT," +
                    "task_title TEXT DEFAULT ''," +
                    "service_vendor TEXT DEFAULT ''," +
                    "is_mandatory INTEGER DEFAULT 1," +
                    "interval INTEGER DEFAULT 12," +
                    "complete_date DATE DEFAULT ''," +
                    "due_date DATE DEFAULT ''," +
                    "is_due INTEGER DEFAULT 0," +
                    "action_type TEXT DEFAULT 'CALIBRATION'," +
                    "directory TEXT DEFAULT ''," +
                    "remarks TEXT DEFAULT ''," +
                    "date_override DATE DEFAULT ''," +
                    "FOREIGN KEY(serial_number) REFERENCES items(serial_number) ON DELETE CASCADE ON UPDATE CASCADE)";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS standard_equipment (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "serial_number TEXT," +
                "manufacturer TEXT DEFAULT ''," +
                "model TEXT DEFAULT ''," +
                "description TEXT DEFAULT ''," +
                "remarks TEXT DEFAULT ''," +
                "item_group TEXT DEFAULT ''," +
                "certificate_number TEXT," +
                "action_due_date DATE," +
                "timestamp DATETIME)";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS task_data (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "task_id INTEGER," +
                    "serial_number TEXT," +
                    "in_tolerance_before INTEGER," +
                    "operational_before INTEGER," +
                    "in_tolerance_after INTEGER," +
                    "operational_after INTEGER," +
                    "calibrated INTEGER," +
                    "verified INTEGER," +
                    "adjusted INTEGER," +
                    "repaired INTEGER," +
                    "maintenance INTEGER," +
                    "complete_date DATE DEFAULT ''," +
                    "procedure TEXT DEFAULT ''," +
                    "remarks TEXT DEFAULT ''," +
                    "technician TEXT," +
                    "timestamp DATE DEFAULT ''," +
                    "FOREIGN KEY(task_id) REFERENCES tasks(id) ON DELETE CASCADE," +
                    "FOREIGN KEY(serial_number) REFERENCES items(serial_number) ON DELETE CASCADE)";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS data_standard_equipment (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "data_id INTEGER," +
                "standard_equipment_id INTEGER," +
                "FOREIGN KEY(data_id) REFERENCES task_data(id) ON DELETE CASCADE," +
                "FOREIGN KEY(standard_equipment_id) REFERENCES standard_equipment(id))";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS findings (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "data_id INTEGER," +
                "name TEXT," +
                "tolerance DOUBLE DEFAULT 0.0," +
                "tolerance_is_percent INTEGER DEFAULT 1," +
                "unit_of_measure TEXT DEFAULT ''," +
                "measurement_before DOUBLE DEFAULT 0.0," +
                "measurement_after DOUBLE DEFAULT 0.0," +
                "setting DOUBLE DEFAULT 0.0)";
            Execute(command);
            command = "CREATE TABLE IF NOT EXISTS task_data_files(" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "task_data_id INTEGER," +
                "description TEXT," +
                "location TEXT NOT NULL," +
                "FOREIGN KEY(task_data_id) REFERENCES task_data(id) ON DELETE CASCADE)";
            Execute(command);
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
                CTItem item = new(calItem.SerialNumber);
                item.Location = calItem.Location;
                item.Manufacturer = calItem.Manufacturer;
                item.Directory = calItem.Directory;
                item.Description = calItem.Description;
                item.InService = calItem.InService;
                item.InServiceDate = calItem.InServiceDate;
                item.Model = calItem.Model;
                item.Remarks = calItem.Comment;
                item.ItemGroup = calItem.ItemGroup;
                item.StandardEquipment = calItem.StandardEquipment;
                item.CertificateNumber = calItem.CertificateNumber;

                CTTask task = new();
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
                TaskDataV5 taskData = new();
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

                SaveTaskDataV5(taskData, true);
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
        private void FromVersion5()
        {
            string command = "ALTER TABLE Items RENAME TO old_items";
            Execute(command);
            command = "ALTER TABLE Tasks RENAME TO old_tasks";
            Execute(command);
            command = "ALTER TABLE TaskData RENAME TO old_data";
            Execute(command);
            ResetConnection();
            CreateV6Tables();
            List<CTItem> v5Items = GetAllItemsV5();
            List<CTTask> v5Tasks = GetAllTasksV5();
            List<TaskDataV5> v5TaskData = GetAllTaskDataV5();
            if (!IsConnected()) { conn.Open(); }
            foreach(CTItem item in v5Items)
            {
                SaveItem(item);
            }
            foreach (CTTask task in v5Tasks)
            {
                SaveTask(task, true, true);
            }
            foreach(TaskDataV5 data in v5TaskData)
            {
                SaveTaskData(TaskDataV5toV6(data), true, true, true);
            }
            
            ResetConnection();
            command = "DROP TABLE IF EXISTS old_items";
            Execute(command);
            ResetConnection();
            command = "DROP TABLE IF EXISTS old_tasks";
            Execute(command);
            ResetConnection();
            command = "DROP TABLE IF EXISTS old_data";
            Execute(command);
            command = "PRAGMA user_version = 7";
            Execute(command);
            ResetConnection();
        }
        public TaskData TaskDataV5toV6(TaskDataV5 v5)
        {
            List<Parameter> parameters = new();
            if(v5.Findings != null)
            {
                foreach (Param param in v5.Findings.parameters)
                {
                    parameters.Add(new Parameter()
                    {
                        DataId = (int)v5.DataID,
                        Name = param.Name,
                        Tolerance = param.Tolerance,
                        ToleranceIsPercent = param.ToleranceIsPercent,
                        UnitOfMeasure = param.UnitOfMeasure,
                        MeasurementBefore = param.MeasurementBefore,
                        MeasurementAfter = param.MeasurementAfter,
                        Setting = param.Setting
                    });
                }
            }
            List<CTStandardEquipment> standardEquipment = new();
            if(v5.StandardEquipment != null && v5.StandardEquipment != "null")
            {
                standardEquipment.Add(JsonConvert.DeserializeObject<CTItem>(v5.StandardEquipment).ToStandardEquipment(DateTime.MaxValue));
            }
            List<Tuple<string, string>> dataFiles = new();
            foreach(string file in v5.Findings.files)
            {
                dataFiles.Add(new Tuple<string, string>("", file));
            }
            return new TaskData()
            {
                DataID = (int)v5.DataID,
                TaskID = v5.TaskID,
                SerialNumber = v5.SerialNumber,
                StateBefore = new State
                {
                    InTolerance = v5.StateBefore.Value.InTolerance,
                    Operational = v5.StateBefore.Value.Operational
                },
                StateAfter = new State
                {
                    InTolerance = v5.StateAfter.Value.InTolerance,
                    Operational = v5.StateAfter.Value.Operational
                },
                Actions = new ActionTaken
                {
                    Calibration = v5.ActionTaken.Value.Calibration,
                    Verification = v5.ActionTaken.Value.Verification,
                    Adjusted = v5.ActionTaken.Value.Adjusted,
                    Repaired = v5.ActionTaken.Value.Repaired,
                    Maintenance = v5.ActionTaken.Value.Maintenance
                },
                CompleteDate = v5.CompleteDate,
                Procedure = v5.Procedure,
                Findings = parameters,
                StandardEquipment = standardEquipment,
                Remarks = v5.Remarks,
                Technician = v5.Technician,
                Timestamp = v5.Timestamp,
                DataFiles = dataFiles,
            };
        }
        public List<CalibrationDataV4> GetAllCalDataLegacy()
        {
            List<CalibrationDataV4> calData = new();
            if (Connect())
            {
                string command = $"SELECT * FROM calibration_data";
                Execute(command);
                while (reader.Read())
                {
                    CalibrationDataV4 data = new();
                    AssignDataValuesV4(ref data);
                    calData.Add(data);
                }
            }
            return calData;
        }
        public List<CalibrationItemV4> GetAllItemsLegacy()
        {
            List<CalibrationItemV4> allItems = new();
            string command = "SELECT * FROM calibration_items";
            if (!Connect()) { return allItems; }
            Execute(command);
            while (reader.Read())
            {
                CalibrationItemV4 item = new(reader.GetString(0));
                AssignItemValuesV4(ref item);
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
                    else if(dbVersion == 6) { FromVersion5(); ResetConnection(); }
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
