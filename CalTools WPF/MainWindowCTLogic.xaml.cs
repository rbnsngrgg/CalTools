using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CalTools_WPF
{
    //Other main window code-behind logic that doesn't directly interact with the GUI elements.
    public partial class MainWindow : Window
    {
        public readonly string version = "4.0.0";
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

        private void ScanFolders()
        {
            string itemsFolder = $"{config.CalScansDir}\\Calibration Items\\";
            foreach (string folder in config.Folders)
            {
                string scanFolder = $"{itemsFolder}{folder}";
                if (Directory.Exists(scanFolder))
                {
                    foreach (string itemFolder in Directory.GetDirectories(scanFolder))
                    {
                        bool newItem = false;
                        string itemSN = System.IO.Path.GetFileName(itemFolder);
                        CalibrationItem calItem = database.GetCalItem("calibration_items", "serial_number", itemSN);
                        if (calItem == null) { calItem = new CalibrationItem(itemSN); newItem = true; }
                        calItem.Directory = itemFolder;
                        DateTime? latest = GetLatestCal(calItem.SerialNumber, calItem.Directory);
                        if (latest != null)
                        {
                            if (latest == new DateTime())
                            {
                                latest = null;
                            }
                        }
                        if (latest != calItem.LastCal | newItem | calItem.LastCal == null)
                        {
                            calItem.LastCal = latest;
                            if (calItem.LastCal != null)
                            { calItem.NextCal = calItem.LastCal.Value.AddMonths(calItem.Interval); }
                            database.SaveCalItem(calItem);
                        }
                    }
                }
            }
        }

        private DateTime? GetLatestCal(string sn, string folder)
        {
            DateTime? calDate = new DateTime();
            foreach (string filePath in Directory.GetFiles(folder))
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
                List<string> fileSplit = new List<string>();
                fileSplit.AddRange(file.Split("_"));
                DateTime fileDate;
                foreach (string split in fileSplit)
                {
                    if (DateTime.TryParseExact(split, database.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate))
                    {
                        if (fileDate > calDate) { calDate = fileDate; break; }
                    }
                }
            }
            foreach (CalibrationData data in database.GetCalData(sn))
            {
                if (data.CalibrationDate > calDate) { calDate = data.CalibrationDate;}
            }
            return calDate;
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
                { folder = $"{config.CalScansDir}\\Calibration Items\\{folderDialog.FolderSelectComboBox.Text}\\{sn}"; }
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

        private List<CalibrationItem> ItemListFilter(string mode,string searchText)
        {
            List<CalibrationItem> filteredItems = new List<CalibrationItem>();
            var property = typeof(CalibrationItem).GetProperty(mode);
            foreach(CalibrationItem calItem in database.GetAllCalItems())
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
