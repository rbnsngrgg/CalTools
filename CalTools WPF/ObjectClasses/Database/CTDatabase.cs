﻿using CalTools_WPF.ObjectClasses;
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
        private readonly SqliteConnection conn;
        private SqliteDataReader reader;
        public string DbPath { get; set; }
        public CTDatabase(string dbPath)
        {
            this.DbPath = dbPath;
            conn = new SqliteConnection($"Data Source={DbPath}");
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
            if (Connect())
            { 
                if (Disconnect()) 
                { return true; } 
                else 
                { return false; } }
            else 
            { return false; }
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
        private void Execute(string com)
        {
            SqliteCommand command = new(com, conn);
            reader = command.ExecuteReader();
        }
        public bool IsConnected()
        {
            if (conn.State == System.Data.ConnectionState.Open) { return true; }
            else { return false; }
        }

        //Data Retrieval-------------------------------------------------------------------------------------------------------------------
        public List<CTItem> GetAllItems()
        {
            List<CTItem> allItems = new();
            if (!Connect()) { return allItems; }
            Execute("SELECT * FROM items");
            while (reader.Read())
            {
                CTItem item = new(reader.GetString(0));
                AssignItemValues(ref item);
                allItems.Add(item);
            }
            Disconnect();
            return allItems;
        }
        public List<CTTask> GetAllTasks()
        {
            List<CTTask> allTasks = new();
            if (!Connect()) { return allTasks; }
            Execute("SELECT * FROM tasks");
            while (reader.Read())
            {
                CTTask task = new();
                AssignTaskValues(ref task);
                allTasks.Add(task);
            }
            Disconnect();
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
            }
            return calData;
        }
        public List<TaskData> GetTaskData(string taskID)
        {
            List<TaskData> calData = new();
            if (Connect())
            {
                Execute($"SELECT * FROM task_data WHERE TaskID='{taskID}'");
                while (reader.Read())
                {
                    TaskData data = new();
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
            if (!Connect()) return null;
            Execute($" SELECT * FROM items WHERE {col}='{item}'");
            if (reader.Read())
            {
                CTItem returnItem = new(reader.GetString(0));
                AssignItemValues(ref returnItem);
                Disconnect();
                return returnItem;
            }
            Disconnect();
            return null;
        }
        public CTItem? GetItemFromTask(CTTask task)
        {
            if (!Connect()) return null;
            Execute($"SELECT * FROM items WHERE SerialNumber='{task.SerialNumber}'");
            if (reader.Read())
            {
                CTItem returnItem = new(reader.GetString(0));
                AssignItemValues(ref returnItem);
                Disconnect();
                return returnItem;
            }
            Disconnect();
            return null;
        }
        public TaskData? GetData(string col, string item)
        {
            if (!Connect()) return null;
            Execute($" SELECT * FROM task_data WHERE {col}='{item}'");
            if (reader.Read())
            {
                TaskData returnItem = new();
                AssignDataValues(ref returnItem);
                Disconnect();
                return returnItem;
            }
            Disconnect();
            return null;
        }
        public List<CTTask> GetTasks(string col, string item, bool disconnect = true)
        {
            List<CTTask> tasks = new();
            if (!Connect()) return tasks;
            Execute($"SELECT * FROM tasks WHERE {col}='{item}'");
            while (reader.Read())
            {
                CTTask task = new();
                AssignTaskValues(ref task);
                tasks.Add(task);
            }
            if (disconnect) { Disconnect(); }
            return tasks;
        }
        public List<Parameter> GetFindingsFromTaskData(int taskDataId, bool disconnect = true)
        {
            List<Parameter> parameters = new();
            if(Connect())
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
                    string command = $"INSERT OR IGNORE INTO Items (serial_number) VALUES ('{item.SerialNumber.Replace("'", "''")}')";
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
                        $"is_standard_equipment='{(item.StandardEquipment == true ? 1 : 0)}' " +
                        $"WHERE serial_number='{item.SerialNumber.Replace("'", "''")}'";
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
        public bool SaveTask(CTTask task, bool disconnect = false)
        {
            string command;
            try
            {
                if (Connect())
                {
                    //New task that hasn't yet been inserted into the database
                    if (task.TaskID == -1)
                    {
                        command = $"INSERT OR IGNORE INTO tasks " +
                            $"(serial_number,task_title,service_vendor,is_mandatory," +
                            $"interval,complete_date,due_date,is_due,action_type,directory,remarks,date_override) " +
                          $"VALUES ('{task.SerialNumber}'," +
                          $"'{task.TaskTitle}'," +
                          $"'{task.ServiceVendor}'," +
                          $"'{(task.Mandatory == true ? 1 : 0)}'," +
                          $"'{task.Interval}'," +
                          $"'{task.CompleteDateString}'," +
                          $"'{task.DueDateString}'," +
                          $"'{(task.Due == true ? 1 : 0)}'," +
                          $"'{task.ActionType}'," +
                          $"'{task.TaskDirectory}'," +
                          $"'{task.Comment}'," +
                          $"'{task.DateOverrideString}')";
                    }
                    //Existing task
                    else
                    {
                        command = $"UPDATE tasks SET " +
                            $"serial_number='{task.SerialNumber}'," +
                            $"task_title='{task.TaskTitle}'," +
                            $"service_vendor='{task.ServiceVendor}'," +
                            $"is_mandatory='{(task.Mandatory == true ? 1 : 0)}'," +
                            $"interval='{task.Interval}'," +
                            $"complete_date='{task.CompleteDateString}'," +
                            $"due_date='{task.DueDateString}'," +
                            $"is_due='{(task.Due == true ? 1 : 0)}'," +
                            $"action_type='{task.ActionType}'," +
                            $"directory='{task.TaskDirectory}'," +
                            $"remarks='{task.Comment}'," +
                            $"date_override='{task.DateOverrideString}' " +
                            $"WHERE TaskID='{task.TaskID}'";
                    }
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
                        $"maintenance,complete_date,procedure,remarks,technician,timestamp) "+
                        $"VALUES ('{data.TaskID}'," +
                        $"'{data.SerialNumber.Replace("'", "''")}'," +
                        $"'{(data.StateBefore.Value.InTolerance ? 1 : 0)}'," +
                        $"'{(data.StateBefore.Value.Operational ? 1 : 0)}'," +
                        $"'{(data.StateAfter.Value.InTolerance ? 1 : 0)}'," +
                        $"'{(data.StateAfter.Value.Operational ? 1 : 0)}'," +
                        $"'{(data.ActionTaken.Value.Calibration ? 1: 0)}'," +
                        $"'{(data.ActionTaken.Value.Verification ? 1 : 0)}'," +
                        $"'{(data.ActionTaken.Value.Adjusted ? 1 : 0)}'," +
                        $"'{(data.ActionTaken.Value.Repaired ? 1 : 0)}'," +
                        $"'{(data.ActionTaken.Value.Maintenance ? 1 : 0)}'," +
                        $"'{((DateTime)data.CompleteDate).ToString(dateFormat)}'," +
                        $"'{data.Procedure.Replace("'", "''")}'," +
                        $"'{data.Remarks.Replace("'", "''")}'," +
                        $"'{data.Technician.Replace("'", "''")}'," +
                        $"'{(timestampOverride ? data.Timestamp : DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture))}')";
                    //TODO: check/insert standard equipment, insert findings
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
        //Delete operations in the DB cascade Item -> Task -> TaskData
        public bool RemoveCalItem(string sn)
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
                Execute("SELECT * FROM Tasks ORDER BY TaskID DESC LIMIT 1");
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
        private void AssignItemValues(ref CTItem item)
        {
            item.Location = reader.GetString((int)ItemsColumns.location);
            item.Manufacturer = reader.GetString((int)ItemsColumns.manufacturer);
            item.Directory = reader.GetString((int)ItemsColumns.directory);
            item.Description = reader.GetString((int)ItemsColumns.description);
            item.InService = reader.GetString((int)ItemsColumns.in_service) == "1";
            item.Model = reader.GetString((int)ItemsColumns.model);
            item.Remarks = reader.GetString((int)ItemsColumns.remarks);
            if (reader.GetString((int)ItemsColumns.timestamp).Length > 0)
            { item.TimeStamp = DateTime.ParseExact(reader.GetString((int)ItemsColumns.timestamp), timestampFormat, CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString((int)ItemsColumns.item_group);
            item.StandardEquipment = reader.GetBoolean((int)ItemsColumns.is_standard_equipment);
            item.CertificateNumber = reader.GetString((int)ItemsColumns.certificate_number);
            item.ChangesMade = false;
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
            data.ActionTaken = new()
            {
                Calibration = reader.GetBoolean((int)TaskDataColumns.calibrated),
                Verification = reader.GetBoolean((int)TaskDataColumns.verified),
                Adjusted = reader.GetBoolean((int)TaskDataColumns.adjusted),
                Repaired = reader.GetBoolean((int)TaskDataColumns.repaired),
                Maintenance = reader.GetBoolean((int)TaskDataColumns.maintenance)
            };
            data.Findings = GetFindingsFromTaskData((int)data.DataID, false);
            //TODO: add query for list of standard equipment
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
        private void AssignTaskValues(ref CTTask task)
        {
            task.TaskID = reader.GetInt32((int)TasksColumns.id);
            task.SerialNumber = reader.GetString((int)TasksColumns.serial_number);
            task.TaskTitle = reader.GetString((int)TasksColumns.task_title);
            task.ServiceVendor = reader.GetString((int)TasksColumns.service_vendor);
            task.Mandatory = reader.GetBoolean((int)TasksColumns.is_mandatory);
            task.Interval = reader.GetInt32((int)TasksColumns.interval);
            if (reader.GetString((int)TasksColumns.complete_date).Length > 0)
            { task.CompleteDate = DateTime.ParseExact(reader.GetString((int)TasksColumns.complete_date), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString((int)TasksColumns.due_date).Length > 0)
            { task.DueDate = DateTime.ParseExact(reader.GetString((int)TasksColumns.due_date), dateFormat, CultureInfo.InvariantCulture); }
            task.Due = reader.GetBoolean((int)TasksColumns.is_due);
            task.ActionType = reader.GetString((int)TasksColumns.action_type);
            task.TaskDirectory = reader.GetString((int)TasksColumns.directory);
            task.Comment = reader.GetString((int)TasksColumns.remarks);
            if (reader.GetString((int)TasksColumns.date_override).Length > 0)
            { task.DateOverride = DateTime.ParseExact(reader.GetString((int)TasksColumns.date_override), dateFormat, CultureInfo.InvariantCulture); }
            
            task.ChangesMade = false;
        }
    }
}
