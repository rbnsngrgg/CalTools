using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.Diagnostics;
using System.Globalization;
using System.Data;
using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;

namespace CalTools_WPF
{
    class CTDatabase
    {
        public readonly string dateFormat = "yyyy-MM-dd";
        public readonly string timestampFormat = "yyyy-MM-dd-HH-mm-ss-ffffff";
        private bool tablesExist = false;
        private SqliteConnection conn;
        private SqliteDataReader reader;
        public string DbPath { get; set; }
        public CTDatabase(string dbPath)
        {
            this.DbPath = dbPath;
            conn = new SqliteConnection($"Data Source={DbPath}");
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
        public List<CalibrationItem> GetAllCalItems()
        {
            List<CalibrationItem> allItems = new List<CalibrationItem>();
            string command = "SELECT * FROM calibration_items";
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
        public List<CalibrationData> GetCalData(string sn)
        {
            List<CalibrationData> calData = new List<CalibrationData>();
            if (Connect())
            {
                string command = $"SELECT * FROM calibration_data WHERE serial_number='{sn}'";
                Execute(command);
                while (reader.Read())
                {
                    CalibrationData data = new CalibrationData();
                    AssignDataValues(ref data);
                    calData.Add(data);
                }
                Disconnect();
            }
            return calData;
        }
#nullable enable
        public CalibrationItem? GetCalItem(string table, string col, string item)
        {
            string command = $" SELECT * FROM {table} WHERE {col}='{item}'";
            if (!Connect()) return null;
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
        //Data Storage---------------------------------------------------------------------------------------------------------------------
        public bool SaveCalItem(CalibrationItem item)
        {
            try
            {
                if (Connect())
                {
                    string command = $"INSERT OR IGNORE INTO calibration_items (serial_number) VALUES ('{item.SerialNumber}')";
                    Execute(command);
                    command = $"UPDATE calibration_items SET serial_number='{item.SerialNumber}', model='{item.Model}',description='{item.Description}',location='{item.Location}'," +
                                        $"manufacturer='{item.Manufacturer}',cal_vendor='{item.CalVendor}',interval='{item.Interval}',mandatory='{(item.Mandatory==true?1:0)}'," +
                                        $"directory='{item.Directory}',inservice='{(item.InService==true?1:0)}',inservicedate='{item.InServiceDate.Value.ToString(dateFormat, CultureInfo.InvariantCulture)}'," +
                                        $"lastcal='{(item.LastCal==null?"":item.LastCal.Value.ToString(dateFormat, CultureInfo.InvariantCulture))}'," +
                                        $"nextcal='{(item.NextCal==null?"":item.NextCal.Value.ToString(dateFormat, CultureInfo.InvariantCulture))}'," +
                                        $"outofservicedate='{(item.OutOfServiceDate==null?"":item.OutOfServiceDate.Value.ToString(dateFormat, CultureInfo.InvariantCulture))}'," +
                                        $"comment='{item.Comment}',timestamp='{DateTime.UtcNow.ToString(timestampFormat, CultureInfo.InvariantCulture)}',item_group='{item.ItemGroup}'," +
                                        $"verify_or_calibrate='{item.VerifyOrCalibrate}',certificate_number='{item.CertificateNumber}' " +
                                        $"WHERE serial_number='{item.SerialNumber}'";
                    Execute(command);
                    Disconnect();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(System.Exception ex)
            {
                MessageBox.Show(ex.Message,"Database Write Error",MessageBoxButton.OK,MessageBoxImage.Error);
                return false;
            }
        }
        public bool SaveCalData(CalibrationData data)
        {
            try
            {
                if(Connect())
                {
                    string command = $"INSERT INTO calibration_data (serial_number) VALUES ('{data.SerialNumber}')";
                    return true;
                }
                else { return false; }
            }
            catch(System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    if (!tablesExist) { if (CreateTables()) { tablesExist = true; } }
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
                            "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "serial_number TEXT," +
                            "state_before_action TEXT DEFAULT ''," +
                            "state_after_action TEXT DEFAULT ''," +
                            "action_taken TEXT DEFAULT ''," +
                            "calibration_date TEXT DEFAULT ''," +
                            "due_date TEXT DEFAULT ''," +
                            "procedure TEXT DEFAULT ''," +
                            "standard_equipment TEXT DEFAULT ''," +
                            "findings TEXT DEFAULT ''," +
                            "remarks TEXT DEFAULT ''," +
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
        //Parse DB columns to CalibrationItem object

        //Data parsing---------------------------------------------------------------------------------------------------------------------
        private void AssignDBValues(ref CalibrationItem item)
        {
            item.Location = reader.GetString((int)CalibrationItem.DatabaseColumns.location);
            item.Interval = reader.GetInt32((int)CalibrationItem.DatabaseColumns.interval);
            item.CalVendor = reader.GetString((int)CalibrationItem.DatabaseColumns.cal_vendor);
            item.Manufacturer = reader.GetString((int)CalibrationItem.DatabaseColumns.manufacturer);
            if (reader.GetString(5).Length > 0) { item.LastCal = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.lastcal), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString(6).Length > 0) { item.NextCal = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.nextcal), dateFormat, CultureInfo.InvariantCulture); }
            item.Mandatory = reader.GetString((int)CalibrationItem.DatabaseColumns.mandatory) == "1";
            item.Directory = reader.GetString((int)CalibrationItem.DatabaseColumns.directory);
            item.Description = reader.GetString((int)CalibrationItem.DatabaseColumns.description);
            item.InService = reader.GetString((int)CalibrationItem.DatabaseColumns.inservice) == "1";
            if (reader.GetString(11).Length > 0) { item.InServiceDate = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.inservicedate), dateFormat, CultureInfo.InvariantCulture); }
            if (reader.GetString(12).Length > 0) { item.OutOfServiceDate = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.outofservicedate), dateFormat, CultureInfo.InvariantCulture); }
            item.CalDue = reader.GetString((int)CalibrationItem.DatabaseColumns.caldue) == "1";
            item.Model = reader.GetString((int)CalibrationItem.DatabaseColumns.model);
            item.Comment = reader.GetString((int)CalibrationItem.DatabaseColumns.comments);
            if (reader.GetString(16).Length > 0) { item.TimeStamp = DateTime.ParseExact(reader.GetString((int)CalibrationItem.DatabaseColumns.timestamp), timestampFormat, CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString((int)CalibrationItem.DatabaseColumns.item_group);
            item.VerifyOrCalibrate = reader.GetString((int)CalibrationItem.DatabaseColumns.verify_or_calibrate);
        }
        //Parse DB columns to CalibrationData object
        private void AssignDataValues(ref CalibrationData data)
        {
            data.ID = reader.GetInt32((int)CalibrationData.DatabaseColumns.ColID);
            data.SerialNumber = reader.GetString((int)CalibrationData.DatabaseColumns.ColSerialNumber);
            data.StateBefore = JsonConvert.DeserializeObject<State>(reader.GetString((int)CalibrationData.DatabaseColumns.ColStateBeforeAction));
            data.StateAfter = JsonConvert.DeserializeObject<State>(reader.GetString((int)CalibrationData.DatabaseColumns.ColStateAfterAction));
            data.ActionTaken = JsonConvert.DeserializeObject<ActionTaken>(reader.GetString((int)CalibrationData.DatabaseColumns.ColActionTaken));
            if(reader.GetString((int)CalibrationData.DatabaseColumns.ColCalibrationDate).Length > 0)
            { data.CalibrationDate = DateTime.ParseExact(reader.GetString((int)CalibrationData.DatabaseColumns.ColCalibrationDate), dateFormat, CultureInfo.InvariantCulture); }
            if(reader.GetString((int)CalibrationData.DatabaseColumns.ColDueDate).Length > 0)
            { data.DueDate = DateTime.ParseExact(reader.GetString((int)CalibrationData.DatabaseColumns.ColDueDate), dateFormat, CultureInfo.InvariantCulture); }
            data.Procedure = reader.GetString((int)CalibrationData.DatabaseColumns.ColProcedure);
            data.StandardEquipment = reader.GetString((int)CalibrationData.DatabaseColumns.ColStandardEquipment);
            data.findings = JsonConvert.DeserializeObject<Findings>(reader.GetString((int)CalibrationData.DatabaseColumns.ColFindings));
            data.Remarks = reader.GetString((int)CalibrationData.DatabaseColumns.ColRemarks);
        }
    }
}
