using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.Diagnostics;
using System.Globalization;
using System.Data;

namespace CalTools_WPF
{
    class CTDatabase
    {
        private bool tablesExist = false;
        private SqliteConnection conn;
        private SqliteDataReader reader;
        public string DbPath { get; set; }
        public CTDatabase(string dbPath)
        {
            this.DbPath = dbPath;
            conn = new SqliteConnection($"Data Source={DbPath}");
        }
        public bool Connect()
        {
            try
            {
                if (!IsConnected())
                {
                    conn.Open();
                    //Assumes blank DB. Should only run CreateTables() once.
                    if (!tablesExist) { if (CreateTables()) { tablesExist = true; } }
                }
                return true;
            }
            catch(Microsoft.Data.Sqlite.SqliteException ex)
            {
                MessageBox.Show(ex.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
                return false;
            }
        }
        public bool Disconnect() //True if disconnected successfully, false if error
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
        public List<CalibrationItem> GetAllItems(string table)
        {
            List<CalibrationItem> allItems = new List<CalibrationItem>();
            string command = $"SELECT * FROM {table}";
            if (!Connect()) { return allItems; }
            Execute(command);
            while(reader.Read())
            {
                CalibrationItem item = new CalibrationItem(reader.GetString(0));
                AssignDBValues(ref item);
                allItems.Add(item);
            }
            Disconnect();
            return allItems;
        }
#nullable enable
        public CalibrationItem? GetCalItem(string table, string col, string item)
        {
            string command = $" SELECT * FROM {table} WHERE {col}='{item}'";
            if(!Connect()) return null;
            Execute(command);
            if (reader.Read()) 
            {
                CalibrationItem returnItem = new CalibrationItem(reader.GetString(0));
                AssignDBValues(ref returnItem);
                Disconnect();
                return returnItem;
            }
            Disconnect();
            return null;
        }
#nullable disable
        //Private members
        private bool CreateTables()
        {
            try
            {
                //Check DB version
                string command = "PRAGMA user_version";
                int dbVersion = 0;
                Execute(command);
                if (reader.Read())
                { dbVersion = reader.GetInt32(0); }
                if (dbVersion < 4)
                {
                    command = "CREATE TABLE IF NOT EXISTS calibration_items (" +
                            "serial_number TEXT PRIMARY KEY," +
                            "location TEXT DEFAULT ''," +
                            "interval INTEGER DEFAULT 12," +
                            "cal_vendor TEXT DEFAULT ''," +
                            "manufacturer TEXT DEFAULT ''," +
                            "lastcal DEFAULT ''," +
                            "nextcal DEFAULT ''," +
                            "mandatory INTEGER DEFAULT 1," +
                            "directory TEXT DEFAULT ''," +
                            "description TEXT DEFAULT ''," +
                            "inservice INTEGER DEFAULT 1," +
                            "inservicedate DEFAULT ''," +
                            "outofservicedate DEFAULT ''," +
                            "caldue INTEGER DEFAULT 0," +
                            "model TEXT DEFAULT ''," +
                            "comment TEXT DEFAULT ''," +
                            "timestamp TEXT DEFAULT ''," +
                            "item_group TEXT DEFAULT ''," +
                            "verify_or_calibrate TEXT DEFAULT 'CALIBRATE'," +
                            "certificate_number TEXT DEFAULT '')";
                    Execute(command);
                    command = "CREATE TABLE IF NOT EXISTS item_groups (" +
                            "name TEXT PRIMARY KEY," +
                            "items TEXT DEFAULT '[]'," +
                            "include_all_model INTEGER DEFAULT 0)";
                    Execute(command);
                    command = "CREATE TABLE IF NOT EXISTS calibration_data (" +
                            "serial_number TEXT PRIMARY KEY," +
                            "state_before_action TEXT DEFAULT ''," +
                            "state_after_action TEXT DEFAULT ''," +
                            "action_taken TEXT DEFAULT ''," +
                            "calibration_date TEXT DEFAULT ''," +
                            "due_date TEXT DEFAULT ''," +
                            "procedure TEXT DEFAULT ''," +
                            "standard_equipment TEXT DEFAULT ''," +
                            "findings TEXT DEFAULT ''," +
                            "technician TEXT DEFAULT ''," +
                            "entry_timestamp TEXT DEFAULT '')";
                    Execute(command);
                    if(dbVersion == 3) { command = "ALTER TABLE calibration_items ADD certificate_number TEXT DEFAULT ''"; Execute(command);}
                    command = "PRAGMA user_version = 4";
                    Execute(command);
                }
                return true;
            }
            catch(System.Exception ex)
            {
                MessageBox.Show(ex.Message, "SQLite Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private void AssignDBValues(ref CalibrationItem item)
        {
            item.Location = reader.GetString((int)CalibrationItem.DatabaseColumns.location);
            item.Interval = reader.GetInt32((int)CalibrationItem.DatabaseColumns.interval);
            item.CalVendor = reader.GetString((int)CalibrationItem.DatabaseColumns.cal_vendor);
            item.Manufacturer = reader.GetString((int)CalibrationItem.DatabaseColumns.manufacturer);
            if (reader.GetString(5).Length > 0) { item.LastCal = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.lastcal), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            if (reader.GetString(6).Length > 0) { item.NextCal = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.nextcal), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            item.Mandatory = reader.GetString((int)CalibrationItem.DatabaseColumns.mandatory) == "1";
            item.Directory = reader.GetString((int)CalibrationItem.DatabaseColumns.directory);
            item.Description = reader.GetString((int)CalibrationItem.DatabaseColumns.description);
            item.InService = reader.GetString((int)CalibrationItem.DatabaseColumns.inservice) == "1";
            if (reader.GetString(11).Length > 0) { item.InServiceDate = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.inservicedate), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            if (reader.GetString(12).Length > 0) { item.OutOfServiceDate = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.outofservicedate), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            item.CalDue = reader.GetString((int)CalibrationItem.DatabaseColumns.caldue) == "1";
            item.Model = reader.GetString((int)CalibrationItem.DatabaseColumns.model);
            item.Comment = reader.GetString((int)CalibrationItem.DatabaseColumns.comments);
            if (reader.GetString(16).Length > 0) { item.TimeStamp = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.timestamp), "yyyy-MM-dd-HH-mm-ss-ffffff", CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString((int)CalibrationItem.DatabaseColumns.item_group);
            item.VerifyOrCalibrate = reader.GetString((int)CalibrationItem.DatabaseColumns.verify_or_calibrate);
        }
    }
}
