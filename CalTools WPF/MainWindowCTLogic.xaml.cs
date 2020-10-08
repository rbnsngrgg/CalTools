using CalTools_WPF.ObjectClasses;
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
            { "Serial Number","SerialNumber" },
            { "Location","Location" },
            { "Service Vendor","Vendor" },
            { "Manufacturer","Manufacturer" },
            { "Description","Description" },
            { "Action Due","Due" },
            { "Model","Model" },
            { "Has Comment","Comment" },
            { "Item Group","ItemGroup" },
            { "Action","ActionType" },
            { "Standard Equipment","StandardEquipment"} };
        private List<string> manufacturers = new List<string>();
        private List<string> locations = new List<string>();
        private List<string> serviceVendors = new List<string>();
        private List<string> itemGroups = new List<string>();
        private List<string> standardEquipment = new List<string>();
        private List<Dictionary<string, string>> weekTodoItems = new List<Dictionary<string, string>>();
        private List<CTTask> detailsTasks = new List<CTTask>();
        private void ScanFolders()
        {
            List<CTTask> allTasks = database.GetAllTasks();
            List<CTItem> allItems = database.GetAllItems();
            List<TaskData> taskData = database.GetAllTaskData();
            string itemsFolder = $"{config.ItemScansDir}\\";
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
                        if (calItem.Directory != itemFolder)
                        {
                            calItem.Directory = itemFolder;
                            if (newItem) { database.CreateItem(calItem.SerialNumber); }
                        }
                        CheckTasks(calItem.SerialNumber, itemFolder, ref taskData, ref allTasks);
                        if (calItem.ChangesMade) { database.SaveItem(calItem); }
                    }
                }
            }
        }
        //Checks completion dates and due dates on all tasks.
        private void CheckTasks(string sn, string folder, ref List<TaskData> taskData, ref List<CTTask> tasks)
        {
            foreach (string taskFolder in Directory.GetDirectories(folder))
            {
                string folderTaskID = Path.GetFileName(taskFolder).Split("_")[0];
                foreach (CTTask task in tasks)
                {
                    if (folderTaskID == task.TaskID.ToString() & Path.GetFileName(folder) == task.SerialNumber)
                    {
                        task.TaskDirectory = taskFolder;
                        task.CheckDates(taskFolder, database.GetTaskData(task.TaskID.ToString()));
                    }
                }
            }
            foreach (CTTask task in tasks)
            {
                if (task.ChangesMade) { database.SaveTask(task); task.ChangesMade = false; }
            }
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
            string receivingFolder = $"{config.ListDir}\\receiving";
            if (!Directory.Exists(receivingFolder) & Directory.Exists(config.ListDir)) { Directory.CreateDirectory(receivingFolder); }
            List<string> files = new List<string>(Directory.GetFiles(receivingFolder));
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
            { fileInfo = ParseFileName(file); calItem = database.GetItem("SerialNumber", fileInfo["SN"]); }
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
                        catch (System.Exception ex) { MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                    }
                    return true;
                }
            }
            return false;
        }
        //Take original path, destination (without file name), and an optional new file name to move the file.
        private bool MoveToTaskFolder(string filePath, string destination, string newFileName = "")
        {
            Debug.WriteLine("MoveToTaskFolder called");
            bool isFolder = false;
            if (Directory.Exists(filePath)) { isFolder = true; }
            if ((File.Exists(filePath) | isFolder) & Directory.Exists(destination))
            {
                Debug.WriteLine("Folder exists, File exists");
                string newFilePath;
                if (newFileName == "") { newFilePath = Path.Combine(destination, Path.GetFileName(filePath)); }
                else { newFilePath = Path.Combine(destination, newFileName); }
                if (isFolder) { File.Copy(filePath, newFilePath);  }
                else { File.Move(filePath, newFilePath, true); }
                return true;
            }
            else { return false; }
        }
        //Create new xaml window for selecting a folder from those listed in the config file.
        private string GetNewItemFolder(string sn)
        {
            string folder = "";
            NewItemFolderSelect folderDialog = new NewItemFolderSelect();
            foreach (string configFolder in config.Folders)
            {
                ComboBoxItem boxItem = new ComboBoxItem{ Content = configFolder };
                folderDialog.FolderSelectComboBox.Items.Add(boxItem);
            }
            if (folderDialog.ShowDialog() == true)
            {
                //Check that the folder from the config exists before the new item folder is allowed to be created.
                if (Directory.Exists($"{config.ItemScansDir}\\{folderDialog.FolderSelectComboBox.Text}"))
                { folder = $"{config.ItemScansDir}\\{folderDialog.FolderSelectComboBox.Text}\\{sn}"; Directory.CreateDirectory(folder); }
            }
            return folder;
        }

        //Get lists to pre-populate combo-boxes
        private void UpdateLists()
        {
            manufacturers.Clear();
            serviceVendors.Clear();
            locations.Clear();
            itemGroups.Clear();
            standardEquipment.Clear();
            foreach (CTItem calItem in database.GetAllItems())
            {
                if (!manufacturers.Contains(calItem.Manufacturer)) { manufacturers.Add(calItem.Manufacturer); }
                if (!locations.Contains(calItem.Location)) { locations.Add(calItem.Location); }
                if (!itemGroups.Contains(calItem.ItemGroup)) { itemGroups.Add(calItem.ItemGroup); }
                if (calItem.StandardEquipment & !standardEquipment.Contains(calItem.SerialNumber)) { standardEquipment.Add(calItem.SerialNumber); }
            }
            foreach (CTTask task in database.GetAllTasks())
            { if (!serviceVendors.Contains(task.ServiceVendor)) { serviceVendors.Add(task.ServiceVendor); } }
            manufacturers.Sort();
            serviceVendors.Sort();
            locations.Sort();
            itemGroups.Sort();
            standardEquipment.Add("");
            standardEquipment.Sort();
        }
        //Single-item list update that doesn't require CTItem DB query
        private void UpdateListsSingle(CTItem calItem)
        {
            if (!manufacturers.Contains(calItem.Manufacturer)) { manufacturers.Add(calItem.Manufacturer); }
            if (!locations.Contains(calItem.Location)) { locations.Add(calItem.Location); }
            if (!itemGroups.Contains(calItem.ItemGroup)) { itemGroups.Add(calItem.ItemGroup); }
            if (calItem.StandardEquipment & !standardEquipment.Contains(calItem.SerialNumber)) { standardEquipment.Add(calItem.SerialNumber); }
            foreach (CTTask task in database.GetTasks("SerialNumber", calItem.SerialNumber))
            { if (!serviceVendors.Contains(task.ServiceVendor)) { serviceVendors.Add(task.ServiceVendor); } }
            manufacturers.Sort();
            serviceVendors.Sort();
            locations.Sort();
            itemGroups.Sort();
            standardEquipment.Sort();
        }
        //Filters items when search is used
        private List<CTItem> ItemListFilter(string mode, string searchText)
        {
            List<CTItem> filteredItems = new List<CTItem>();
            List<CTTask> allTasks = database.GetAllTasks();
            var property = typeof(CTItem).GetProperty(mode);
            foreach (CTItem item in database.GetAllItems())
            {
                if (mode == "ActionType")
                {
                    foreach (CTTask task in allTasks)
                    {
                        if (task.ActionType.Contains(searchText.ToUpper()) & task.SerialNumber == item.SerialNumber)
                        {
                            filteredItems.Add(item);
                            break;
                        }
                    }
                }
                else if (mode == "Comment") { if (property.GetValue(item).ToString().Length > 0) { filteredItems.Add(item); } }
                else if (mode == "StandardEquipment") { if ((bool)property.GetValue(item) == true) { filteredItems.Add(item); } }
                else if (mode == "Due")
                {
                    foreach (CTTask task in allTasks)
                    {
                        if (task.SerialNumber == item.SerialNumber & task.CheckDue(config.MarkDueDays)) { filteredItems.Add(item); break; }
                    }
                }
                else if (mode == "Vendor")
                {
                    foreach (CTTask task in allTasks)
                    {
                        if (task.SerialNumber == item.SerialNumber & task.ServiceVendor.ToLower().Contains(searchText.ToLower()))
                        {
                            filteredItems.Add(item);
                            break;
                        }
                    }
                }
                else if (property.GetValue(item).ToString().ToLower().Contains(searchText.ToLower()))
                {
                    filteredItems.Add(item);
                }
            }
            return filteredItems;
        }
        //Generate list for calendar item source
        private List<Dictionary<string, string>> CreateCalendarList(bool mandatoryOnly)
        {
            List<Dictionary<string, string>> compositeList = new List<Dictionary<string, string>>();
            List<CTTask> allTasks = database.GetAllTasks();
            List<CTItem> allItems = database.GetAllItems();
            foreach (CTTask task in allTasks)
            {
                if (ItemCalendar.SelectedDate != null & task.DueDate != null)
                {
                    if ((task.Mandatory & mandatoryOnly) | (!mandatoryOnly))
                    {
                        foreach (CTItem item in allItems)
                        {
                            if (item.InService & item.SerialNumber == task.SerialNumber)
                            {
                                if (task.CheckDue(config.MarkDueDays))
                                {
                                    Dictionary<string, string> compositeItem = new Dictionary<string, string>
                                    {
                                        {"SerialNumber",item.SerialNumber},
                                        {"Model", item.Model},
                                        {"TaskID", task.TaskID.ToString()},
                                        {"TaskTitle",$"({task.TaskID}){task.TaskTitle}" },
                                        {"Description",item.Description},
                                        {"Location",item.Location},
                                        {"ServiceVendor",task.ServiceVendor},
                                        {"DueDateString",task.DueDateString}
                                    };
                                    compositeList.Add(compositeItem);
                                }
                            }
                        }
                    }
                }
            }
            return compositeList;
        }
    }
}
