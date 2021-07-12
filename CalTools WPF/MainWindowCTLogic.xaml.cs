using CalTools_WPF.ObjectClasses;
using CalTools_WPF.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.Linq;

namespace CalTools_WPF
{
    //Main window code-behind logic outside of event handlers
    //TODO: Create separate class. Decouple from MainWindow, which isn't unit tested
    public partial class MainWindow : Window
    {
        public readonly string version = "6.0.0";
        private readonly CTDatabase database;
        private readonly CTConfig config = new();
        private readonly Dictionary<string, string> searchModes = new() {
            { "Serial Number", "SerialNumber" },
            { "Location", "Location" },
            { "Service Vendor", "Vendor" },
            { "Manufacturer", "Manufacturer" },
            { "Description", "Description" },
            { "Action Due", "Due" },
            { "Model", "Model" },
            { "Has Remarks", "Remarks" },
            { "Item Group", "ItemGroup" },
            { "Action", "ActionType" },
            { "Standard Equipment", "IsStandardEquipment"} };
        private readonly List<string> manufacturers = new();
        private readonly List<string> locations = new();
        private readonly List<string> serviceVendors = new();
        private readonly List<string> itemGroups = new();
        private readonly List<string> standardEquipment = new();
        private readonly List<Dictionary<string, string>> weekTodoItems = new();
        //Dict keys: SerialNumber, Model, TaskID, TaskTitle, Description, Location, ServiceVendor, DueDateString

