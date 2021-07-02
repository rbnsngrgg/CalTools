using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;

namespace CalTools_WPF
{
    //Contains the CTDatabase methods related to checking and updating the structure and version of the SQLite database.
    internal partial class CTDatabase
    {
        #region V5 Methods
        private List<CTItem> AssignItemValuesV5(List<Dictionary<string, string>> v5rows)
        {
            List<CTItem> v5Items = new();
            foreach( Dictionary<string, string> row in v5rows)
            {
                CTItem item = new();
                item.SerialNumber = row["SerialNumber"];
                item.Location = row["Location"];
                item.Manufacturer = row["Manufacturer"];
                item.Directory = row["Directory"];
                item.Description = row["Description"];
                item.InService = row["InService"] == "1";
                item.Model = row["Model"];
                item.Remarks = row["Comment"];
                if (row["Timestamp"].Length > 0)
                { item.TimeStamp = DateTime.ParseExact(row["Timestamp"], timestampFormat, CultureInfo.InvariantCulture); }
                item.ItemGroup = row["ItemGroup"];
                item.IsStandardEquipment = row["StandardEquipment"] == "1";
                item.CertificateNumber = row["CertificateNumber"];
                item.ChangesMade = false;
                v5Items.Add(item);
            }
            return v5Items;
        }
        private List<TaskDataV5> AssignDataValuesV5(List<Dictionary<string, string>> v5rows)
        {
            List<TaskDataV5> v5Data = new();
            foreach (Dictionary<string, string> row in v5rows)
            {
                TaskDataV5 data = new();
                data.DataID = int.Parse(row["DataID"]);
                data.TaskID = int.Parse(row["TaskID"]);
                data.SerialNumber = row["SerialNumber"];
                data.StateBefore = JsonConvert.DeserializeObject<State>(row["StateBeforeAction"]);
                data.StateAfter = JsonConvert.DeserializeObject<State>(row["StateAfterAction"]);
                data.ActionTaken = JsonConvert.DeserializeObject<ActionTakenV5>(row["ActionTaken"]);
                if (row["CompleteDate"].Length > 0)
                { data.CompleteDate = DateTime.ParseExact(row["CompleteDate"], dateFormat, CultureInfo.InvariantCulture); }
                data.Procedure = row["Procedure"];
                data.StandardEquipment = row["StandardEquipment"];
                data.Findings = JsonConvert.DeserializeObject<FindingsV5>(row["Findings"]);
                if (data.Findings == null) { data.Findings = new(); }
                data.Remarks = row["Remarks"];
                data.Technician = row["Technician"];
                data.Timestamp = row["EntryTimestamp"];
                data.ChangesMade = false;
                v5Data.Add(data);
            }
            return v5Data;
        }
        private List<CTTask> AssignTaskValuesV5(List<Dictionary<string, string>> v5rows)
        {
            List<CTTask> v5Tasks = new();
            foreach (Dictionary<string, string> row in v5rows)
            {
                CTTask task = new();
                task.TaskId = int.Parse(row["TaskID"]);
                task.SerialNumber = row["SerialNumber"];
                task.TaskTitle = row["TaskTitle"];
                task.ServiceVendor = row["ServiceVendor"];
                task.IsMandatory = row["Mandatory"] == "1";
                task.Interval = int.Parse(row["Interval"]);
                if (row["CompleteDate"].Length > 0)
                { task.CompleteDate = DateTime.ParseExact(row["CompleteDate"], dateFormat, CultureInfo.InvariantCulture); }
                if (row["DueDate"].Length > 0)
                { task.DueDate = DateTime.ParseExact(row["DueDate"], dateFormat, CultureInfo.InvariantCulture); }
                task.IsDue = row["Due"] == "1";
                task.ActionType = row["ActionType"];
                task.TaskDirectory = row["Directory"];
                task.Remarks = row["Comments"];
                if (row["ManualFlag"].Length > 0)
                { task.DateOverride = DateTime.ParseExact(row["ManualFlag"], dateFormat, CultureInfo.InvariantCulture); }
                task.ChangesMade = false;
                v5Tasks.Add(task);
            }
            return v5Tasks;
        }
        public List<CTItem> GetAllItemsV5()
        {
            List<Dictionary<string,string>> v5rows = handler.SelectAllFromTable("old_items");
            List<CTItem> allItems = AssignItemValuesV5(v5rows);
            return allItems;
        }
        public List<CTTask> GetAllTasksV5()
        {
            List<Dictionary<string, string>> v5rows = handler.SelectAllFromTable("old_tasks");
            List<CTTask> allTasks = AssignTaskValuesV5(v5rows);
            return allTasks;
        }
        public List<TaskDataV5> GetAllTaskDataV5()
        {
            List<Dictionary<string, string>> v5rows = handler.SelectAllFromTable("old_data");
            List<TaskDataV5> data = AssignDataValuesV5(v5rows);
            return data;
        }
        #endregion
        private void CreateV6Tables()
        {
            handler.CreateTable("CREATE TABLE IF NOT EXISTS items (" +
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
                    "timestamp DATETIME DEFAULT '')");
            handler.CreateTable("CREATE TABLE IF NOT EXISTS tasks (" +
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
                    "FOREIGN KEY(serial_number) REFERENCES items(serial_number) ON DELETE CASCADE ON UPDATE CASCADE)");
            handler.CreateTable("CREATE TABLE IF NOT EXISTS standard_equipment (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "serial_number TEXT NOT NULL," +
                "manufacturer TEXT DEFAULT ''," +
                "model TEXT DEFAULT ''," +
                "description TEXT DEFAULT ''," +
                "remarks TEXT DEFAULT ''," +
                "item_group TEXT DEFAULT ''," +
                "certificate_number TEXT NOT NULL," +
                "action_due_date DATE," +
                "timestamp DATETIME)");
            handler.CreateTable("CREATE TABLE IF NOT EXISTS task_data (" +
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
                    "FOREIGN KEY(serial_number) REFERENCES items(serial_number) ON DELETE CASCADE)");
            handler.CreateTable("CREATE TABLE IF NOT EXISTS data_standard_equipment (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "task_data_id INTEGER," +
                "standard_equipment_id INTEGER," +
                "FOREIGN KEY(task_data_id) REFERENCES task_data(id) ON DELETE CASCADE," +
                "FOREIGN KEY(standard_equipment_id) REFERENCES standard_equipment(id))");
            handler.CreateTable("CREATE TABLE IF NOT EXISTS findings (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "task_data_id INTEGER," +
                "name TEXT," +
                "tolerance DOUBLE DEFAULT 0.0," +
                "tolerance_is_percent INTEGER DEFAULT 1," +
                "unit_of_measure TEXT DEFAULT ''," +
                "measurement_before DOUBLE DEFAULT 0.0," +
                "measurement_after DOUBLE DEFAULT 0.0," +
                "setting DOUBLE DEFAULT 0.0)");
            handler.CreateTable("CREATE TABLE IF NOT EXISTS task_data_files(" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "task_data_id INTEGER," +
                "description TEXT," +
                "location TEXT NOT NULL," +
                "FOREIGN KEY(task_data_id) REFERENCES task_data(id) ON DELETE CASCADE)");
        }
        private void FromVersion5()
        {
            handler.RenameTable("Items", "old_items");
            handler.RenameTable("Tasks", "old_tasks");
            handler.RenameTable("TaskData", "old_data");

            CreateV6Tables();
            List<CTItem> v5Items = GetAllItemsV5();
            List<CTTask> v5Tasks = GetAllTasksV5();
            List<TaskDataV5> v5TaskData = GetAllTaskDataV5();
            foreach(CTItem item in v5Items)
            {
                SaveItem(item, true);
            }
            foreach (CTTask task in v5Tasks)
            {
                SaveTask(task);
            }
            foreach(TaskDataV5 data in v5TaskData)
            {
                SaveTaskData(TaskDataV5toV6(data));
            }
            
            handler.DropTable("old_items");
            handler.DropTable("old_tasks");
            handler.DropTable("old_data");
            handler.SetVersion("7");
        }
        public TaskData TaskDataV5toV6(TaskDataV5 v5)
        {
            List<Findings> parameters = new();
            if(v5.Findings != null)
            {
                foreach (Param param in v5.Findings.parameters)
                {
                    parameters.Add(new Findings()
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
            List<TaskDataFile> dataFiles = new();
            foreach(string file in v5.Findings.files)
            {
                dataFiles.Add(new TaskDataFile() { Location = file});
            }
            if(!DateTime.TryParseExact(v5.Timestamp, timestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            { v5.Timestamp = DateTime.MinValue.ToString(timestampFormat); }
            return new TaskData()
            {
                DataId = (int)v5.DataID,
                TaskId = v5.TaskID,
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
                Timestamp = DateTime.ParseExact(v5.Timestamp, timestampFormat, CultureInfo.InvariantCulture),
                DataFiles = dataFiles,
            };
        }
        private bool UpdateDatabase()
        {
            try
            {
                //Check DB version
                int dbVersion = handler.GetDatabaseVersion();
                if (dbVersion == currentVersion)
                {
                    return true;
                }
                while (dbVersion < currentVersion)
                {
                    if (dbVersion < 6)
                    {
                        MessageBox.Show($"The database version is not compatible with this version of CalTools.",
                            "Database Version Outdated", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    else if(dbVersion == 6) { FromVersion5(); }
                    dbVersion = handler.GetDatabaseVersion();
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CTDatabaseUpdates.UpdateDatabase", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
