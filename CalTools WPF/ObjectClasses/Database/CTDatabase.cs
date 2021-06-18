using CalTools_WPF.ObjectClasses;
using CalTools_WPF.ObjectClasses.Database;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;

namespace CalTools_WPF
{
    partial class CTDatabase
    {
        public readonly int currentVersion = 7;
        public readonly string dateFormat = "yyyy-MM-dd";
        public readonly string timestampFormat = "yyyy-MM-dd-HH-mm-ss-ffffff";
        public bool tablesExist = false;
        public string ItemScansDir { get; set; }
        public List<string> Folders { get; set; }
        private readonly SqliteConnectionHandler handler;
        private readonly SqliteConnection conn;
        private SqliteDataReader reader;
        public string DbPath { get; set; }
        public CTDatabase(string dbPath)
        {
            this.DbPath = dbPath;
            conn = new SqliteConnection($"Data Source={DbPath}");
            handler = new(DbPath);
        }

        //Basic Operations-----------------------------------------------------------------------------------------------------------------
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
        public bool DatabaseReady() //Check for successful connect and disconnect
        {
            return (Connect() & Disconnect());
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
            return true;
        }
        private void Execute(string com)
        {
            SqliteCommand command = new(com, conn);
            reader = command.ExecuteReader();
        }
        public bool IsConnected()
        {
            return conn.State == System.Data.ConnectionState.Open;
        }

        //Data Retrieval-------------------------------------------------------------------------------------------------------------------
        public List<CTItem> GetAllItems()
        {
            List<CTItem> allItems = AssignItemValues(
                handler.SelectAllFromTable("items"));
            return allItems;
        }
        public List<CTTask> GetAllTasks()
        {
            List<CTTask> allTasks = AssignTaskValues(
                handler.SelectAllFromTable("tasks"));
            return allTasks;
        }
        public List<TaskData> GetAllTaskData()
        {
            List<TaskData> calData = new();
            if (Connect())
            {
                Execute($"SELECT * FROM task_data");
                while (reader.Read())
                {
                    TaskData data = new();
                    AssignDataValues(ref data);
                    calData.Add(data);
                }
                Disconnect();
                foreach (TaskData data in calData)
                {
                    data.Findings = GetFindingsFromTaskData(data.DataID);
                    data.StandardEquipment = GetDataStandardEquipment(data.DataID, true);
                    data.DataFiles = GetDataFiles(data.DataID);
                }
            }
            return calData;
        }
        public List<TaskData> GetTaskData(string taskID)
        {
            List<TaskData> calData = new();
            if (Connect())
            {
                Execute($"SELECT * FROM task_data WHERE task_id='{taskID}'");
                while (reader.Read())
                {
                    TaskData data = new();
                    AssignDataValues(ref data);
                    calData.Add(data);
                }
                Disconnect();
                //TODO: break into separate method
                foreach (TaskData data in calData)
                {
                    data.Findings = GetFindingsFromTaskData(data.DataID);
                    data.StandardEquipment = GetDataStandardEquipment(data.DataID, true);
                    data.DataFiles = GetDataFiles(data.DataID);
                }
            }
            return calData;
        }
#nullable enable
        public CTItem? GetItem(string col, string value)
        {
            List<CTItem> items = AssignItemValues(
                handler.SelectFromTableWhere(
                "items",
                new string[] { col },
                new string[] { value }
            ));
            return items.Count > 0 ? items[0] : null;
        }
        public CTItem? GetItemFromTask(CTTask task)
        {
            List<CTItem> items = AssignItemValues(
                handler.SelectFromTableWhere(
                "items",
                new string[] { "serial_number" },
                new string[] { task.SerialNumber }
            ));
            return items.Count > 0 ? items[0] : null;
        }
        public List<CTTask> GetTasks(string col, string value, bool disconnect = true)
        {
            List<CTTask> tasks = AssignTaskValues(
                handler.SelectFromTableWhere(
                "tasks",
                new string[] { col },
                new string[] { value }
            ));
            return tasks;
        }
        public List<Parameter> GetFindingsFromTaskData(int taskDataId, bool disconnect = true)
        {
            List<Parameter> parameters = new();
            if (Connect())
            {
                Execute($"SELECT * FROM findings WHERE data_id='{taskDataId}'");
                while (reader.Read())
                {
                    Parameter param = new();
                    AssignFindingsValues(ref param);
                    parameters.Add(param);
                }
                if (disconnect) { Disconnect(); }
            }
            return parameters;
        }
        public List<CTStandardEquipment> GetDataStandardEquipment(int taskDataId, bool disconnect = true)
        {
            List<CTStandardEquipment> equipment = new();
            if (Connect())
            {
                Execute($"SELECT * FROM standard_equipment " +
                    $"WHERE standard_equipment.id IN " +
                    $"(SELECT standard_equipment_id FROM data_standard_equipment WHERE data_id = '{taskDataId}')");
                while (reader.Read())
                {
                    CTStandardEquipment item = new(
                        reader.GetString((int)StandardEquipmentColumns.serial_number),
                        reader.GetInt32((int)StandardEquipmentColumns.id));
                    AssignStandardEquipmentValues(ref item);
                    equipment.Add(item);
                }
                if (disconnect) { Disconnect(); }
            }
            return equipment;
        }
        public List<TaskDataFile> GetDataFiles(int taskDataId, bool disconnect = true)
        {
            List<TaskDataFile> files = new();
            if (Connect())
            {
                Execute($"SELECT * FROM task_data_files " +
                    $"WHERE task_data_id = {taskDataId}");
                while (reader.Read())
                {
                    files.Add(new TaskDataFile()
                    {
                        Description = reader.GetString((int)TaskDataFiles.description),
                        Path = reader.GetString((int)TaskDataFiles.location)
                    });
                }
                if (disconnect) { Disconnect(); }
            }
            return files;
        }
        public List<CTStandardEquipment> GetAllStandardEquipment()
        {
            List<CTStandardEquipment> equipment = new();
            if (Connect())
            {
                Execute("SELECT * FROM (SELECT * FROM standard_equipment ORDER BY id DESC) " +
                    "WHERE action_due_date > date('now') GROUP BY serial_number");
                while (reader.Read())
                {
                    CTStandardEquipment item = new(
                        reader.GetString((int)StandardEquipmentColumns.serial_number),
                        reader.GetInt32((int)StandardEquipmentColumns.id));
                    AssignStandardEquipmentValues(ref item);
                    equipment.Add(item);
                }
                Disconnect();
            }
            return equipment;
        }
#nullable disable

