using CalTools_WPF.ObjectClasses;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                    Debug.WriteLine(command);
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
                    Debug.WriteLine(command);
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
        public bool RemoveTaskData(string id)
        {
            try
            {
                if (Connect())
                {
                    string command = $"DELETE FROM TaskData WHERE DataID='{id}'";
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
            task.TaskDirectory = reader.GetString((int)CTTask.DatabaseColumns.Directory);
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

        private void MoveToTaskFolder(CTItem item, CTTask task)
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

        //Check for task subfolder, create the subfolder if it doesn't exist
        //Delete? Handled in MainWIndowCTLogic.xaml.cs
        private void CheckTaskfolders()
        {
            List<CTItem> allItems = GetAllItems();
            List<CTTask> allTasks = GetAllTasks();
            foreach (CTItem item in allItems)
            {
                foreach (CTTask task in allTasks)
                {
                    bool taskFolderFound = false;
                    foreach (string taskFolder in Directory.GetDirectories(item.Directory))
                    {
                        if (taskFolder.Split("_")[0] == task.TaskID.ToString())
                        { taskFolderFound = true; task.TaskDirectory = taskFolder; }
                    }
                    if (!taskFolderFound)
                    {
                        string newTaskFolder = Path.Join(item.Directory, $"{task.TaskID}_{task.TaskTitle}");
                        task.TaskDirectory = newTaskFolder;
                        //Directory.CreateDirectory(newTaskFolder);
                    }
                    if (task.ChangesMade) { SaveTask(task); }
                }
            }
        }
        //Make sure db is disconnected before closing window.
        public void CleanUp()
        {
            if (IsConnected()) { Disconnect(); }
        }
    }
}
