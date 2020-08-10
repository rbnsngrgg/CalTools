using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.Diagnostics;
using System.Globalization;

namespace CalTools_WPF
{
    class CTDatabase
    {
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
                Debug.WriteLine(returnItem.Directory);
                return returnItem;
            }
            return null;
        }
#nullable disable
        //Private members
        private bool CreateTables()
        {
            try
            {

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
            item.Location = reader.GetString(1);
            item.Interval = reader.GetInt32(2);
            item.CalVendor = reader.GetString(3);
            item.Manufacturer = reader.GetString(4);
            if (reader.GetString(5).Length > 0) { item.LastCal = DateTime.ParseExact(reader.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            if (reader.GetString(6).Length > 0) { item.NextCal = DateTime.ParseExact(reader.GetString(6), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            item.Mandatory = reader.GetString(7) == "1" ? true : false;
            item.Directory = reader.GetString(8);
            item.Description = reader.GetString(9);
            item.InService = reader.GetString(10) == "1" ? true : false;
            if (reader.GetString(11).Length > 0) { item.InServiceDate = DateTime.ParseExact(reader.GetString(11), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            if (reader.GetString(12).Length > 0) { item.OutOfServiceDate = DateTime.ParseExact(reader.GetString(12), "yyyy-MM-dd", CultureInfo.InvariantCulture); }
            item.CalDue = reader.GetString(13) == "1" ? true : false;
            item.Model = reader.GetString(14);
            item.Comment = reader.GetString(15);
            if (reader.GetString(16).Length > 0) { item.TimeStamp = DateTime.ParseExact(reader.GetString(16), "yyyy-MM-dd-HH-mm-ss-ffffff", CultureInfo.InvariantCulture); }
            item.ItemGroup = reader.GetString(17);
            item.VerifyOrCalibrate = reader.GetString(18);
        }
    }
}
