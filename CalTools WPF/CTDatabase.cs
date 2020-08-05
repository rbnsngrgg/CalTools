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
            //conn.ConnectionString = $"Data Source={DbPath}";
        }
        public bool Connect()
        {
            conn.Open();
            return true;
        }
        public bool Disconnect()
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

        public void Execute(string com)
        {
            SqliteCommand command = new SqliteCommand(com, conn);
            reader = command.ExecuteReader();
        }

        public List<CalibrationItem> GetAllItems(string table)
        {
            List<CalibrationItem> allItems = new List<CalibrationItem>();
            string command = $"SELECT * FROM {table}";
            Connect();
            Execute(command);
            while(reader.Read())
            {
                CalibrationItem item = new CalibrationItem(reader.GetString(0));
                item.Location = reader.GetString(1);
                item.Interval = reader.GetInt32(2);
                item.CalVendor = reader.GetString(3);
                item.Manufacturer = reader.GetString(4);
                if (reader.GetString(5).Length > 0) { item.LastCal = reader.GetDateTime(5); }
                if (reader.GetString(6).Length > 0) { item.NextCal = reader.GetDateTime(6); }
                item.Mandatory = reader.GetString(7) == "1"? true : false;
                item.Directory = reader.GetString(8);
                item.Description = reader.GetString(9);
                item.InService = reader.GetString(10) == "1" ? true : false;
                if (reader.GetString(11).Length > 0) { item.InServiceDate = reader.GetDateTime(11); }
                if (reader.GetString(12).Length > 0) { item.OutOfServiceDate = reader.GetDateTime(12); }
                item.CalDue = reader.GetString(13) == "1" ? true : false;
                item.Model = reader.GetString(14);
                item.Comment = reader.GetString(15);
                if(reader.GetString(16).Length > 0) { item.TimeStamp = DateTime.ParseExact(reader.GetString(16), "yyyy-MM-dd-HH-mm-ss-ffffff",CultureInfo.InvariantCulture); }
                item.ItemGroup = reader.GetString(17);
                item.VerifyOrCalibrate = reader.GetString(18);

                allItems.Add(item);
            }
            return allItems;
        }
    }
}