        //Save data------------------------------------------------------------------------------------------------------------------------
        public bool CreateItem(string sn)
        {
            try
            {
                if (Connect())
                {
                    Execute($"INSERT OR IGNORE INTO items (serial_number) VALUES ('{sn}')");
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
                    string command = $"INSERT OR IGNORE INTO items (serial_number) VALUES ('{item.SerialNumber.Replace("'", "''")}')";
                    Execute(command);
                    command = $"UPDATE Items SET serial_number='{item.SerialNumber.Replace("'", "''")}'," +
                        $"model='{item.Model.Replace("'", "''")}'," +
                        $"description='{item.Description.Replace("'", "''")}'," +
                        $"location='{item.Location.Replace("'", "''")}'," +
                        $"manufacturer='{item.Manufacturer.Replace("'", "''")}'," +
                        $"directory='{item.Directory.Replace("'", "''")}'," +
                        $"in_service='{(item.InService == true ? 1 : 0)}'," +
                        $"remarks='{item.Remarks.Replace("'", "''")}'," +
                        $"timestamp='{DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture)}'," +
                        $"item_group='{item.ItemGroup.Replace("'", "''")}'," +
                        $"certificate_number='{item.CertificateNumber.Replace("'", "''")}'," +
                        $"is_standard_equipment='{(item.IsStandardEquipment == true ? 1 : 0)}' " +
                        $"WHERE serial_number='{item.SerialNumber.Replace("'", "''")}'";
                    Execute(command);
                    if (disconnect) { Disconnect(); }
                    return true;
                }
                    return false;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool SaveDataStandardEquipment(int dataId, int equipmentId)
        {
            Execute($"SELECT * FROM data_standard_equipment WHERE " +
                $"data_id='{dataId}' AND " +
                $"standard_equipment_id='{equipmentId}'");
            if (reader.Read()) { return false; }
            Execute("INSERT INTO data_standard_equipment (data_id, standard_equipment_id) " +
                $"VALUES ({dataId},{equipmentId})");
            return true;
        }
        public bool SaveStandardEquipment(CTStandardEquipment item, bool disconnect = false)
        {
            try
            {
                if (Connect())
                {
                    string command = $"INSERT INTO standard_equipment (serial_number,model,description," +
                        $"manufacturer,remarks,timestamp,item_group,certificate_number,action_due_date) " +
                        $"VALUES ('{item.SerialNumber.Replace("'", "''")}'," +
                        $"'{item.Model.Replace("'", "''")}'," +
                        $"'{item.Description.Replace("'", "''")}'," +
                        $"'{item.Manufacturer.Replace("'", "''")}'," +
                        $"'{item.Remarks.Replace("'", "''")}'," +
                        $"'{DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture)}'," +
                        $"'{item.ItemGroup.Replace("'", "''")}'," +
                        $"'{item.CertificateNumber.Replace("'", "''")}'," +
                        $"'{item.ActionDueDate.ToString(dateFormat)}')";
                    Execute(command);
                    if (disconnect) { Disconnect(); }
                    return true;
                }
                    return false;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool SaveTask(CTTask task, bool disconnect = true, bool overrideId = false)
        {
            string command;
            try
            {
                if (Connect())
                {
                    //New task that hasn't yet been inserted into the database
                    if (overrideId && task.TaskId != -1)
                    {
                        command = $"INSERT OR IGNORE INTO tasks " +
                            $"(id,serial_number,task_title,service_vendor,is_mandatory," +
                            $"interval,complete_date,due_date,is_due,action_type,directory,remarks,date_override) " +
                            $"VALUES ('{task.TaskId}'," +
                            $"'{task.SerialNumber}'," +
                            $"'{task.TaskTitle}'," +
                            $"'{task.ServiceVendor}'," +
                            $"'{(task.IsMandatory ? 1 : 0)}'," +
                            $"'{task.Interval}'," +
                            $"'{task.CompleteDateString}'," +
                            $"'{task.DueDateString}'," +
                            $"'{(task.IsDue ? 1 : 0)}'," +
                            $"'{task.ActionType}'," +
                            $"'{task.TaskDirectory}'," +
                            $"'{task.Remarks}'," +
                            $"'{task.DateOverrideString}')";
                    }
                    else if (task.TaskId == -1 )
                    {
                        command = $"INSERT OR IGNORE INTO tasks " +
                            $"(serial_number,task_title,service_vendor,is_mandatory," +
                            $"interval,complete_date,due_date,is_due,action_type,directory,remarks,date_override) " +
                            $"VALUES ('{task.SerialNumber}'," +
                            $"'{task.TaskTitle}'," +
                            $"'{task.ServiceVendor}'," +
                            $"'{(task.IsMandatory ? 1 : 0)}'," +
                            $"'{task.Interval}'," +
                            $"'{task.CompleteDateString}'," +
                            $"'{task.DueDateString}'," +
                            $"'{(task.IsDue ? 1 : 0)}'," +
                            $"'{task.ActionType}'," +
                            $"'{task.TaskDirectory}'," +
                            $"'{task.Remarks}'," +
                            $"'{task.DateOverrideString}')";
                    }
                    //Existing task
                    else
                    {
                        command = $"UPDATE tasks SET " +
                            $"serial_number='{task.SerialNumber}'," +
                            $"task_title='{task.TaskTitle}'," +
                            $"service_vendor='{task.ServiceVendor}'," +
                            $"is_mandatory='{(task.IsMandatory ? 1 : 0)}'," +
                            $"interval='{task.Interval}'," +
                            $"complete_date='{task.CompleteDateString}'," +
                            $"due_date='{task.DueDateString}'," +
                            $"is_due='{(task.IsDue ? 1 : 0)}'," +
                            $"action_type='{task.ActionType}'," +
                            $"directory='{task.TaskDirectory}'," +
                            $"remarks='{task.Remarks}'," +
                            $"date_override='{task.DateOverrideString}' " +
                            $"WHERE id='{task.TaskId}'";
                    }
                    Execute(command);
                    if (disconnect) { Disconnect(); }
                    return true;
                }
                else { return false; }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool SaveTaskData(TaskData data, bool timestampOverride = false, bool disconnect = false, bool overrideId = false)
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
                    string command = $"INSERT INTO task_data (task_id,serial_number,in_tolerance_before,operational_before," +
                        $"in_tolerance_after,operational_after,calibrated,verified,adjusted,repaired," +
                        $"maintenance,complete_date,procedure,remarks,technician,timestamp) " +
                        $"VALUES ('{data.TaskID}'," +
                        $"'{data.SerialNumber.Replace("'", "''")}'," +
                        $"'{(data.StateBefore.Value.InTolerance ? 1 : 0)}'," +
                        $"'{(data.StateBefore.Value.Operational ? 1 : 0)}'," +
                        $"'{(data.StateAfter.Value.InTolerance ? 1 : 0)}'," +
                        $"'{(data.StateAfter.Value.Operational ? 1 : 0)}'," +
                        $"'{(data.Actions.Value.Calibration ? 1 : 0)}'," +
                        $"'{(data.Actions.Value.Verification ? 1 : 0)}'," +
                        $"'{(data.Actions.Value.Adjusted ? 1 : 0)}'," +
                        $"'{(data.Actions.Value.Repaired ? 1 : 0)}'," +
                        $"'{(data.Actions.Value.Maintenance ? 1 : 0)}'," +
                        $"'{data.CompleteDateString}'," +
                        $"'{data.Procedure.Replace("'", "''")}'," +
                        $"'{data.Remarks.Replace("'", "''")}'," +
                        $"'{data.Technician.Replace("'", "''")}'," +
                        $"'{(timestampOverride ? data.Timestamp : DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture))}')";
                    if (overrideId && data.DataID != -1)
                    {
                        command = $"INSERT INTO task_data (id,task_id,serial_number,in_tolerance_before,operational_before," +
                            $"in_tolerance_after,operational_after,calibrated,verified,adjusted,repaired," +
                            $"maintenance,complete_date,procedure,remarks,technician,timestamp) " +
                            $"VALUES ('{data.DataID}'," +
                            $"'{data.TaskID}'," +
                            $"'{data.SerialNumber.Replace("'", "''")}'," +
                            $"'{(data.StateBefore.Value.InTolerance ? 1 : 0)}'," +
                            $"'{(data.StateBefore.Value.Operational ? 1 : 0)}'," +
                            $"'{(data.StateAfter.Value.InTolerance ? 1 : 0)}'," +
                            $"'{(data.StateAfter.Value.Operational ? 1 : 0)}'," +
                            $"'{(data.Actions.Value.Calibration ? 1 : 0)}'," +
                            $"'{(data.Actions.Value.Verification ? 1 : 0)}'," +
                            $"'{(data.Actions.Value.Adjusted ? 1 : 0)}'," +
                            $"'{(data.Actions.Value.Repaired ? 1 : 0)}'," +
                            $"'{(data.Actions.Value.Maintenance ? 1 : 0)}'," +
                            $"'{data.CompleteDateString}'," +
                            $"'{data.Procedure.Replace("'", "''")}'," +
                            $"'{data.Remarks.Replace("'", "''")}'," +
                            $"'{data.Technician.Replace("'", "''")}'," +
                            $"'{(timestampOverride ? data.Timestamp : DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture))}')";
                    }
                    Execute(command);
                    Execute("SELECT last_insert_rowid()");
                    reader.Read();
                    int dataId = reader.GetInt32(0);
                    data.DataID = dataId;
                    foreach(Parameter p in data.Findings)
                    {
                        p.DataId = dataId;
                        SaveParameter(p, false);
                    }
                    foreach(CTStandardEquipment e in data.StandardEquipment)
                    {
                        int equipmentId = CheckStandardEquipment(e);
                        SaveDataStandardEquipment(dataId, equipmentId);
                    }
                    SaveTaskDataFiles(data);
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
        public void SaveTaskDataFiles(TaskData data, bool disconnect = false)
        {
            foreach(TaskDataFile file in data.DataFiles)
            {
                Execute($"SELECT * FROM task_data_files WHERE " +
                    $"task_data_id='{data.DataID}' AND " +
                    $"description='{file.Description.Replace("'", "''")}' AND " +
                    $"location='{file.Path.Replace("'", "''")}'");
                if (reader.Read()) { continue; }
                Execute($"INSERT INTO task_data_files (task_data_id,description,location) " +
                    $"VALUES ({data.DataID},'{file.Description.Replace("'", "''")}','{file.Path.Replace("'","''")}')");
            }
        }
        private bool SaveParameter(Parameter p, bool disconnect = true)
        {
            try
            {
                if (Connect())
                {
                    string command = $"INSERT INTO findings (data_id,name,tolerance,tolerance_is_percent," +
                        $"unit_of_measure,measurement_before,measurement_after,setting) " +
                        $"VALUES ('{p.DataId}'," +
                        $"'{p.Name.Replace("'", "''")}'," +
                        $"'{p.Tolerance}'," +
                        $"'{(p.ToleranceIsPercent ? 1 : 0)}'," +
                        $"'{p.UnitOfMeasure}'," +
                        $"'{p.MeasurementBefore}'," +
                        $"'{p.MeasurementAfter}'," +
                        $"'{p.Setting}')";
                    Execute(command);
                    if (disconnect) { Disconnect(); }
                    return true;
                }
                else { return false; }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        //Remove data----------------------------------------------------------------------------------------------------------------------
        //Delete operations in the DB cascade Item -> Task -> TaskData
        public bool RemoveItem(string sn)
        {
            try
            {
                if (Connect())
                {
                    Execute($"DELETE FROM items WHERE serial_number='{sn}'");
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
        public bool RemoveTask(string taskID)
        {
            try
            {
                if (Connect())
                {
                    Execute($"DELETE FROM tasks WHERE id='{taskID}'");
                    Disconnect();
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error while removing task from the database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool RemoveTaskData(string id)
        {
            try
            {
                if (Connect())
                {
                    Execute($"DELETE FROM task_data WHERE id='{id}'");
                    Disconnect();
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error while removing task data from the database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        //Misc members-------------------------------------------------------------------------------------------------------------
        public int CheckStandardEquipment(CTStandardEquipment e)
        {
            Execute($"SELECT * FROM standard_equipment WHERE " +
                $"serial_number='{e.SerialNumber}' AND " +
                $"action_due_date='{e.ActionDueDate.ToString(dateFormat)}' AND " +
                $"certificate_number='{e.CertificateNumber}'");
            if (reader.Read()) { return reader.GetInt32(0); }
            SaveStandardEquipment(e);
            Execute("SELECT last_insert_rowid()");
            reader.Read();
            int equipmentId = reader.GetInt32(0);
            return equipmentId;
        }
        public void CleanUp()
        {
            if (IsConnected()) { Disconnect(); }
        }
        private int GetDatabaseVersion()
        {
            string command = "PRAGMA user_version";
            Execute(command);
            if (reader.Read())
            { return reader.GetInt32(0); }
            else
            { return 0; }
        }
        public int GetLastTaskID()
        {
            if (Connect())
            {
                Execute("SELECT * FROM tasks ORDER BY id DESC LIMIT 1");
                reader.Read();
                int taskID = reader.GetInt32(0);
                Disconnect();
                return taskID;
            }
            else { return -1; }
        }
        private void ResetConnection()
        {
            conn.Close();
            conn.Open();
        }

        //Data parsing---------------------------------------------------------------------------------------------------------------------
        private List<CTItem> AssignItemValues(List<Dictionary<string,string>> queryResults)
        {
            List<CTItem> items = new();
            try
            {
                foreach (Dictionary<string, string> row in queryResults)
                {
                    items.Add(new CTItem(row));
                }
                return items;
            }
            catch(Exception ex)
            {
                MessageBox.Show(
                    $"Error parsing query results: {ex.Message}",
                    "CTDatabase.AssignItemValues",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                items.Clear();
                return items;
            }
        }
        private void AssignStandardEquipmentValues(ref CTStandardEquipment item)
        {
            item.Manufacturer = reader.GetString((int)StandardEquipmentColumns.manufacturer);
            item.Model = reader.GetString((int)StandardEquipmentColumns.model);
            item.Description = reader.GetString((int)StandardEquipmentColumns.description);
            item.Remarks = reader.GetString((int)StandardEquipmentColumns.remarks);
            if (reader.GetString((int)StandardEquipmentColumns.timestamp).Length > 0)
            { item.TimeStamp = DateTime.ParseExact(reader.GetString((int)StandardEquipmentColumns.timestamp), timestampFormat, CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString((int)StandardEquipmentColumns.item_group);
            item.CertificateNumber = reader.GetString((int)StandardEquipmentColumns.certificate_number);
            item.ActionDueDate = DateTime.ParseExact(reader.GetString((int)StandardEquipmentColumns.action_due_date), dateFormat, CultureInfo.InvariantCulture);
            item.ChangesMade = false;
        }
        private void AssignDataValues(ref TaskData data)
        {
            data.DataID = reader.GetInt32((int)TaskDataColumns.id);
            data.TaskID = reader.GetInt32((int)TaskDataColumns.task_id);
            data.SerialNumber = reader.GetString((int)TaskDataColumns.serial_number);
            if (reader.GetString((int)TaskDataColumns.complete_date).Length > 0)
            { data.CompleteDate = DateTime.ParseExact(reader.GetString((int)TaskDataColumns.complete_date), dateFormat, CultureInfo.InvariantCulture); }
            data.Procedure = reader.GetString((int)TaskDataColumns.procedure);
            
            data.Remarks = reader.GetString((int)TaskDataColumns.remarks);
            data.Technician = reader.GetString((int)TaskDataColumns.technician);
            data.Timestamp = reader.GetString((int)TaskDataColumns.timestamp);

            data.StateBefore = new()
            {
                InTolerance = reader.GetBoolean((int)TaskDataColumns.in_tolerance_before),
                Operational = reader.GetBoolean((int)TaskDataColumns.operational_before)
            };
            data.StateAfter = new()
            {
                InTolerance = reader.GetBoolean((int)TaskDataColumns.in_tolerance_after),
                Operational = reader.GetBoolean((int)TaskDataColumns.operational_after)
            };
            data.Actions = new()
            {
                Calibration = reader.GetBoolean((int)TaskDataColumns.calibrated),
                Verification = reader.GetBoolean((int)TaskDataColumns.verified),
                Adjusted = reader.GetBoolean((int)TaskDataColumns.adjusted),
                Repaired = reader.GetBoolean((int)TaskDataColumns.repaired),
                Maintenance = reader.GetBoolean((int)TaskDataColumns.maintenance)
            };
            data.ChangesMade = false;
        }
        private void AssignFindingsValues(ref Parameter param)
        {
            param.Id = reader.GetInt32((int)FindingsColumns.id);
            param.DataId = reader.GetInt32((int)FindingsColumns.data_id);
            param.Name = reader.GetString((int)FindingsColumns.name);
            param.Tolerance = reader.GetFloat((int)FindingsColumns.tolerance);
            param.ToleranceIsPercent = reader.GetBoolean((int)FindingsColumns.tolerance_is_percent);
            param.UnitOfMeasure = reader.GetString((int)FindingsColumns.unit_of_measure);
            param.MeasurementBefore = reader.GetFloat((int)FindingsColumns.measurement_before);
            param.MeasurementAfter = reader.GetFloat((int)FindingsColumns.measurement_after);
            param.Setting = reader.GetFloat((int)FindingsColumns.setting);
        }
        private List<CTTask> AssignTaskValues(List<Dictionary<string, string>> queryResults)
        {
            List<CTTask> tasks = new();
            try
            {
                foreach (Dictionary<string, string> row in queryResults)
                {
                    tasks.Add(new CTTask(row));
                }
                return tasks;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error parsing query results: {ex.Message}",
                    "CTDatabase.AssignTaskValues",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                tasks.Clear();
                return tasks;
            }
        }
    }
}
