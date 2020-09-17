using CalTools_WPF.ObjectClasses;
using IronXL.Xml.Spreadsheet;
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
        public readonly string version = "5.0.0";
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
        private List<CTItem> weekTodoItems = new List<CTItem>();
        private void ScanFolders()
        {
            List<CTTask> allTasks = database.GetAllTasks();
            List<CTItem> allItems = database.GetAllItems();
            List<TaskData> taskData = database.GetAllTaskData();
            DateTime defaultDate = new DateTime();
            string itemsFolder = $"{config.CalScansDir}\\Calibration Items\\";
            foreach (string folder in config.Folders)
            {
                string scanFolder = $"{itemsFolder}{folder}";
                if (Directory.Exists(scanFolder))
                {
                    foreach (string itemFolder in Directory.GetDirectories(scanFolder))
                    {
                        CTItem calItem = null;
                        bool newItem = false;
                        string itemSN = System.IO.Path.GetFileName(itemFolder);
                        foreach (CTItem item in allItems)
                        {
                            if (item.SerialNumber == itemSN) { calItem = item; break; }
                        }
                        if (calItem == null) { calItem = new CTItem(itemSN); newItem = true; }
                        DateTime? latest = CheckTasks(calItem.SerialNumber, itemFolder, ref taskData, ref allTasks);
                        if (latest == defaultDate) { latest = null; }
                        if (latest != calItem.LastCal | calItem.Directory != itemFolder)
                        {
                            calItem.Directory = itemFolder;
                            calItem.LastCal = latest;

                            if (calItem.LastCal != null)
                            { calItem.NextCal = calItem.LastCal.Value.AddMonths(calItem.Interval); }
                            if (newItem) { database.CreateItem(calItem.SerialNumber); }
                        }
                        if (latest != null)
                        {
                            if (calItem.NextCal != calItem.LastCal.Value.AddMonths(calItem.Interval))
                            { calItem.NextCal = calItem.LastCal.Value.AddMonths(calItem.Interval); }
                        }
                        else { if (calItem.NextCal != null) { calItem.NextCal = null; } }
                        if (calItem.NextCal != null)
                        {
                            if (((calItem.NextCal - DateTime.Today).Value.TotalDays < config.MarkCalDue) & calItem.Mandatory)
                            {
                                if (!calItem.TaskDue)
                                {
                                    database.UpdateColumn(calItem.SerialNumber, "TaskDue", "1");
                                }
                            }
                            else { if (calItem.TaskDue) { database.UpdateColumn(calItem.SerialNumber, "TaskDue", "0"); } }
                        }
                        else { if (calItem.TaskDue) { database.UpdateColumn(calItem.SerialNumber, "TaskDue", "0"); } }
                        if (calItem.ChangesMade) { database.SaveItem(calItem); }
                    }
                }
            }
        }
        private void CheckTasks(string sn, string folder, ref List<TaskData> taskData, ref List<CTTask> tasks)
        {
            List<CTTask> currentTasks = new List<CTTask>();
            foreach(CTTask task in tasks)
            {
                if(task.SerialNumber == sn) { currentTasks.Add(task); }
            }
            foreach (string taskFolder in Directory.GetDirectories(folder))
            {
                foreach (CTTask task in currentTasks)
                {
                    if (Path.GetFileName(taskFolder).Split("_")[0] == task.TaskID.ToString())
                    {
                        DateTime latestDate = new DateTime();
                        foreach (string filePath in Directory.GetFiles(taskFolder))
                        {
                            string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
                            bool snFound = false;
                            DateTime currentFileDate = new DateTime();
                            foreach (string split in file.Split("_"))
                            {
                                DateTime.TryParseExact(split, database.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out currentFileDate);
                                if(sn == split) { snFound = true; }
                            }
                            if (snFound & currentFileDate > latestDate) { latestDate = currentFileDate; }
                        }
                        if(latestDate > task.CompleteDate)
                        {
                            task.CompleteDate = latestDate;
                            if ((task.DueDate - DateTime.UtcNow).Value.Days < config.MarkCalDue) { task.Due = true; }
                            else { task.Due = false; }
                        }
                        break;
                    }
                }
            }
            //Test to make sure that a later date here will overwrite a date from the previous section.
            foreach (CTTask task in currentTasks)
            {
                DateTime? latestTaskDate = task.CompleteDate;
                foreach (TaskData data in taskData)
                { 
                    if (data.CompleteDate > latestTaskDate & data.TaskID == task.TaskID) 
                    { latestTaskDate = data.CompleteDate; } 
                }
                if(latestTaskDate > task.CompleteDate)
                {
                    task.CompleteDate = latestTaskDate;
                    if((task.DueDate - DateTime.UtcNow).Value.Days < config.MarkCalDue) { task.Due = true; }
                    else { task.Due = false; }
                }
                if (task.ChangesMade) { database.SaveTask(task); }
            }
        }
        //Gets all task data for an item and lists them by (date,location)
        private List<Dictionary<string, string>> ListTaskData(string taskID)
        {
            List<Dictionary<string, string>> taskDataList = new List<Dictionary<string, string>>();
            foreach (TaskData data in database.GetTaskData(taskID))
            {
                Dictionary<string, string> cal = new Dictionary<string, string>
                {
                    { "date", data.CompleteDate.Value.ToString(database.dateFormat) },
                    { "location", $"{config.DbName}, \"TaskData\", ID: {data.DataID}" },
                    { "id", data.DataID.ToString() }
                };
                taskDataList.Add(cal);
            }
            string sn = database.GetTask("TaskID",taskID).SerialNumber;
            //TODO: Add functionality for task sub folders
            foreach (string filePath in Directory.GetFiles(database.GetItem("SerialNumber", sn).Directory))
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
                        cal.Add("location", filePath); taskDataList.Add(cal);
                        cal.Add("id", "");
                        break; }
                }
            }
            return taskDataList;
        }
        private Dictionary<string, string> ParseFileName(string filePath)
        {
            Dictionary<string, string> fileInfo = new Dictionary<string, string>
            {
                { "Date", "" },
                { "SN", "" }
            };
            string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
            DateTime fileDate;
            foreach (string split in file.Split("_"))
            {
                if (DateTime.TryParseExact(split, database.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate))
                {
                    fileInfo["Date"] = split;
                }
                else if (database.GetItem("SerialNumber", split) != null)
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
            CTItem calItem;
            if (newFileName == "")
            {  fileInfo = ParseFileName(file); calItem = database.GetItem("SerialNumber", fileInfo["SN"]); }
            else { fileInfo = ParseFileName(newFileName); calItem = database.GetItem("SerialNumber", fileInfo["SN"]); }
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
            foreach (CTItem calItem in database.GetAllItems())
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
        private void UpdateListsSingle(CTItem calItem)
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

        private List<CTItem> ItemListFilter(string mode, string searchText)
        {
            List<CTItem> filteredItems = new List<CTItem>();
            var property = typeof(CTItem).GetProperty(mode);
            foreach (CTItem calItem in database.GetAllItems())
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
