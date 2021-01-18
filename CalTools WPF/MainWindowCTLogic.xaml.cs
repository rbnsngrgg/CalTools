﻿using CalTools_WPF.ObjectClasses;
using CalTools_WPF.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
//using System.Diagnostics;

namespace CalTools_WPF
{
    //Main window code-behind logic outside of event handlers
    public partial class MainWindow : Window
    {
        public readonly string version = "5.1.0";
        private CTDatabase database;
        private CTConfig config = new CTConfig();
        private readonly Dictionary<string, string> searchModes = new Dictionary<string, string>() {
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

        private void AddItemsToList(List<CTItem> items)
        {
            items.Sort((x, y) => x.SerialNumber.CompareTo(y.SerialNumber));
            CalibrationItemTree.Items.Clear();
            foreach (string folder in config.Folders)
            {
                TreeViewItem group = new TreeViewItem();
                group.Header = folder;
                foreach (var item in items)
                {
                    if (item.Directory.Contains(folder))
                    {
                        TreeViewItem treeItem = new TreeViewItem();
                        treeItem.Header = item.SerialNumber;
                        group.Items.Add(treeItem);
                    }
                }
                CalibrationItemTree.Items.Add(group);
            }
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
        private void CheckTasks(string folder, ref List<CTTask> tasks, ref List<TaskData> taskData)
        {
            foreach (string taskFolder in Directory.GetDirectories(folder))
            {
                string folderTaskID = Path.GetFileName(taskFolder).Split("_")[0];
                foreach (CTTask task in tasks)
                {
                    List<TaskData> currentData = new List<TaskData>();
                    foreach (TaskData data in taskData)
                    {
                        if (data.TaskID == task.TaskID) { currentData.Add(data); }
                    }
                    if (folderTaskID == task.TaskID.ToString() & Path.GetFileName(folder) == task.SerialNumber)
                    {
                        task.TaskDirectory = taskFolder;
                        task.CheckDates(taskFolder, currentData);
                    }
                }
            }
            SaveTaskChanges(ref tasks);
        }
        private List<Dictionary<string, string>> CreateCalendarList(bool mandatoryOnly, DateTime calendarDate)
        {
            List<Dictionary<string, string>> compositeList = new List<Dictionary<string, string>>();
            List<CTTask> allTasks = database.GetAllTasks();
            List<CTItem> allItems = database.GetAllItems();
            foreach (CTTask task in allTasks)
            {
                if (ItemCalendar.SelectedDate != null)
                {
                    if ((task.Mandatory & mandatoryOnly) | (!mandatoryOnly))
                    {
                        foreach (CTItem item in allItems)
                        {
                            if (item.InService & item.SerialNumber == task.SerialNumber)
                            {
                                if (task.IsTaskDue(config.MarkDueDays, calendarDate))
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
        private void CreateTaskFolders(ref List<CTTask> tasks)
        {
            foreach (CTTask task in tasks)
            {
                if (task.TaskDirectory == "")
                {
                    CTItem taskItem = database.GetItemFromTask(task);
                    if (Directory.Exists(taskItem.Directory))
                    {
                        string newPath = Path.Combine(taskItem.Directory, $"{task.TaskID}_{task.TaskTitle}");
                        Directory.CreateDirectory(newPath);
                        task.TaskDirectory = newPath;
                    }
                }
            }
        }
        private void DetailsEditToggle()
        {
            bool enable = !DetailsSN.IsEnabled;
            foreach (UIElement child in DetailsGrid.Children)
            {
                child.IsEnabled = enable;
            }
            DetailsComments.IsEnabled = enable;
            DetailsTasksTable.IsEnabled = true;
            DetailsTasksTable.IsReadOnly = !enable;
            AddTaskButton.IsEnabled = enable;
            RemoveTaskButton.IsEnabled = enable;
        }
        private void ExpandTreeItems()
        {
            foreach (TreeViewItem group in CalibrationItemTree.Items)
            {
                group.IsExpanded = true;
            }
        }
        private string FindItemDirectory(string serialNumber)
        {
            //Iterate through folders in the Item Scans directory
            foreach (string directoryFolder in Directory.GetDirectories(config.ItemScansDir))
            {
                //Match folder with one of the folders specified in the config
                foreach (string configFolder in config.Folders)
                {
                    if (directoryFolder.Contains(configFolder))
                    {
                        //Search for a folder that matches the specified item.
                        foreach (string itemFolder in Directory.GetDirectories(directoryFolder))
                        {
                            if (itemFolder == serialNumber) { return itemFolder; }
                        }
                    }
                }
            }
            return "";
        }
        private string GetNewItemFolder(string sn)
        {
            string folder = "";
            NewItemFolderSelect folderDialog = new NewItemFolderSelect();
            foreach (string configFolder in config.Folders)
            {
                ComboBoxItem boxItem = new ComboBoxItem { Content = configFolder };
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
        private void SwapItems()
        {
            string selectedSN = ((TreeViewItem)CalibrationItemTree.SelectedItem).Header.ToString();
            List<string> itemsInGroup = new List<string>();
            CTItem selectedItem = database.GetItem("SerialNumber", selectedSN);
            foreach(CTItem item in database.GetAllItems())
            {
                if(item.ItemGroup == selectedItem.ItemGroup)
                {
                    itemsInGroup.Add(item.SerialNumber);
                }
            }
            ReplaceItemSelection selectionDialog = new ReplaceItemSelection();
            selectionDialog.ReplaceSelectComboBox.ItemsSource = itemsInGroup;
            if(selectionDialog.ShowDialog() == true)
            {
                CTItem newItem1 = database.GetItem("SerialNumber", selectedItem.SerialNumber);
                CTItem newItem2 = database.GetItem("SerialNumber", selectionDialog.ReplaceSelectComboBox.SelectedItem.ToString());

                newItem1.Location = newItem2.Location;
                newItem1.InService = newItem2.InService;

                newItem2.Location = selectedItem.Location;
                newItem2.InService = selectedItem.InService;

                database.SaveItem(newItem1);
                database.SaveItem(newItem2);
            }    
        }
        private void GoToItem(string sn)
        {
            foreach (TreeViewItem item in CalibrationItemTree.Items)
            {
                foreach (TreeViewItem subItem in item.Items)
                {
                    if ((string)subItem.Header == sn)
                    {
                        item.IsExpanded = true;
                        subItem.IsSelected = true;
                        subItem.BringIntoView();
                        return;
                    }
                }
                item.IsExpanded = false;
            }
        }
        private void HighlightNonExistent()
        {
            List<string> nonExistent = new List<string>();
            foreach (CTItem calItem in database.GetAllItems())
            {
                if (!Directory.Exists(calItem.Directory))
                {
                    nonExistent.Add(calItem.SerialNumber);
                }
            }
            foreach (TreeViewItem item in CalibrationItemTree.Items)
            {
                foreach (TreeViewItem subItem in item.Items)
                {
                    if (nonExistent.Contains((string)subItem.Header)) { subItem.Foreground = Brushes.Red; subItem.ToolTip = "Missing folder"; }
                    else { subItem.Foreground = Brushes.Black; subItem.ToolTip = null; }
                }
            }
        }
        private bool IsItemSelected()
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if (selectedItem != null)
            {
                if (!config.Folders.Contains(selectedItem.Header.ToString()))
                { return true; }
            }
            return false;
        }
        private bool IsTaskSelected()
        {
            if (DetailsTasksTable.SelectedItem == null) { return false; }
            else { return true; }
        }
        private List<CTItem> ItemListFilter(string mode, string searchText) //Filters items when search is used
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
                        if (task.SerialNumber == item.SerialNumber & task.IsTaskDue(config.MarkDueDays, DateTime.UtcNow)) { filteredItems.Add(item); break; }
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
        private void UpdateItemList(bool single = false)
        {
            if (!database.tablesExist) { return; }
            CheckReceiving();
            string currentItem = SelectedSN();
            if (single) { ScanFoldersSingle(database.GetItem("SerialNumber", currentItem)); }
            else { ScanFolders(); }
            if (SearchBox.Text.Length != 0)
            {
                AddItemsToList(ItemListFilter(searchModes[SearchOptions.SelectedItem.ToString()], SearchBox.Text));
                ExpandTreeItems();
            }
            else { AddItemsToList(database.GetAllItems()); }
            UpdateLists();
            GoToItem(currentItem);
        }
        private void UpdateDetails(CTItem item)
        {
            if (item != null)
            {
                DetailsSN.Text = item.SerialNumber;
                DetailsModel.Text = item.Model;
                DetailsDescription.Text = item.Description;
                DetailsLocation.Text = item.Location;
                DetailsManufacturer.Text = item.Manufacturer;
                if (item.InServiceDate != null) { DetailsOperationDate.Text = item.InServiceDate.Value.ToString("yyyy-MM-dd"); } else { DetailsOperationDate.Clear(); }
                DetailsInOperation.IsChecked = item.InService;
                DetailsItemGroup.Text = item.ItemGroup;
                DetailsComments.Text = item.Comment;
                DetailsStandardBox.IsChecked = item.StandardEquipment;
                DetailsCertificateNum.Text = item.CertificateNumber;
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
        private bool MoveToTaskFolder(string filePath, string destination, string newFileName = "")
        {
            if ((File.Exists(filePath)) & Directory.Exists(destination))
            {
                string newFilePath;
                if (newFileName == "") { newFilePath = Path.Combine(destination, Path.GetFileName(filePath)); }
                else { newFilePath = Path.Combine(destination, newFileName); }
                File.Move(filePath, newFilePath, true);
                return true;
            }
            else { return false; }
        }
        private void NewReport(CTTask task)
        {
            CalDataEntry dataEntry = new CalDataEntry();
            dataEntry.SerialNumberBox.Text = task.SerialNumber;
            dataEntry.MaintenanceSerialNumberBox.Text = task.SerialNumber;
            dataEntry.DateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
            dataEntry.MaintenanceDateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
            dataEntry.ProcedureBox.ItemsSource = config.Procedures;
            dataEntry.MaintenanceProcedureBox.ItemsSource = config.Procedures;
            dataEntry.EquipmentBox.ItemsSource = standardEquipment;
            dataEntry.MaintenanceEquipmentBox.ItemsSource = standardEquipment;
            dataEntry.TaskBox.Text = $"({task.TaskID}) {task.TaskTitle}";
            dataEntry.MaintenanceTaskBox.Text = dataEntry.TaskBox.Text;
            dataEntry.data.TaskID = task.TaskID;
            if (task.ActionType == "MAINTENANCE")
            { dataEntry.MaintenanceSelection.IsSelected = true; }
            if (config.Procedures.Count > 0) { dataEntry.ProcedureBox.SelectedIndex = 0; }
            if (standardEquipment.Count > 0) { dataEntry.EquipmentBox.SelectedIndex = 0; }
            dataEntry.findings.parameters.Add(new Param($"Parameter {dataEntry.findings.parameters.Count + 1}"));
            if (dataEntry.ShowDialog() == true)
            {
                try
                {
                    dataEntry.data.StandardEquipment = JsonConvert.SerializeObject(database.GetItem("SerialNumber", dataEntry.EquipmentBox.Text));
                }
                catch
                { MessageBox.Show($"Invalid \"Standard Equipment\" entry.", "Invalid Entry", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                database.SaveTaskData(dataEntry.data);
            }
            SaveTasksTable();
            CTItem item = database.GetItemFromTask(task);
            List<CTTask> tasks = database.GetTasks("SerialNumber", item.SerialNumber);
            List<TaskData> taskData = database.GetAllTaskData();
            CheckTasks(item.Directory, ref tasks, ref taskData);
            UpdateTasksTable();
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
        private void SaveItem()
        {
            if (DetailsSN.Text.Length > 0)
            {
                CTItem item;
                item = database.GetItem("SerialNumber", DetailsSN.Text);
                MessageBoxResult result = MessageBoxResult.None;
                if (item == null)
                {
                    item = new CTItem(DetailsSN.Text);
                    result = MessageBox.Show($"Item SN: {DetailsSN.Text} does not exist in the database. Create this item?", "Item Not Found", MessageBoxButton.YesNo, MessageBoxImage.Information);
                }
                item.Model = DetailsModel.Text;
                item.Description = DetailsDescription.Text;
                item.Location = DetailsLocation.Text;
                item.Manufacturer = DetailsManufacturer.Text;
                DateTime inservice;
                if (DateTime.TryParseExact(DetailsOperationDate.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out inservice))
                { item.InServiceDate = inservice; };
                item.InService = DetailsInOperation.IsChecked == true;
                item.ItemGroup = DetailsItemGroup.Text;
                item.Comment = DetailsComments.Text;
                if ((bool)DetailsStandardBox.IsChecked)
                {
                    item.StandardEquipment = true;
                    if (DetailsCertificateNum.Text.Length == 0)
                    {
                        MessageBox.Show("A certificate number is required for items marked as Standard Equipment.", "Certificate Number", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
                else { item.StandardEquipment = false; }
                item.CertificateNumber = DetailsCertificateNum.Text;
                if (result == MessageBoxResult.Yes | result == MessageBoxResult.None)
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        string folder = GetNewItemFolder(item.SerialNumber);
                        if (folder != "")
                        {
                            item.Directory = folder;
                            Directory.CreateDirectory(folder);
                            database.SaveItem(item);
                            UpdateDetails(database.GetItem("SerialNumber", item.SerialNumber));
                            UpdateItemList();
                        }
                    }
                    else
                    {
                        database.SaveItem(item);
                        SaveItemButton.Visibility = Visibility.Collapsed;
                        DetailsEditToggle();
                        EditItemButton.Visibility = Visibility.Visible;
                    }
                }
                UpdateListsSingle(item);
            }
        }
        private void SaveTaskChanges(ref List<CTTask> tasks)
        {
            foreach (CTTask task in tasks)
            {
                if (task.ChangesMade) { database.SaveTask(task); task.ChangesMade = false; }
            }
        }
        private void SaveTasksTable()
        {
            foreach (CTTask task in DetailsTasksTable.Items)
            {
                string itemFolder = Directory.GetParent(task.TaskDirectory).FullName;
                string currentFolder = Path.Combine(itemFolder, Path.GetFileName(task.TaskDirectory));
                string newFolder = Path.Combine(itemFolder, $"{task.TaskID}_{task.TaskTitle}");

                if (currentFolder != newFolder) { Directory.Move(currentFolder, newFolder); task.TaskDirectory = newFolder; }
                if (task.ChangesMade) { database.SaveTask(task); }
            }
        }
        private void ScanFoldersSingle(CTItem item)
        {
            if (item == null) { return; }
            List<CTTask> itemTasks = database.GetTasks("SerialNumber", item.SerialNumber);
            List<TaskData> taskData = database.GetAllTaskData();
            if (!Directory.Exists(item.Directory)) { item.Directory = FindItemDirectory(item.SerialNumber); }
            CheckTasks(item.Directory, ref itemTasks, ref taskData);
            if (item.ChangesMade) { database.SaveItem(item); }
        }
        private void ScanFolders()
        {
            List<CTTask> allTasks = database.GetAllTasks();
            List<CTItem> allItems = database.GetAllItems();
            List<TaskData> taskData = database.GetAllTaskData();

            foreach (string folder in config.Folders)
            {
                string scanFolder = Path.Combine(config.ItemScansDir, folder);
                if (Directory.Exists(scanFolder))
                {
                    foreach (string itemFolder in Directory.GetDirectories(scanFolder))
                    {
                        CTItem calItem = null;
                        bool newItem = false;
                        string itemSN = System.IO.Path.GetFileName(itemFolder);
                        foreach (CTItem item in allItems)                                       //Match folder to CTItem in list
                        {
                            if (item.SerialNumber == itemSN) { calItem = item; break; }
                        }
                        if (calItem == null) { calItem = new CTItem(itemSN); newItem = true; }  //If existing item wasn't found, create new
                        if (calItem.Directory != itemFolder)                                    //Check if directory is valid. Set to the found folder if not
                        {
                            calItem.Directory = itemFolder;
                            if (newItem) { database.CreateItem(calItem.SerialNumber); }
                        }
                        CheckTasks(itemFolder, ref allTasks, ref taskData);
                        if (calItem.ChangesMade) { database.SaveItem(calItem); }
                    }
                }
            }
            CreateTaskFolders(ref allTasks);
        }
        private void SearchItems()
        {
            SearchBox.Clear();
            string selection = SearchOptions.SelectedItem.ToString();
            if (selection == "Calibration Due" | selection == "Has Comment" | selection == "Standard Equipment" | selection == "Action Due")
            {
                AddItemsToList(ItemListFilter(searchModes[selection], ""));
                SearchBox.IsEnabled = false;
                ExpandTreeItems();
            }
            else { SearchBox.IsEnabled = true; AddItemsToList(database.GetAllItems()); }
        }
        private string SelectedSN()
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if (!IsItemSelected()) { return ""; }
            string sn = selectedItem.Header.ToString();
            return sn;
        }
        private void ToggleView()
        {
            if (MainViewGrid.Visibility == Visibility.Visible)
            {
                MainViewGrid.Visibility = Visibility.Collapsed;
                CalendarViewGrid.Visibility = Visibility.Visible;
                UpdateItemsTable();
            }
            else
            {
                CalendarViewGrid.Visibility = Visibility.Collapsed;
                MainViewGrid.Visibility = Visibility.Visible;
            }
        }
        private void UpdateItemsTable()
        {
            weekTodoItems.Clear();
            todoTable.Items.Refresh();

            if (ItemCalendar.SelectedDate != null)
            {
                DateTime calendarDate = (DateTime)ItemCalendar.SelectedDate;
                weekTodoItems = CreateCalendarList((bool)MandatoryOnlyBox.IsChecked, calendarDate);
                todoTable.ItemsSource = weekTodoItems;
                todoTable.Items.Refresh();
            }
        }
        private void UpdateLists() //Get lists required to pre-populate combo-boxes
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
        private void UpdateTasksTable(bool keepChanges = false)
        {
            if (IsItemSelected())
            {
                List<CTTask> detailsTasks = database.GetTasks("SerialNumber", SelectedSN());
                if (keepChanges)
                {
                    //Add new items to table without reverting changes made to existing items.
                    List<CTTask> currentTaskList = new List<CTTask>();
                    foreach (CTTask task in DetailsTasksTable.Items)
                    {
                        currentTaskList.Add(task);
                    }
                    foreach (CTTask task in detailsTasks)
                    {
                        bool found = false;
                        foreach (CTTask currentTask in currentTaskList)
                        {
                            if (task.TaskID == currentTask.TaskID) { found = true; break; }
                        }
                        if (!found) { currentTaskList.Add(task); }
                    }
                    foreach (CTTask task in currentTaskList)
                    {
                        task.ServiceVendorList = serviceVendors;
                        task.IsTaskDue(config.MarkDueDays, DateTime.UtcNow);
                    }
                    DetailsTasksTable.ItemsSource = currentTaskList;
                    return;
                }
                foreach (CTTask task in detailsTasks) { task.ServiceVendorList = serviceVendors; task.IsTaskDue(config.MarkDueDays, DateTime.UtcNow); }
                DetailsTasksTable.ItemsSource = detailsTasks;
            }
        }
    }
}
