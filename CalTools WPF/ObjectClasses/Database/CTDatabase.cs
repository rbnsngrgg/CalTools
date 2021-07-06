using CalTools_WPF.ObjectClasses;
using CalTools_WPF.ObjectClasses.Database;
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
    internal partial class CTDatabase
    {
        public readonly int currentVersion = 7;
        public readonly string dateFormat = "yyyy-MM-dd";
        public readonly string timestampFormat = "yyyy-MM-dd-HH-mm-ss-ffffff";
        public List<string> Folders { get; set; }
        private readonly IConnectionHandler handler;
        public string DbPath { get; set; }
        public CTDatabase(string dbPath, IConnectionHandler connectionHandler = null)
        {
            DbPath = dbPath;
            handler = connectionHandler ?? new SqliteConnectionHandler(DbPath);
        }

        //Basic Operations-----------------------------------------------------------------------------------------------------------------
        public bool DatabaseReady()
        {
            if (handler.DatabaseReady() && handler.GetDatabaseVersion() == currentVersion)
            {
                return true;
            }
            else
            {
                return UpdateDatabase();
            }
        }

        //Data Retrieval-------------------------------------------------------------------------------------------------------------------
        private string TableForType(Type type)
        {
            string table = "";
            if (type == typeof(CTItem)) { table = "items"; }
            else if (type == typeof(CTTask)) { table = "tasks"; }
            else if (type == typeof(Findings)) { table = "findings"; }
            else if (type == typeof(TaskData)) { table = "task_data"; }
            else if (type == typeof(TaskDataFile)) { table = "task_data_files"; }
            else if (type == typeof(CTStandardEquipment)) { table = "standard_equipment"; }
            return table;
        }
        public List<Type> GetAll<Type>() where Type : ICTObject, new()
        {
            List<Type> items = AssignValues<Type>(
                handler.SelectAllFromTable(TableForType(typeof(Type))));
            return items;
        }
        public List<Type> GetFromWhere<Type>(Dictionary<string,string> whereValues) where Type : ICTObject, new()
        {
            List<Type> items = AssignValues<Type>(
                handler.SelectFromTableWhere(
                    TableForType(typeof(Type)),
                    whereValues));
            return items;
        }
        public List<CTStandardEquipment> GetDataStandardEquipment(int taskDataId)
        {
            List<CTStandardEquipment> equipment = AssignValues<CTStandardEquipment>(
                handler.SelectStandardEquipmentWhere(
                    new() { { "task_data_id", $"{taskDataId}" } }
                ));
            return equipment;
        }

        //Save data------------------------------------------------------------------------------------------------------------------------
        public void SaveItem(CTItem item, bool timestampOverride = false)
        {
            handler.InsertIntoTable("items", new() { { "serial_number", item.SerialNumber } });
            handler.UpdateTable("items",
                new()
                {
                    { "serial_number", item.SerialNumber },
                    { "model", item.Model },
                    { "description", item.Description },
                    { "location", item.Location },
                    { "manufacturer", item.Manufacturer },
                    { "directory", item.Directory },
                    { "in_service", item.InService ? "1" : "0" },
                    { "remarks", item.Remarks },
                    { "timestamp", timestampOverride ?
                        item.TimeStampString :
                        DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture) },
                    { "item_group", item.ItemGroup },
                    { "certificate_number", item.CertificateNumber },
                    { "is_standard_equipment", item.IsStandardEquipment ? "1" : "0" }
                },
                new()
                {
                    { "serial_number", $"{item.SerialNumber}" }
                });
        }
        internal virtual void SaveDataStandardEquipment(int dataId, int equipmentId)
        {
            List<Dictionary<string, string>> id_pairs = handler.SelectFromTableWhere(
                "data_standard_equipment",
                new()
                {
                    { "task_data_id", dataId.ToString() },
                    { "standard_equipment_id", equipmentId.ToString() }
                });
            if (id_pairs.Count == 0)
            {
                handler.InsertIntoTable(
                    "data_standard_equipment",
                    new()
                    {
                        { "task_data_id", dataId.ToString() },
                        { "standard_equipment_id", equipmentId.ToString() }
                    },
                    false);
            }
        }
        public int SaveStandardEquipment(CTStandardEquipment item)
        {
            int id = handler.InsertIntoTable("standard_equipment",
                new()
                {
                    { "serial_number", $"{item.SerialNumber}" },
                    { "model", $"{item.Model}" },
                    { "description", $"{item.Description}" },
                    { "manufacturer", $"{item.Manufacturer}" },
                    { "remarks", $"{item.Remarks}" },
                    { "timestamp", item.TimeStamp != DateTime.MinValue ?
                        item.TimeStampString :
                        DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture) },
                    { "item_group", $"{item.ItemGroup}" },
                    { "certificate_number", $"{item.CertificateNumber}" },
                    { "action_due_date", $"{item.ActionDueDate.ToString(dateFormat, CultureInfo.InvariantCulture)}" }
                }, false);
            return id;
        }
        public int SaveTask(CTTask task)
        {
            int id = task.TaskId;
            Dictionary<string, string> colValues = new()
            {
                { "id", $"{task.TaskId}" },
                { "serial_number", $"{task.SerialNumber}"},
                { "task_title", $"{task.TaskTitle}" },
                { "service_vendor", $"{task.ServiceVendor}" },
                { "is_mandatory", $"{(task.IsMandatory ? 1 : 0)}" },
                { "interval", $"{task.Interval}" },
                { "complete_date", $"{task.CompleteDateString}" },
                { "due_date", $"{task.DueDateString}" },
                { "is_due", $"{(task.DueDate.HasValue && task.DueDate.Value <= DateTime.Today ? 1 : 0)}" },
                { "action_type", $"{task.ActionType}" },
                { "directory", $"{task.TaskDirectory}" },
                { "remarks", $"{task.Remarks}" },
                { "date_override", $"{task.DateOverrideString}" },
            };
            if(task.TaskId == -1)
            {
                colValues.Remove("id");
                id = handler.InsertIntoTable("tasks", colValues);
            }
            else
            {
                if (handler.SelectFromTableWhere("tasks", new() { { "id", $"{task.TaskId}" } }).Count > 0)
                {
                    handler.UpdateTable("tasks", colValues,
                        new() { { "id", $"{task.TaskId}" } });
                }
                else
                {
                    handler.InsertIntoTable("tasks", colValues);
                }
            }
            return id;
        }
        public void SaveTaskData(TaskData data)
        {
            List<int> equipmentIds;
            try
            {
                equipmentIds = CheckStandardEquipment(data.StandardEquipment);
                if (data.TaskId == null)
                {
                    throw new ArgumentException("CTDatabase.SaveTaskData: Task data TaskID is null.");
                }
            }
            catch (ArgumentException ex)
            {
#if DEBUG
                Debug.WriteLine(ex.Message);
#else
                MessageBox.Show($"{ex.Message}", "CTDatabase.SaveTaskData", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                return;
            }

            string newTimestamp = data.Timestamp != DateTime.MinValue ? 
                data.TimestampString : DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture);
            Dictionary<string, string> colValues = new()
            {
                { "task_id", $"{data.TaskId}" },
                { "serial_number", $"{data.SerialNumber}" },
                { "in_tolerance_before", $"{(data.StateBefore.Value.InTolerance ? 1 : 0)}" },
                { "operational_before", $"{(data.StateBefore.Value.Operational ? 1 : 0)}" },
                { "in_tolerance_after", $"{(data.StateAfter.Value.InTolerance ? 1 : 0)}" },
                { "operational_after", $"{(data.StateAfter.Value.Operational ? 1 : 0)}" },
                { "calibrated", $"{(data.Actions.Value.Calibration ? 1 : 0)}" },
                { "verified", $"{(data.Actions.Value.Verification ? 1 : 0)}" },
                { "adjusted", $"{(data.Actions.Value.Adjusted ? 1 : 0)}" },
                { "repaired", $"{(data.Actions.Value.Repaired ? 1 : 0)}" },
                { "maintenance", $"{(data.Actions.Value.Maintenance ? 1 : 0)}" },
                { "complete_date", $"{data.CompleteDateString}" },
                { "procedure", $"{data.Procedure}" },
                { "remarks", $"{data.Remarks}" },
                { "technician", $"{data.Technician}" },
                { "timestamp", newTimestamp }
            };
            if(data.DataId != -1) { colValues.Add("id", $"{data.DataId}"); }
            data.DataId = handler.InsertIntoTable("task_data", colValues, false);

            foreach(Findings p in data.Findings)
            {
                p.DataId = data.DataId;
                SaveFindings(p);
            }
            foreach(int equipmentId in equipmentIds)
            {
                SaveDataStandardEquipment(data.DataId, equipmentId);
            }
            SaveTaskDataFiles(data);
        }
        internal virtual void SaveTaskDataFiles(TaskData data)
        {
            foreach (TaskDataFile file in data.DataFiles)
            {
                Dictionary<string, string> colValues = new()
                {
                    { "task_data_id", $"{data.DataId}" },
                    { "description", $"{file.Description}" },
                    { "location", $"{file.Location}" }
                };
                List<Dictionary<string,string>> fileRow = handler.SelectFromTableWhere("task_data_files", colValues);
                if (fileRow.Count == 0) { handler.InsertIntoTable("task_data_files", colValues, false); }
            }
        }
        internal virtual void SaveFindings(Findings findings)
        {
            if(findings.DataId != -1)
            {
                handler.InsertIntoTable("findings",
                    new()
                    {
                        { "task_data_id", $"{findings.DataId}" },
                        { "name", $"{findings.Name}" },
                        { "tolerance", $"{findings.Tolerance}" },
                        { "tolerance_is_percent", $"{(findings.ToleranceIsPercent ? 1 : 0)}" },
                        { "unit_of_measure", $"{findings.UnitOfMeasure}" },
                        { "measurement_before", $"{findings.MeasurementBefore}" },
                        { "measurement_after", $"{findings.MeasurementAfter}" },
                        { "setting", $"{findings.Setting}" },
                    },
                    false);
            }
            else
            {
                throw new ArgumentException("CTDatabase.SaveFindings: Invalid findings.DataId (-1).");
            }
        }

        //Remove data----------------------------------------------------------------------------------------------------------------------
        //Delete operations in the DB cascade Item -> Task -> TaskData
        public void Remove<Type>(Dictionary<string, string> whereValues) where Type : ICTObject, new()
        {
            handler.RemoveFromTable(
                TableForType(typeof(Type)),
                whereValues);
        }

        //Misc members-------------------------------------------------------------------------------------------------------------
        internal virtual List<int> CheckStandardEquipment(List<CTStandardEquipment> equipmentList)
        {
            List<int> idList = new();
            foreach(CTStandardEquipment e in equipmentList)
            {
                List<Dictionary<string, string>> equipment = handler.SelectFromTableWhere("standard_equipment", new()
                {
                    { "serial_number", $"{e.SerialNumber}" },
                    { "certificate_number", $"{e.CertificateNumber}" }
                });
                if (equipment.Count > 0)
                {
                    List<CTStandardEquipment> items = AssignValues<CTStandardEquipment>(equipment);
                    if (items[0].ActionDueDate.Date != e.ActionDueDate.Date)
                    {
                        throw new ArgumentException($"CTDatabase.CheckStandardEquipment: The selected standard equipment matches the serial number" +
                            $" and certificate number of an existing database entry, but with a different date. " +
                            $"The current or previous certificate number must be updated. The data was not saved.");
                    }
                    else
                    {
                        idList.Add(items[0].Id);
                    }
                }
                else
                {
                    idList.Add(SaveStandardEquipment(e));
                }
            }
            return idList;
        }

        //Data parsing---------------------------------------------------------------------------------------------------------------------
        internal virtual List<Type> AssignValues<Type>(List<Dictionary<string, string>> queryResults) where Type : ICTObject, new()
        {
            List<Type> data = new();
            try
            {
                foreach (Dictionary<string, string> row in queryResults)
                {
                    Type item = new Type();
                    item.ParseParameters(row);
                    data.Add(item);
                }
                if (typeof(Type) == typeof(TaskData))
                {
                    data = AssignTaskDataComponents(data as List<TaskData>) as List<Type>;
                }
                return data;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"CTDatabase.AssignValues<{typeof(Type)}>:\n\tError parsing query results: {ex.Message}");
#else
                MessageBox.Show(
                    $"Error parsing query results: {ex.Message}",
                    $"CTDatabase.AssignValues<{typeof(Type)}>",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
#endif
                data.Clear();
                return data;
            }
        }
        internal virtual List<TaskData> AssignTaskDataComponents(List<TaskData> taskData)
        {
            foreach (TaskData data in taskData)
            {
                data.Findings = GetFromWhere<Findings>(new() { { "task_data_id", $"{data.DataId}" } });
                data.StandardEquipment = GetDataStandardEquipment(data.DataId);
                data.DataFiles = GetFromWhere<TaskDataFile>(new() { { "task_data_id", $"{data.DataId}" } });
            }
            return taskData;
        }

        public void ExportDb(string exportFolder)
        {
            string targetFolder = Path.Join(exportFolder, $"TSV_Export_{DateTime.UtcNow.ToString(timestampFormat)}");
            if (!Directory.Exists(Directory.GetParent(targetFolder).FullName))
            {
                MessageBox.Show($"Directory does not exist or is inaccessible. {Directory.GetParent(targetFolder).FullName}",
                    "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Directory.CreateDirectory(targetFolder);
            foreach (string table in handler.TableNames)
            {
                if (table.Contains("old")) { continue; }
                List<string> tableLines = new();
                List<Dictionary<string, string>> tableItems = handler.SelectAllFromTable(table);
                if(tableItems.Count == 0) { continue; }
                foreach (Dictionary<string, string> row in tableItems)
                {
                    if(tableLines.Count == 0)
                    {
                        tableLines.Add("");
                        foreach(string key in row.Keys)
                        {
                            tableLines[0] = $"{tableLines[0]}{key}\t";
                        }
                    }
                    string newLine = "";
                    foreach(string key in row.Keys)
                    {
                        newLine = $"{newLine}{row[key]}\t";
                    }
                    tableLines.Add(newLine);
                }
                File.WriteAllLines(Path.Join(targetFolder, $"{table}.txt"), tableLines);
            }
            Process.Start("explorer", targetFolder);
        }
    }
    public interface ICTObject
    {
        public void ParseParameters(Dictionary<string, string> parameters);
    }
}
