﻿using CalTools_WPF.ObjectClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CalTools_WPF
{
    //Other main window code-behind logic that doesn't directly interact with the GUI elements.
    public partial class MainWindow : Window
    {
        public readonly string version = "4.2.0";
        private CTDatabase database;
        private CTConfig config = new CTConfig();
        private Dictionary<string, string> searchModes = new Dictionary<string, string>() {
            { "Serial Number","SerialNumber"},
            {"Location","Location"},
            { "Calibration Vendor","CalVendor" },
            { "Manufacturer","Manufacturer" },
            { "Description","Description" },
            { "Calibration Due","CalDue" },
            { "Model","Model" },
            { "Has Comment","Comment" },
            { "Item Group","ItemGroup" },
            { "Action","VerifyOrCalibrate" },
            { "Standard Equipment","StandardEquipment"} };
        private List<string> manufacturers = new List<string>();
        private List<string> locations = new List<string>();
        private List<string> calVendors = new List<string>();
        private List<string> itemGroups = new List<string>();
        private List<string> standardEquipment = new List<string>();
        private List<CalibrationItem> weekTodoItems = new List<CalibrationItem>();
        private void ScanFolders()
        {
            List<CalibrationItem> allItems = database.GetAllCalItems();
            List<CalibrationData> calData = database.GetAllCalData();
            DateTime defaultDate = new DateTime();
            string itemsFolder = $"{config.CalScansDir}\\Calibration Items\\";
            foreach (string folder in config.Folders)
            {
                string scanFolder = $"{itemsFolder}{folder}";
                if (Directory.Exists(scanFolder))
                {
                    foreach (string itemFolder in Directory.GetDirectories(scanFolder))
                    {
                        CalibrationItem calItem = null;
                        bool newItem = false;
                        bool changesMade = false;
                        string itemSN = System.IO.Path.GetFileName(itemFolder);
                        foreach (CalibrationItem item in allItems)
                        {
                            if (item.SerialNumber == itemSN) { calItem = item; break; }
                        }
                        if (calItem == null) { calItem = new CalibrationItem(itemSN); newItem = true; }
                        DateTime? latest = GetLatestCal(calItem.SerialNumber, itemFolder, ref calData);
                        if (latest == defaultDate) { latest = null; }
                        if (latest != calItem.LastCal | calItem.Directory != itemFolder)
                        {
                            calItem.Directory = itemFolder;
                            calItem.LastCal = latest;

                            if (calItem.LastCal != null)
                            { calItem.NextCal = calItem.LastCal.Value.AddMonths(calItem.Interval); }
                            if (newItem) { database.CreateCalItem(calItem.SerialNumber); }
                            changesMade = true;
                        }
                        if (latest != null)
                        {
                            if (calItem.NextCal != calItem.LastCal.Value.AddMonths(calItem.Interval))
                            { calItem.NextCal = calItem.LastCal.Value.AddMonths(calItem.Interval); changesMade = true; }
                        }
                        else { if (calItem.NextCal != null) { calItem.NextCal = null; changesMade = true; } }
                        if (calItem.NextCal != null)
                        {
                            if (((calItem.NextCal - DateTime.Today).Value.TotalDays < config.MarkCalDue) & calItem.Mandatory)
                            {
                                if (!calItem.CalDue)
                                {
                                    database.UpdateColumn(calItem.SerialNumber, "caldue", "1");
                                }
                            }
                            else { if (calItem.CalDue) { database.UpdateColumn(calItem.SerialNumber, "caldue", "0"); } }
                        }
                        else { if (calItem.CalDue) { database.UpdateColumn(calItem.SerialNumber, "caldue", "0"); } }
                        if (changesMade) { database.SaveCalItem(calItem); }
                    }
                }
            }
        }
        private DateTime? GetLatestCal(string sn, string folder, ref List<CalibrationData> calData)
        {
            DateTime? calDate = new DateTime();
            foreach (string filePath in Directory.GetFiles(folder))
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
                bool snFound = false;
                DateTime fileDate = new DateTime();
                DateTime currentFileDate;
                foreach (string split in file.Split("_"))
                {
                    if (DateTime.TryParseExact(split, database.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out currentFileDate))
                    { if (currentFileDate > fileDate) { fileDate = currentFileDate; } }
                    else if (sn == split) { snFound = true; }
                }
                if (snFound & fileDate > calDate) { calDate = fileDate; }
            }
            foreach (CalibrationData data in calData)
            { if (data.CalibrationDate > calDate & data.SerialNumber == sn) { calDate = data.CalibrationDate; } }
            return calDate;
        }
        //Gets all calibration data for an item and lists them by (date,location)
        private List<Dictionary<string, string>> ListCalData(string sn)
        {
            List<Dictionary<string, string>> calDataList = new List<Dictionary<string, string>>();
            foreach (CalibrationData data in database.GetCalData(sn))
            {
                Dictionary<string, string> cal = new Dictionary<string, string>();
                cal.Add("date", data.CalibrationDate.Value.ToString(database.dateFormat));
                cal.Add("location", $"{config.DbName}, \"calibration_data\" Row {data.ID}");
                cal.Add("id", data.ID.ToString());
                calDataList.Add(cal);
            }
            foreach (string filePath in Directory.GetFiles(database.GetCalItem("calibration_items", "serial_number", sn).Directory))
            {
                Dictionary<string, string> cal = new Dictionary<string, string>();
                string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
                bool snFound = false;
                bool dateFound = false;
                DateTime fileDate = new DateTime();
                DateTime tryDate;
                foreach (string split in file.Split("_"))
                {
                    if (DateTime.TryParseExact(split, database.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tryDate))
                    { dateFound = true; fileDate = tryDate; }
                    else if (sn == split) { snFound = true; }
                    if (dateFound & snFound) 
                    { 
                        cal.Add("date", fileDate.ToString(database.dateFormat, CultureInfo.InvariantCulture));
                        cal.Add("location", filePath); calDataList.Add(cal);
                        cal.Add("id", "");
                        break; }
                }
            }
            return calDataList;
        }
        private Dictionary<string, string> ParseFileName(string filePath)
        {
            Dictionary<string, string> fileInfo = new Dictionary<string, string>();
            fileInfo.Add("Date", "");
            fileInfo.Add("SN", "");
            string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
            DateTime fileDate;
            foreach (string split in file.Split("_"))
            {
                if (DateTime.TryParseExact(split, database.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate))
                {
                    fileInfo["Date"] = split;
                }
                else if (database.GetCalItem("calibration_items", "serial_number", split) != null)
                {
                    fileInfo["SN"] = split;
                }
            }
            return fileInfo;
        }
        private void CheckReceiving()
        {
            List<string> files = new List<string>(Directory.GetFiles($"{config.CalListDir}\\receiving"));
            if (files.Count > 0)
            {
                foreach (string file in files)
                {
                    MoveToItemFolder(file);
                }
            }
        }
        private bool MoveToItemFolder(string file, string newFileName = "")
        {
            Dictionary<string, string> fileInfo;
            CalibrationItem calItem;
            if (newFileName == "")
            {  fileInfo = ParseFileName(file); calItem = database.GetCalItem("calibration_items", "serial_number", fileInfo["SN"]); }
            else { fileInfo = ParseFileName(newFileName); calItem = database.GetCalItem("calibration_items", "serial_number", fileInfo["SN"]); }
            if (calItem != null)
            {
                if (Directory.Exists(calItem.Directory))
                {
                    if (newFileName == "")
                    { File.Move(file, $"{calItem.Directory}\\{System.IO.Path.GetFileName(file)}"); }
                    else
                    {
                        try { File.Move(file, $"{calItem.Directory}\\{newFileName}"); }
                        catch (System.IO.IOException) { MessageBox.Show($"The file \"{calItem.Directory}\\{newFileName}\" already exists", "File Already Exists", MessageBoxButton.OK, MessageBoxImage.Error); }
                        catch (System.Exception ex) {MessageBox.Show($"{ex.Message}","Error",MessageBoxButton.OK,MessageBoxImage.Error); }
                    }
                    return true;
                }
            }
            return false;
        }

        //Create new xaml window for selecting a folder from those listed in the config file.
        private string GetNewItemFolder(string sn)
        {
            string folder = "";
            NewItemFolderSelect folderDialog = new NewItemFolderSelect();
            foreach (string configFolder in config.Folders)
            {
                ComboBoxItem boxItem = new ComboBoxItem();
                boxItem.Content = configFolder;
                folderDialog.FolderSelectComboBox.Items.Add(boxItem);
            }
            if (folderDialog.ShowDialog() == true)
            {
                //Check that the folder from the config exists before the new item folder is allowed to be created.
                if (Directory.Exists($"{config.CalScansDir}\\Calibration Items\\{folderDialog.FolderSelectComboBox.Text}"))
                { folder = $"{config.CalScansDir}\\Calibration Items\\{folderDialog.FolderSelectComboBox.Text}\\{sn}"; Directory.CreateDirectory(folder); }
            }
            return folder;
        }

        //Get lists to pre-populate combo-boxes
        private void UpdateLists()
        {
            manufacturers.Clear();
            calVendors.Clear();
            locations.Clear();
            itemGroups.Clear();
            standardEquipment.Clear();
            foreach (CalibrationItem calItem in database.GetAllCalItems())
            {
                if (!manufacturers.Contains(calItem.Manufacturer)) { manufacturers.Add(calItem.Manufacturer); }
                if (!calVendors.Contains(calItem.CalVendor)) { calVendors.Add(calItem.CalVendor); }
                if (!locations.Contains(calItem.Location)) { locations.Add(calItem.Location); }
                if (!itemGroups.Contains(calItem.ItemGroup)) { itemGroups.Add(calItem.ItemGroup); }
                if (calItem.StandardEquipment & !standardEquipment.Contains(calItem.SerialNumber)) { standardEquipment.Add(calItem.SerialNumber); }
            }
            manufacturers.Sort();
            calVendors.Sort();
            locations.Sort();
            itemGroups.Sort();
            standardEquipment.Add("");
            standardEquipment.Sort();
        }
        //Single-item list update that doesn't require DB query
        private void UpdateListsSingle(CalibrationItem calItem)
        {
            if (!manufacturers.Contains(calItem.Manufacturer)) { manufacturers.Add(calItem.Manufacturer); }
            if (!calVendors.Contains(calItem.CalVendor)) { calVendors.Add(calItem.CalVendor); }
            if (!locations.Contains(calItem.Location)) { locations.Add(calItem.Location); }
            if (!itemGroups.Contains(calItem.ItemGroup)) { itemGroups.Add(calItem.ItemGroup); }
            if (calItem.StandardEquipment & !standardEquipment.Contains(calItem.SerialNumber)) { standardEquipment.Add(calItem.SerialNumber); }
            manufacturers.Sort();
            calVendors.Sort();
            locations.Sort();
            itemGroups.Sort();
            standardEquipment.Sort();
        }

        private List<CalibrationItem> ItemListFilter(string mode, string searchText)
        {
            List<CalibrationItem> filteredItems = new List<CalibrationItem>();
            var property = typeof(CalibrationItem).GetProperty(mode);
            foreach (CalibrationItem calItem in database.GetAllCalItems())
            {
                if (mode == "CalDue") { if ((bool)property.GetValue(calItem) == true) { filteredItems.Add(calItem); } }
                else if (mode == "Comment") { if (property.GetValue(calItem).ToString().Length > 0) { filteredItems.Add(calItem); } }
                else if (mode == "StandardEquipment") { if ((bool)property.GetValue(calItem) == true) { filteredItems.Add(calItem); } }
                else if (property.GetValue(calItem).ToString().ToLower().Contains(searchText.ToLower()))
                {
                    filteredItems.Add(calItem);
                }
            }
            return filteredItems;
        }
    }
}