        #region FileOps
        private void CheckReceiving()
        {
            string receivingFolder = $"{config.ListDir}\\receiving";
            if (!Directory.Exists(receivingFolder) & Directory.Exists(config.ListDir)) { Directory.CreateDirectory(receivingFolder); }
            List<string> files = new(Directory.GetFiles(receivingFolder));
            if (files.Count > 0)
            {
                foreach (string file in files)
                {
                    MoveToItemFolder(file);
                }
            }
        }
        private void CreateTaskFolders(ref List<CTTask> tasks)
        {
            foreach (CTTask task in tasks)
            {
                if (task.TaskDirectory == "")
                {
                    CTItem taskItem = database.GetFromWhere<CTItem>(new() { { "serial_number", task.SerialNumber } })[0];
                    if (Directory.Exists(taskItem.Directory))
                    {
                        string newPath = Path.Combine(taskItem.Directory, $"{task.TaskId}_{task.TaskTitle}");
                        Directory.CreateDirectory(newPath);
                        task.TaskDirectory = newPath;
                    }
                }
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
            NewItemFolderSelect folderDialog = new();
            foreach (string configFolder in config.Folders)
            {
                ComboBoxItem boxItem = new() { Content = configFolder };
                folderDialog.FolderSelectComboBox.Items.Add(boxItem);
            }
            folderDialog.FolderSelectSerialNumber.Text = sn;
            folderDialog.FolderSelectSerialNumber.IsReadOnly = true;
            if (folderDialog.ShowDialog() == true)
            {
                //Check that the folder from the config exists before the new item folder is allowed to be created.
                folder = CreateFolderIfNotExists($"{config.ItemScansDir}\\{folderDialog.FolderSelectComboBox.Text}", folderDialog.FolderSelectSerialNumber.Text);
            }
            return folder;
        }
        private string CreateFolderIfNotExists(string folder, string sn)
        {
            string newFolder = "";
            if (Directory.Exists(folder))
            { 
                newFolder = $"{folder}\\{sn}";
                try
                {
                    Directory.CreateDirectory(newFolder);
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message, "Error creating a folder for the new item", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return newFolder;
        }
        private bool MoveToItemFolder(string file, string newFileName = "")
        {
            Dictionary<string, string> fileInfo;
            CTItem calItem;
            fileInfo = newFileName == "" ? ParseFileName(file) : ParseFileName(newFileName);
            calItem = database.GetFromWhere<CTItem>(new() { { "serial_number", fileInfo["SN"] } })[0];
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
        private Dictionary<string, string> ParseFileName(string filePath)
        {
            Dictionary<string, string> fileInfo = new()
            {
                { "Date", "" },
                { "SN", "" }
            };
            string file = Path.GetFileNameWithoutExtension(filePath);
            DateTime fileDate;
            foreach (string split in file.Split("_"))
            {
                if (DateTime.TryParseExact(split, database.dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate))
                {
                    fileInfo["Date"] = split;
                }
                else if (database.GetFromWhere<CTItem>(new() { { "serial_number", split } }).Count > 0)
                {
                    fileInfo["SN"] = split;
                }
            }
            return fileInfo;
        }
        private void ExportDueListTSV()
        {
            string file = $"{DateTime.UtcNow.ToString(database.dateFormat)}_Items Due Near {((DateTime)ItemCalendar.SelectedDate).ToString(database.dateFormat)}.txt";
            string exportsFolder = Path.Join(config.ListDir, "CalTools Exports");
            if (!Directory.Exists(exportsFolder)) { Directory.CreateDirectory(exportsFolder); }
            List<string> listLines = new() { "SerialNumber\tModel\tTaskTitle\tDescription\tLocation\tServiceVendor\tDueDate" };
            foreach (Dictionary<string, string> item in weekTodoItems)
            {
                listLines.Add($"{item["SerialNumber"]}\t{item["Model"]}\t{item["TaskTitle"]}\t{item["Description"]}" +
                    $"\t{item["Location"]}\t{item["ServiceVendor"]}\t{item["DueDateString"]}");
            }
            System.IO.File.WriteAllLines(Path.Join(exportsFolder, file), listLines);
            Process.Start("explorer", exportsFolder);
        }
        #endregion


        #region ToDatabase
        private void CheckTasks(string folder, ref List<CTTask> tasks, ref List<TaskData> taskData)
        {
            foreach (string taskFolder in Directory.GetDirectories(folder))
            {
                string folderTaskID = Path.GetFileName(taskFolder).Split("_")[0];
                foreach (CTTask task in tasks)
                {
                    if (folderTaskID == task.TaskId.ToString() & Path.GetFileName(folder) == task.SerialNumber)
                    {
                        List<TaskData> currentData = new();
                        foreach (TaskData data in taskData)
                        {
                            if (data.TaskId == task.TaskId) { currentData.Add(data); }
                        }
                        if (task.TaskDirectory != taskFolder) { task.TaskDirectory = taskFolder; }
                        task.SetCompleteDateFromData(taskFolder, currentData);
                        break;
                    }
                }
            }
            SaveTaskChanges(ref tasks);
        }
        private bool SaveItem()
        {
            if (DetailsSN.Text.Length > 0)
            {
                CTItem item = database.GetFromWhere<CTItem>(new() { { "serial_number", DetailsSN.Text } })[0];
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
                item.InService = DetailsInOperation.IsChecked == true;
                item.ItemGroup = DetailsItemGroup.Text;
                item.Remarks = DetailsComments.Text;
                if ((bool)DetailsStandardBox.IsChecked)
                {
                    item.IsStandardEquipment = true;
                    if (DetailsCertificateNum.Text.Length == 0)
                    {
                        MessageBox.Show("A certificate number is required for items marked as Standard Equipment.", "Certificate Number", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return false;
                    }
                }
                else { item.IsStandardEquipment = false; }
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
                            if (ItemIsSelected())
                            { UpdateDetails(database.GetFromWhere<CTItem>(new() { { "serial_number", item.SerialNumber } })[0]); }
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
            return true;
        }
        private void SaveTaskChanges(ref List<CTTask> tasks)
        {
            foreach (CTTask task in tasks)
            {
                if (task.ChangesMade)
                {
                    database.SaveTask(task);
                    if (task.CompleteDateChanged) { PromptCertificateNumber(task.SerialNumber); }
                    task.ChangesMade = false;
                    task.CompleteDateChanged = false;
                }
            }
        }
        private void SaveTasksTable()
        {
            foreach (CTTask task in DetailsTasksTable.Items)
            {
                string itemFolder = Directory.GetParent(task.TaskDirectory).FullName;
                string currentFolder = Path.Combine(itemFolder, Path.GetFileName(task.TaskDirectory));
                string newFolder = Path.Combine(itemFolder, $"{task.TaskId}_{task.TaskTitle}");

                if (currentFolder != newFolder) { Directory.Move(currentFolder, newFolder); task.TaskDirectory = newFolder; }
                if (task.ChangesMade)
                {
                    database.SaveTask(task);
                    if (task.CompleteDateChanged) { PromptCertificateNumber(task.SerialNumber); task.CompleteDateChanged = false; }
                }
            }
        }
        private void ScanFoldersSingle(CTItem item)
        {
            if (item == null) { return; }
            List<CTTask> itemTasks = database.GetFromWhere<CTTask>(new() { { "serial_number", item.SerialNumber } });
            List<TaskData> taskData = database.GetAll<TaskData>();
            if (!Directory.Exists(item.Directory)) { item.Directory = FindItemDirectory(item.SerialNumber); }
            CheckTasks(item.Directory, ref itemTasks, ref taskData);
            if (item.ChangesMade) { database.SaveItem(item); }
        }
        private void ScanFolders()
        {
            List<CTTask> allTasks = database.GetAll<CTTask>();
            List<CTItem> allItems = database.GetAll<CTItem>();
            List<TaskData> taskData = database.GetAll<TaskData>();
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
                            if (item.SerialNumber == itemSN)
                            {
                                calItem = item;

                                break;
                            }
                        }
                        if (calItem == null) { calItem = new CTItem(itemSN); newItem = true; }  //If existing item wasn't found, create new
                        if (calItem.Directory != itemFolder)                                    //Check if directory is valid. Set to the found folder if not
                        {
                            calItem.Directory = itemFolder;
                            if (newItem) { database.SaveItem(calItem); }
                        }
                        CheckTasks(itemFolder, ref allTasks, ref taskData);
                        if (calItem.ChangesMade) { database.SaveItem(calItem); }
                    }
                }
            }
            CreateTaskFolders(ref allTasks);
        }
        #endregion


        #region FromDatabase
        private void CheckReplacements(ref List<CTItem> itemList)
        {
            Dictionary<string, List<CTItem>> groups = new();
            foreach (CTItem item in itemList) //Get item groups, add items to list in dictionary
            {
                if (item.ItemGroup != "")
                {
                    if (groups.ContainsKey(item.ItemGroup))
                    { groups[item.ItemGroup].Add(item); }
                    else
                    {
                        groups.Add(item.ItemGroup, new List<CTItem>() { item });
                    }
                }
            }
            foreach (List<CTItem> group in groups.Values) //for item in group
            {
                if (group.Count < 2) { continue; }
                int availableItems = 0;
                foreach (CTItem item in group) //get number of available items
                {
                    if (IsItemAvailable(item))
                    {
                        availableItems += 1;
                    }
                }
                foreach (CTItem item in group) //expend replacements
                {
                    if (availableItems == 0)
                    {
                        break;
                    }
                    bool itemDue = false;
                    foreach (CTTask task in database.GetFromWhere<CTTask>(new() { { "serial_number", item.SerialNumber } }))
                    {
                        if (task.IsTaskDueWithinDays(config.MarkDueDays, DateTime.UtcNow) & task.IsMandatory)
                        { itemDue = true; break; }
                    }
                    if (itemDue & availableItems > 0)
                    {
                        if (itemList.Any(i => i.SerialNumber == item.SerialNumber))
                        {
                            itemList[itemList.FindIndex(i => i.SerialNumber == item.SerialNumber)].ReplacementAvailable = true;
                            availableItems -= 1;
                        }
                    }
                }
            }
        }
        private List<CTStandardEquipment> GetCurrentStandardEquipment()
        {
            List<CTItem> items = database.GetAll<CTItem>();
            List<CTStandardEquipment> standardEquipment = new();
            items.RemoveAll(item => !item.IsStandardEquipment);
            foreach(CTItem item in items)
            {
                standardEquipment.Add(item.ToStandardEquipment(GetMandatoryDueDate(item.SerialNumber)));
            }
            //Standard equipment that has a mandatory task due will not be included in the list.
            standardEquipment.RemoveAll(item => item.ActionDueDate < DateTime.Today);
            return standardEquipment;
        }
        private DateTime GetMandatoryDueDate(string sn)
        {
            List<CTTask> tasks = database.GetFromWhere<CTTask>(new() { { "serial_number", sn } });
            DateTime earliest = DateTime.MaxValue;
            bool hasMandatoryTasks = false;
            foreach(CTTask task in tasks)
            {
                if (task.IsMandatory) { hasMandatoryTasks = true; }
                if (task.DueDate < earliest) { earliest = (DateTime)task.DueDate; }
            }
            if (earliest == DateTime.MaxValue)
            {
                return hasMandatoryTasks ? DateTime.MinValue : earliest;
            }
            else { return earliest; }
        }
        private bool IsItemAvailable(CTItem item = null)
        {
            bool tasksDue = false;
            foreach (CTTask task in database.GetFromWhere<CTTask>(new() { { "serial_number", item.SerialNumber } }))
            {
                if (ItemCalendar.SelectedDate != null)
                {
                    DateTime calendarDate = (DateTime)ItemCalendar.SelectedDate;
                    if (task.IsTaskDueWithinDays(config.MarkDueDays, calendarDate) & task.IsMandatory)
                    {
                        tasksDue = true;
                    }
                }
            }
            return !tasksDue & !item.InService;
        }
        private void UpdateLists() //Get lists required to pre-populate combo-boxes
        {
            manufacturers.Clear();
            serviceVendors.Clear();
            locations.Clear();
            itemGroups.Clear();
            standardEquipment.Clear();
            foreach (CTItem calItem in database.GetAll<CTItem>())
            {
                if (!manufacturers.Contains(calItem.Manufacturer)) { manufacturers.Add(calItem.Manufacturer); }
                if (!locations.Contains(calItem.Location)) { locations.Add(calItem.Location); }
                if (!itemGroups.Contains(calItem.ItemGroup)) { itemGroups.Add(calItem.ItemGroup); }
                if (calItem.IsStandardEquipment & !standardEquipment.Contains(calItem.SerialNumber)) { standardEquipment.Add(calItem.SerialNumber); }
            }
            foreach (CTTask task in database.GetAll<CTTask>())
            { if (!serviceVendors.Contains(task.ServiceVendor)) { serviceVendors.Add(task.ServiceVendor); } }
            manufacturers.Sort();
            serviceVendors.Sort();
            locations.Sort();
            itemGroups.Sort();
            standardEquipment.Add("");
            standardEquipment.Sort();
        }
        #endregion


        #region ItemListOps
        private void AddItemsToList(List<CTItem> items)
        {
            items.Sort((x, y) => x.SerialNumber.CompareTo(y.SerialNumber));
            CalibrationItemTree.Items.Clear();
            foreach (string folder in config.Folders)
            {
                TreeViewItem group = new();
                group.Header = folder;
                foreach (var item in items)
                {
                    if (item.Directory.Contains(folder))
                    {
                        TreeViewItem treeItem = new();
                        treeItem.Header = item.SerialNumber;
                        group.Items.Add(treeItem);
                    }
                }
                CalibrationItemTree.Items.Add(group);
            }
        }
        private void CreateNewItem()
        {
            NewItemFolderSelect folderDialog = new();
            foreach (string configFolder in config.Folders)
            {
                ComboBoxItem boxItem = new() { Content = configFolder };
                folderDialog.FolderSelectComboBox.Items.Add(boxItem);
            }
            if (folderDialog.ShowDialog() == true)
            {
                //Check that the folder from the config exists before the new item folder is allowed to be created.
                string folder = CreateFolderIfNotExists($"{config.ItemScansDir}\\{folderDialog.FolderSelectComboBox.Text}", folderDialog.FolderSelectSerialNumber.Text);
                CTItem newItem = new(folderDialog.FolderSelectSerialNumber.Text);
                newItem.Directory = folder;
                database.SaveItem(newItem);
                UpdateItemList();
                GoToItem(newItem.SerialNumber);
            }
        }
        private void ExpandTreeItems()
        {
            foreach (TreeViewItem group in CalibrationItemTree.Items)
            {
                group.IsExpanded = true;
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
            List<string> nonExistent = new();
            foreach (CTItem calItem in database.GetAll<CTItem>())
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
        private List<CTItem> ItemListFilter(string mode, string searchText) //Filters items when search is used
        {
            List<CTItem> filteredItems = new();
            List<CTTask> allTasks = database.GetAll<CTTask>();
            var property = typeof(CTItem).GetProperty(mode);
            foreach (CTItem item in database.GetAll<CTItem>())
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
                else if (mode == "Remarks") { if (property.GetValue(item).ToString().Length > 0) { filteredItems.Add(item); } }
                else if (mode == "IsStandardEquipment") { if ((bool)property.GetValue(item)) { filteredItems.Add(item); } }
                else if (mode == "Due")
                {
                    foreach (CTTask task in allTasks)
                    {
                        if (task.SerialNumber == item.SerialNumber & task.IsTaskDueWithinDays(config.MarkDueDays, DateTime.UtcNow)) { filteredItems.Add(item); break; }
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
        private bool ItemIsSelected()
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if (selectedItem != null)
            {
                if (!config.Folders.Contains(selectedItem.Header.ToString()))
                { return true; }
            }
            return false;
        }
        private void SearchItems()
        {
            SearchBox.Clear();
            string selection = SearchOptions.SelectedItem.ToString();
            if (selection == "Action Due" | selection == "Has Remarks" | selection == "Standard Equipment")
            {
                AddItemsToList(ItemListFilter(searchModes[selection], ""));
                SearchBox.IsEnabled = false;
                ExpandTreeItems();
            }
            else { SearchBox.IsEnabled = true; AddItemsToList(database.GetAll<CTItem>()); }
        }
        private string SelectedSN()
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if (!ItemIsSelected()) { return ""; }
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
        private void UpdateItemList(bool single = false)
        {
            if (!database.DatabaseReady()) { return; }
            CheckReceiving();
            string currentItem = SelectedSN();
            if (single) { ScanFoldersSingle(database.GetFromWhere<CTItem>(new() { { "serial_number", currentItem } })[0]); }
            else { ScanFolders(); }

            if (SearchBox.Text.Length != 0)
            {
                AddItemsToList(ItemListFilter(searchModes[SearchOptions.SelectedItem.ToString()], SearchBox.Text));
                ExpandTreeItems();
            }
            else { AddItemsToList(database.GetAll<CTItem>()); }
            UpdateLists();
            GoToItem(currentItem);
            
            
        }
        private void UpdateListsSingle(CTItem item)
        {
            if (!manufacturers.Contains(item.Manufacturer)) { manufacturers.Add(item.Manufacturer); }
            if (!locations.Contains(item.Location)) { locations.Add(item.Location); }
            if (!itemGroups.Contains(item.ItemGroup)) { itemGroups.Add(item.ItemGroup); }
            if (item.IsStandardEquipment & !standardEquipment.Contains(item.SerialNumber)) { standardEquipment.Add(item.SerialNumber); }
            foreach (CTTask task in database.GetFromWhere<CTTask>(new() { { "serial_number", item.SerialNumber } }))
            { if (!serviceVendors.Contains(task.ServiceVendor)) { serviceVendors.Add(task.ServiceVendor); } }
            manufacturers.Sort();
            serviceVendors.Sort();
            locations.Sort();
            itemGroups.Sort();
            standardEquipment.Sort();
        }
        #endregion


        #region ItemDetailsOps
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
        private bool IsTaskSelected()
        {
            if (DetailsTasksTable.SelectedItem == null) { return false; }
            else { return true; }
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
                DetailsInOperation.IsChecked = item.InService;
                DetailsItemGroup.Text = item.ItemGroup;
                DetailsComments.Text = item.Remarks;
                DetailsStandardBox.IsChecked = item.IsStandardEquipment;
                DetailsCertificateNum.Text = item.CertificateNumber;
            }
        }
        private void UpdateTasksTable(bool keepChanges = false)
        {
            if (ItemIsSelected())
            {
                List<CTTask> detailsTasks = database.GetFromWhere<CTTask>(new() { { "serial_number", SelectedSN() } });
                if (keepChanges)
                {
                    //Add new items to table without reverting changes made to existing items.
                    List<CTTask> currentTaskList = new();
                    foreach (CTTask task in DetailsTasksTable.Items)
                    {
                        currentTaskList.Add(task);
                    }
                    foreach (CTTask task in detailsTasks)
                    {
                        bool found = false;
                        foreach (CTTask currentTask in currentTaskList)
                        {
                            if (task.TaskId == currentTask.TaskId) { found = true; break; }
                        }
                        if (!found) { currentTaskList.Add(task); }
                    }
                    foreach (CTTask task in currentTaskList)
                    {
                        task.ServiceVendorList = serviceVendors;
                        task.SetDueWithinDays(config.MarkDueDays, DateTime.UtcNow);
                    }
                    DetailsTasksTable.ItemsSource = currentTaskList;
                    return;
                }
                foreach (CTTask task in detailsTasks) { task.ServiceVendorList = serviceVendors; task.SetDueWithinDays(config.MarkDueDays, DateTime.UtcNow); }
                DetailsTasksTable.ItemsSource = detailsTasks;
            }
        }
        #endregion


        #region CalendarOps
        private List<Dictionary<string, string>> CreateCalendarList(bool mandatoryOnly, bool inOperationOnly, DateTime calendarDate)
        {
            List<Dictionary<string, string>> compositeList = new();
            List<CTTask> allTasks = database.GetAll<CTTask>();
            List<CTItem> allItems = database.GetAll<CTItem>();
            CheckReplacements(ref allItems);
            foreach (CTTask task in allTasks)
            {
                if (ItemCalendar.SelectedDate != null)
                {
                    if (mandatoryOnly)
                    {
                        if (!task.IsMandatory) { continue; }
                    }
                    if (inOperationOnly)
                    {
                        if (!allItems.Find(x => x.SerialNumber == task.SerialNumber).InService) { continue; }
                    }
                    foreach (CTItem item in allItems)
                    {
                        if (item.SerialNumber == task.SerialNumber)
                        {
                            if (task.IsTaskDueWithinDays(config.MarkDueDays, calendarDate))
                            {
                                Dictionary<string, string> compositeItem = new()
                                {
                                    {"SerialNumber",item.SerialNumber},
                                    {"Model", item.Model},
                                    {"TaskID", task.TaskId.ToString()},
                                    {"TaskTitle",$"({task.TaskId}) {task.TaskTitle}" },
                                    {"Description",item.Description},
                                    {"Location",item.Location},
                                    {"ServiceVendor",task.ServiceVendor},
                                    {"DueDateString",task.DueDateString},
                                    {"ReplacementAvailable", item.ReplacementAvailable.ToString() }
                                };
                                compositeList.Add(compositeItem);
                            }
                        }
                    }
                }
            }
            return compositeList;
        }
        private void UpdateItemsTable()
        {
            weekTodoItems.Clear();
            todoTable.Items.Refresh();

            if (ItemCalendar.SelectedDate != null)
            {
                DateTime calendarDate = (DateTime)ItemCalendar.SelectedDate;
                weekTodoItems.AddRange(CreateCalendarList((bool)MandatoryOnlyBox.IsChecked, (bool)InOperationOnlyBox.IsChecked, calendarDate));
                todoTable.ItemsSource = weekTodoItems;
                todoTable.Items.Refresh();
            }
        }
        #endregion


        #region WindowsAndDialogs
        private void NewReport(CTTask task)
        {
            try
            {
                CalDataEntry dataEntry = new();
                if (database.GetFromWhere<CTItem>(new() { { "serial_number", task.SerialNumber } })[0].IsStandardEquipment)
                {
                    dataEntry.ItemIsStandard = true;
                }
                dataEntry.SerialNumberBox.Text = task.SerialNumber;
                dataEntry.DateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
                dataEntry.ProcedureBox.ItemsSource = config.Procedures;
                dataEntry.EquipmentDataGrid.ItemsSource = GetCurrentStandardEquipment();
                dataEntry.TaskBox.Text = $"({task.TaskId}) {task.TaskTitle}";
                dataEntry.data.TaskId = task.TaskId;
                if (task.ActionType == "MAINTENANCE")
                { dataEntry.MaintenanceSelection.IsSelected = true; }
                if (config.Procedures.Count > 0) { dataEntry.ProcedureBox.SelectedIndex = 0; }
                dataEntry.parameters.Add(new Findings($"Parameter {dataEntry.parameters.Count + 1}"));
                if (dataEntry.ShowDialog() == true)
                {
                    database.SaveTaskData(dataEntry.data);
                }
                SaveTasksTable();
                CTItem item = database.GetFromWhere<CTItem>(new() { { "serial_number", task.SerialNumber } })[0];
                List<CTTask> tasks = database.GetFromWhere<CTTask>(new() { { "serial_number", item.SerialNumber } });
                List<TaskData> taskData = database.GetAll<TaskData>();
                CheckTasks(item.Directory, ref tasks, ref taskData);
                UpdateTasksTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}",
                    "MainWindowCTLogic.xaml.cs.NewReport",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void SwapItems()
        {
            List<string> itemsInGroup = new();
            CTItem selectedItem = database.GetFromWhere<CTItem>(new() { { "serial_number", SelectedSN() } })[0];
            foreach (CTItem item in database.GetAll<CTItem>())
            {
                if (item.ItemGroup == selectedItem.ItemGroup)
                {
                    itemsInGroup.Add(item.SerialNumber);
                }
            }
            ReplaceItemSelection selectionDialog = new();
            selectionDialog.ReplaceSelectComboBox.ItemsSource = itemsInGroup;
            if (selectionDialog.ShowDialog() == true)
            {
                CTItem newItem1 = database.GetFromWhere<CTItem>(
                    new() { { "serial_number", selectedItem.SerialNumber } })[0];
                CTItem newItem2 = database.GetFromWhere<CTItem>(
                    new() { { "serial_number", selectionDialog.ReplaceSelectComboBox.SelectedItem.ToString() } })[0];

                newItem1.Location = newItem2.Location;
                newItem1.InService = newItem2.InService;

                newItem2.Location = selectedItem.Location;
                newItem2.InService = selectedItem.InService;

                database.SaveItem(newItem1);
                database.SaveItem(newItem2);
            }
        }
        private void PromptCertificateNumber(string sn)
        {
            if (standardEquipment.Contains(sn))
            {
                MessageBox.Show("This item is standard equipment, be sure to update the certificate number.",
                    "Certificate Number Update", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion
    }
}
