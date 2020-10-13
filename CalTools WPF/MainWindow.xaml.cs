using CalTools_WPF.ObjectClasses;
using CalTools_WPF.Windows;
using Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
namespace CalTools_WPF
{
    /// This file is reserved for GUI actions and event handlers in the main window.
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            config.LoadConfig();
            database = new CTDatabase(config.DbPath);
            database.ItemScansDir = config.ItemScansDir;
            database.Folders = config.Folders;
            InitializeComponent();
            LogicInit();
        }
        private void LogicInit()
        {
            CalToolsMainWindow.Title = $"CalTools {version}";
            if (database.DatabaseReady())
            {
                UpdateItemList();
                HighlightNonExistent();
                MandatoryOnlyBox.IsChecked = true;
                List<string> searchOptionsList = new List<string>(searchModes.Keys);
                searchOptionsList.Sort();
                SearchOptions.ItemsSource = (searchOptionsList);
                SearchOptions.SelectedItem = "Serial Number";
                ItemCalendar.SelectedDate = DateTime.Today;
            }
            DetailsManufacturer.ItemsSource = manufacturers;
            DetailsLocation.ItemsSource = locations;
            DetailsItemGroup.ItemsSource = itemGroups;
            todoTable.ItemsSource = weekTodoItems;
        }
        //Update GUI Elements
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

        private void DetailsEditToggle()
        {
            bool enable = !DetailsSN.IsEnabled;
            foreach (UIElement child in DetailsGrid.Children)
            {
                child.IsEnabled = enable;
            }
            DetailsComments.IsEnabled = enable;
            DetailsTasksTable.IsEnabled = enable;
            AddTaskButton.IsEnabled = enable;
            RemoveTaskButton.IsEnabled = enable;
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

        private void ExpandTreeItems()
        {
            foreach (TreeViewItem group in CalibrationItemTree.Items)
            {
                group.IsExpanded = true;
            }
        }
        //True if the TreeView has an item selected, false if item is null
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
        //True if a task is selected in the details area
        private bool IsTaskSelected()
        {
            if (DetailsTasksTable.SelectedItem == null) { return false; }
            else { return true; }
        }
        //Return header(SN) of selected item in treeview, empty string if no item is selected.
        private string SelectedSN()
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if (!IsItemSelected()) { return ""; }
            string sn = selectedItem.Header.ToString();
            return sn;
        }

        //GUI Event handlers---------------------------------------------------------------------------------------------------------------
        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleView();
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
        private void CalFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer", config.ListDir); }
            catch (System.Exception ex) { MessageBox.Show($"Error opening calibrations folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void ReceivingFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer", $"{config.ListDir}\\receiving"); }
            catch (System.Exception ex) { MessageBox.Show($"Error opening receiving folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void CalibrationItemTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DetailsSN.IsEnabled)
            {
                SaveItemButton.Visibility = Visibility.Collapsed;
                DetailsEditToggle();
                EditItemButton.Visibility = Visibility.Visible;
            }
            UpdateTasksTable();
        }
        private void UpdateTasksTable(bool keepChanges = false)
        {
            if (IsItemSelected())
            {
                UpdateDetails(database.GetItem("SerialNumber", SelectedSN()));
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
                        task.CheckDue(config.MarkDueDays, DateTime.UtcNow);
                    }
                    DetailsTasksTable.ItemsSource = currentTaskList;
                    return;
                }
                foreach (CTTask task in detailsTasks) { task.ServiceVendorList = serviceVendors; task.CheckDue(config.MarkDueDays, DateTime.UtcNow); }
                DetailsTasksTable.ItemsSource = detailsTasks;
            }
        }
        //Prevent calendar from holding focus, requiring two clicks to escape
        private void ItemCalendar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured is CalendarItem)
            {
                Mouse.Capture(null);
            }
        }
        private void EditItemButton_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if (selectedItem != null)
            {
                if (!config.Folders.Contains(selectedItem.Header))
                {
                    EditItemButton.Visibility = Visibility.Collapsed;
                    DetailsEditToggle();
                    SaveItemButton.Visibility = Visibility.Visible;
                }
            }
        }
        private void SaveItemButton_Click(object sender, RoutedEventArgs e)
        {
            SaveItem();
            SaveTasksTable();
            //Update specific item
            //UpdateSingleItem(SelectedSN());
            UpdateItemList(true);
        }
        private void SaveItem()
        {
            string sn = "";
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
                            sn = item.SerialNumber;
                        }
                    }
                    else
                    {
                        database.SaveItem(item);
                        SaveItemButton.Visibility = Visibility.Collapsed;
                        DetailsEditToggle();
                        EditItemButton.Visibility = Visibility.Visible;
                        sn = item.SerialNumber;
                    }
                }
                UpdateListsSingle(item);
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
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsItemSelected())
            {
                string directory = database.GetItem("SerialNumber", SelectedSN()).Directory;
                Process.Start("explorer", directory);
            }
        }
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateItemList();
            HighlightNonExistent();
        }
        private void NewReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsItemSelected()) { return; }
            if (DetailsTasksTable.SelectedItem == null)
            {
                MessageBox.Show("A task in the details area must be selected to add data.", "No Task Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            NewReport((CTTask)DetailsTasksTable.SelectedItem);
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
        private void DetailsStandardBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)DetailsStandardBox.IsChecked)
            {
                //Make all tasks mandatory for standard equipment
                foreach (CTTask task in database.GetTasks("SerialNumber", SelectedSN()))
                {
                    if (!task.Mandatory)
                    {
                        task.Mandatory = true;
                        database.SaveTask(task);
                    }
                }
                DetailsCertificateLabel.Visibility = Visibility.Visible;
                DetailsCertificateNum.Visibility = Visibility.Visible;
            }
            else
            {
                DetailsCertificateLabel.Visibility = Visibility.Collapsed;
                DetailsCertificateNum.Clear();
                DetailsCertificateNum.Visibility = Visibility.Collapsed;
            }

        }
        private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsItemSelected())
            {
                string selectedItem = SelectedSN();
                if (MessageBox.Show($"This will delete {selectedItem} from the database. Any files will remain (the item will be re-added if its folder isn't removed). Continue?",
                    "Delete Item", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    database.RemoveCalItem(selectedItem);
                    UpdateItemList();
                }
            }
        }
        private void MoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsItemSelected())
            {
                CTItem selectedItem = database.GetItem("SerialNumber", SelectedSN());
                NewItemFolderSelect selection = new NewItemFolderSelect();
                selection.FolderSelectComboBox.ItemsSource = config.Folders;
                if ((bool)selection.ShowDialog() & selectedItem != null)
                {
                    string selectedFolder = selection.FolderSelectComboBox.SelectedItem.ToString();
                    string newDirectory = $"{config.ItemScansDir}\\{selectedFolder}";
                    if (Directory.Exists(newDirectory)) { newDirectory += $"\\{selectedItem.SerialNumber}"; }
                    else { MessageBox.Show($"The directory \"{newDirectory}\" is missing or inaccessible.", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                    if (selectedItem.Directory == newDirectory) { return; }
                    Directory.Move(selectedItem.Directory, newDirectory);
                    UpdateItemList();
                }
            }
        }
        private void SearchOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddItemsToList(ItemListFilter(searchModes[SearchOptions.SelectedItem.ToString()], SearchBox.Text));
            ExpandTreeItems();
        }
        //Calendar event handlers----------------------------------------------------------------------------------------------------------
        private void ItemCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            weekTodoLabel.Content = "To do during week of: " + ItemCalendar.SelectedDate.Value.ToString(database.dateFormat);
            UpdateItemsTable();
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
        private void MandatoryOnlyBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateItemsTable();
        }
        private void TableMenuGoto_Click(object sender, RoutedEventArgs e)
        {
            if (todoTable.SelectedItem != null)
            { ToggleView(); GoToItem(((Dictionary<string, string>)todoTable.SelectedItem)["SerialNumber"]); }
        }
        private void TableMenuCalData_Click(object sender, RoutedEventArgs e)
        {
            if (todoTable.SelectedItem != null)
            {
                CTTask task = database.GetTasks("TaskID", ((Dictionary<string, string>)todoTable.SelectedItem)["TaskID"])[0];
                NewReport(task);
            }
        }

        private void ContextViewData_Click(object sender, RoutedEventArgs e)
        {
            if (DetailsTasksTable.SelectedItem != null)
            {
                CTTask currentTask = (CTTask)DetailsTasksTable.SelectedItem;
                List<TaskData> currentTaskData = database.GetTaskData(currentTask.TaskID.ToString());
                CalDataViewer viewer = new CalDataViewer(ref currentTaskData, currentTask);
                if (viewer.ShowDialog() == true)
                {
                    foreach (TaskData dbData in database.GetTaskData(currentTask.TaskID.ToString()))
                    {
                        bool delete = true;
                        foreach (TaskData windowData in currentTaskData)
                        {
                            if (windowData.DataID == dbData.DataID)
                            {
                                delete = false;
                            }
                        }
                        if (delete) { database.RemoveTaskData(dbData.DataID.ToString()); }
                    }
                }
            }
        }

        private void ContextOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (DetailsTasksTable.SelectedItem != null)
            {
                CTTask task = (CTTask)DetailsTasksTable.SelectedItem;
                CTItem item = database.GetItem("SerialNumber", task.SerialNumber);
                if (Directory.Exists(task.TaskDirectory))
                { Process.Start("explorer", task.TaskDirectory); }
                else if (Directory.Exists(item.Directory))
                { Process.Start("explorer", item.Directory); }
                else { Process.Start("explorer", config.ListDir); }
            }
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (!IsItemSelected())
            { MessageBox.Show("An item must be selected in the list to drop a file.", "No Item Selection", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            //Handle an Outlook attachment being dropped
            if (e.Data.GetDataPresent("FileGroupDescriptorW"))
            {
                OutlookDataObject outlookData = new OutlookDataObject(e.Data);
                string[] files = (string[])outlookData.GetData("FileGroupDescriptorW");
                if (files.Length > 1) { MessageBox.Show("Only one item can be dropped into the window at a time.", "Multiple Items", MessageBoxButton.OK, MessageBoxImage.Exclamation); return; }
                string file = files[0];
                //Move file from memory to receiving folder
                MemoryStream[] fileContents = (MemoryStream[])outlookData.GetData("FileContents");
                FileStream fileStream = new FileStream($"{config.ListDir}\\receiving\\{file}", FileMode.Create);

                string filePath = $"{config.ListDir}\\receiving\\{file}";
                fileContents[0].CopyTo(fileStream);
                fileStream.Close();
                //Try to move the file to item folder
                string newFileName = "";
                string taskFolder = "";
                do
                {
                    DropFileInfo info = new DropFileInfo();
                    if (IsItemSelected()) { info.SerialNumberBox.Text = SelectedSN(); }
                    info.DateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
                    info.TaskBox.ItemsSource = database.GetTasks("SerialNumber", SelectedSN());
                    if (info.ShowDialog() == false) { if (File.Exists(filePath)) { File.Delete(filePath); } return; }
                    else
                    {
                        if (Directory.Exists(filePath))
                        {
                            File.Delete(filePath);
                            MessageBox.Show("Drag and drop for folders is not currently supported.",
                                "Drag and Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        newFileName = $"{info.DateBox.Text}_{info.SerialNumberBox.Text}{Path.GetExtension(file)}";
                        CTTask currentTask = (CTTask)info.TaskBox.SelectedItem;
                        taskFolder = currentTask.GetTaskFolder();
                        if (taskFolder == "")
                        {
                            MessageBox.Show($"{info.SerialNumberBox.Text} Task: ({currentTask.TaskID}){currentTask.TaskTitle} does not have a valid folder.",
                                "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        }
                    }
                }
                while (!MoveToTaskFolder(filePath, taskFolder, newFileName));
            }
            //Handle a file on disk being dropped
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 1) { MessageBox.Show("Only one item can be dropped into the window at a time.", "Multiple Items", MessageBoxButton.OK, MessageBoxImage.Exclamation); return; }
                string file = files[0];
                string newFileName = "";
                string taskFolder = "";
                do
                {
                    if (Directory.Exists(file))
                    {
                        MessageBox.Show("Drag and drop for folders is not currently supported.",
                            "Drag and Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    DropFileInfo info = new DropFileInfo();
                    if (IsItemSelected()) { info.SerialNumberBox.Text = SelectedSN(); }
                    info.TaskBox.ItemsSource = database.GetTasks("SerialNumber", SelectedSN());
                    info.DateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
                    if (info.ShowDialog() == false) { return; }
                    else
                    {
                        newFileName = $"{info.DateBox.Text}_{info.SerialNumberBox.Text}{Path.GetExtension(file)}";
                        CTTask currentTask = (CTTask)info.TaskBox.SelectedItem;
                        taskFolder = currentTask.GetTaskFolder();
                        if (taskFolder == "")
                        {
                            MessageBox.Show($"{info.SerialNumberBox.Text} Task: ({currentTask.TaskID}){currentTask.TaskTitle} does not have a valid folder.",
                                "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        }
                    }
                }
                while (!MoveToTaskFolder(file, taskFolder, newFileName));
            }
            UpdateItemList();
        }

        //Update CTTask item when the combo box selection is changed.
        private void TaskActionColBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DetailsTasksTable.SelectedItem == null) { return; }
            ((CTTask)DetailsTasksTable.SelectedItem).ActionType = ((ComboBox)sender).SelectedItem.ToString();
        }

        private void TaskVendorColBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DetailsTasksTable.SelectedItem == null) { return; }
            ((CTTask)DetailsTasksTable.SelectedItem).ServiceVendor = ((ComboBox)sender).SelectedItem.ToString();
        }

        private void CalToolsMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            database.CleanUp();
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            CTItem currentItem = database.GetItem("SerialNumber", SelectedSN());
            if (Directory.Exists(currentItem.Directory))
            {
                database.SaveTask(new CTTask { SerialNumber = SelectedSN() }, true);
                int taskID = database.GetLastTaskID();
                if (taskID == -1) { return; }
                CTTask task = database.GetTasks("TaskID", taskID.ToString())[0];
                string newPath = Path.Combine(currentItem.Directory, $"{taskID}_{task.TaskTitle}");
                Directory.CreateDirectory(newPath);
                if (Directory.Exists(newPath)) { task.TaskDirectory = newPath; }
                database.SaveTask(task);
            }
            UpdateTasksTable(true);
        }

        private void RemoveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsTaskSelected())
            {
                CTTask task = (CTTask)DetailsTasksTable.SelectedItem;
                if (MessageBox.Show($"Remove ({task.TaskID}){task.TaskTitle}? This cannot be undone.", "Remove Task", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    == MessageBoxResult.Yes)
                {
                    if (Directory.Exists(task.TaskDirectory)) { Directory.Delete(task.TaskDirectory, true); }
                    database.RemoveTask(task.TaskID.ToString());
                    UpdateTasksTable();
                }
            }
        }
    }
}
