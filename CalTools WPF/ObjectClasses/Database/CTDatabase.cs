using CalTools_WPF.ObjectClasses;
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
        public readonly string dateFormat = "yyyy-MM-dd";
        public readonly string timestampFormat = "yyyy-MM-dd-HH-mm-ss-ffffff";
        public bool tablesExist = false;
        public string ItemScansDir { get; set; }
        public List<string> Folders { get; set; }
        private SqliteConnection conn;
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
            SqliteCommand command = new SqliteCommand(com, conn);
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
            List<CTItem> allItems = new List<CTItem>();
            if (!Connect()) { return allItems; }
            Execute("SELECT * FROM Items");
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
            if (!Connect()) { return allTasks; }
            Execute("SELECT * FROM Tasks");
            while (reader.Read())
            {
                CTTask task = new CTTask();
                AssignTaskValues(ref task);
                allTasks.Add(task);
            }
            Disconnect();
            return allTasks;
        }
        public List<TaskData> GetAllTaskData()
        {
            List<TaskData> calData = new List<TaskData>();
            if (Connect())
            {
                Execute($"SELECT * FROM TaskData");
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
        public List<TaskData> GetTaskData(string taskID)
        {
            List<TaskData> calData = new List<TaskData>();
            if (Connect())
            {
                Execute($"SELECT * FROM TaskData WHERE TaskID='{taskID}'");
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
            if (!Connect()) return null;
            Execute($" SELECT * FROM Items WHERE {col}='{item}'");
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
            if (!Connect()) return null;
            Execute($"SELECT * FROM Items WHERE SerialNumber='{task.SerialNumber}'");
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
        public TaskData? GetData(string col, string item)
        {
            if (!Connect()) return null;
            Execute($" SELECT * FROM TaskData WHERE {col}='{item}'");
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
        public List<CTTask> GetTasks(string col, string item, bool disconnect = true)
        {
            List<CTTask> tasks = new List<CTTask>();
            if (!Connect()) return tasks;
            Execute($"SELECT * FROM Tasks WHERE {col}='{item}'");
            while (reader.Read())
            {
                CTTask task = new CTTask();
                AssignTaskValues(ref task);
                tasks.Add(task);
            }
            if (disconnect) { Disconnect(); }
            return tasks;
        }
#nullable disable

        //Save data------------------------------------------------------------------------------------------------------------------------
        public bool CreateItem(string sn)
        {
            try
            {
                if (Connect())
                {
                    Execute($"INSERT OR IGNORE INTO Items (SerialNumber) VALUES ('{sn}')");
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
        public bool SaveTask(CTTask task, bool disconnect = false)
        {
            string command;
            try
            {
                if (Connect())
                {
                    if (task.TaskID == -1)
                    {
                        command = $"INSERT OR IGNORE INTO Tasks " +
                            $"(SerialNumber,TaskTitle,ServiceVendor,Mandatory," +
                            $"Interval,CompleteDate,DueDate,Due,ActionType,Directory,Comments) " +
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
                          $"'{task.Comment}')";
                    }
                    else
                    {
                        command = $"UPDATE Tasks SET " +
                            $"SerialNumber='{task.SerialNumber}'," +
                            $"TaskTitle='{task.TaskTitle}'," +
                            $"ServiceVendor='{task.ServiceVendor}'," +
                            $"Mandatory='{(task.Mandatory == true ? 1 : 0)}'," +
                            $"Interval='{task.Interval}'," +
                            $"CompleteDate='{task.CompleteDateString}'," +
                            $"DueDate='{task.DueDateString}'," +
                            $"Due='{(task.Due == true ? 1 : 0)}'," +
                            $"ActionType='{task.ActionType}'," +
                            $"Directory='{task.TaskDirectory}'," +
                            $"Comments='{task.Comment}' " +
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

        //Remove data----------------------------------------------------------------------------------------------------------------------
        //Delete operations in the DB cascade Item -> Task -> TaskData
        public bool RemoveCalItem(string sn)
        {
            try
            {
                if (Connect())
                {
                    Execute($"DELETE FROM Items WHERE SerialNumber='{sn}'");
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
                    Execute($"DELETE FROM Tasks WHERE TaskID='{taskID}'");
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
                    Execute($"DELETE FROM TaskData WHERE DataID='{id}'");
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
            task.TaskDirectory = reader.GetString((int)CTTask.DatabaseColumns.Directory);
            task.Comment = reader.GetString((int)CTTask.DatabaseColumns.Comments);
            task.ChangesMade = false;
        }
    }
}
