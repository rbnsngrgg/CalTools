using CalTools_WPF.ObjectClasses;
using CalTools_WPF.Windows;
using Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
namespace CalTools_WPF
{
    /// This file is reserved for event handlers in the main window.
    public partial class MainWindow : Window
    {
        #region Main
        public MainWindow()
        {
            config.LoadConfig(Directory.GetCurrentDirectory());
            database = new(config.DbPath);
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
                InOperationOnlyBox.IsChecked = true;
                List<string> searchOptionsList = new(searchModes.Keys);
                searchOptionsList.Sort();
                SearchOptions.ItemsSource = searchOptionsList;
                SearchOptions.SelectedItem = "Serial Number";
                ItemCalendar.SelectedDate = DateTime.Today;
            }
            DetailsManufacturer.ItemsSource = manufacturers;
            DetailsLocation.ItemsSource = locations;
            DetailsItemGroup.ItemsSource = itemGroups;
            todoTable.ItemsSource = weekTodoItems;
        }
        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (!IsItemSelected())
            { MessageBox.Show("An item must be selected in the list to drop a file.", "No Item Selection", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            //Handle an Outlook attachment being dropped
            if (e.Data.GetDataPresent("FileGroupDescriptorW"))
            {
                OutlookDataObject outlookData = new(e.Data);
                string[] files = (string[])outlookData.GetData("FileGroupDescriptorW");
                if (files.Length > 1) { MessageBox.Show("Only one item can be dropped into the window at a time.", "Multiple Items", MessageBoxButton.OK, MessageBoxImage.Exclamation); return; }
                string file = files[0];
                //Move file from memory to receiving folder
                MemoryStream[] fileContents = (MemoryStream[])outlookData.GetData("FileContents");
                FileStream fileStream = new($"{config.ListDir}\\receiving\\{file}", FileMode.Create);

                string filePath = $"{config.ListDir}\\receiving\\{file}";
                fileContents[0].CopyTo(fileStream);
                fileStream.Close();
                //Try to move the file to item folder
                string newFileName;
                string taskFolder = null;
                do
                {
                    DropFileInfo info = new();
                    if (IsItemSelected()) { info.SerialNumberBox.Text = SelectedSN(); }
                    info.DateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
                    info.TaskBox.ItemsSource = database.GetTasks("serial_number", SelectedSN());
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
                        taskFolder = currentTask.GetTaskFolderIfExists();
                        if (taskFolder == null)
                        {
                            MessageBox.Show($"{info.SerialNumberBox.Text} Task: ({currentTask.TaskId}){currentTask.TaskTitle} does not have a valid folder.",
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
                string newFileName;
                string taskFolder = null;
                do
                {
                    if (Directory.Exists(file))
                    {
                        MessageBox.Show("Drag and drop for folders is not currently supported.",
                            "Drag and Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    DropFileInfo info = new();
                    if (IsItemSelected()) { info.SerialNumberBox.Text = SelectedSN(); }
                    info.TaskBox.ItemsSource = database.GetTasks("serial_number", SelectedSN());
                    info.DateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
                    if (info.ShowDialog() == false) { return; }
                    else
                    {
                        newFileName = $"{info.DateBox.Text}_{info.SerialNumberBox.Text}{Path.GetExtension(file)}";
                        CTTask currentTask = (CTTask)info.TaskBox.SelectedItem;
                        taskFolder = currentTask.GetTaskFolderIfExists();
                        if (taskFolder == null)
                        {
                            MessageBox.Show($"{info.SerialNumberBox.Text} Task: ({currentTask.TaskId}){currentTask.TaskTitle} does not have a valid folder.",
                                "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        }
                    }
                }
                while (!MoveToTaskFolder(file, taskFolder, newFileName));
            }
            UpdateItemList();
        }
        //On program exit
        private void CalToolsMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            database.CleanUp();
        }
        #endregion


        #region Main_Buttons
        private void FileMenuExit_Click(object sender, RoutedEventArgs e)
        {
            CalToolsMainWindow.Close();
        }
        private void ToolsMenuExportTsv_Click(object sender, RoutedEventArgs e)
        {
            ExportTSV();
        }
        private void ReceivingFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer", $"{config.ListDir}\\receiving"); }
            catch (System.Exception ex) { MessageBox.Show($"Error opening receiving folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateItemList();
            HighlightNonExistent();
        }
        private void CalFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer", config.ListDir); }
            catch (System.Exception ex) { MessageBox.Show($"Error opening calibrations folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleView();
        }
        private void EditItemButton_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if (selectedItem != null)
            {
                if (!config.Folders.Contains(selectedItem.Header)) //Check that the selected item is not a folder, but one of the database items.
                {
                    EditItemButton.Visibility = Visibility.Collapsed;
                    DetailsEditToggle();
                    SaveItemButton.Visibility = Visibility.Visible;
                }
            }
        }
        private void SaveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveItem())
            {
                SaveTasksTable();
                UpdateItemList(true);
            }
        }
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsItemSelected())
            {
                string directory = database.GetItem("serial_number", SelectedSN()).Directory;
                Process.Start("explorer", directory);
            }
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
        private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsItemSelected())
            {
                string selectedItem = SelectedSN();
                if (MessageBox.Show($"This will delete {selectedItem} from the database. Any files will remain (the item will be re-added if its folder isn't removed). Continue?",
                    "Delete Item", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    database.RemoveItem(selectedItem);
                    UpdateItemList();
                }
            }
        }
        private void MoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsItemSelected())
            {
                CTItem selectedItem = database.GetItem("serial_number", SelectedSN());
                NewItemFolderSelect selection = new();
                selection.FolderSelectComboBox.ItemsSource = config.Folders;
                selection.FolderSelectSerialNumber.Text = selectedItem.SerialNumber;
                selection.FolderSelectSerialNumber.IsReadOnly = true;
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
        #endregion


        #region Item_List
        private void CalibrationItemTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DetailsSN.IsEnabled)
            {
                SaveItemButton.Visibility = Visibility.Collapsed;
                DetailsEditToggle();
                EditItemButton.Visibility = Visibility.Visible;
            }
            UpdateDetails(database.GetItem("serial_number", SelectedSN()));
            UpdateTasksTable();
        }
        //TreeView Context Menu
        private void TreeViewNewItem_Click(object sender, RoutedEventArgs e)
        {
            CreateNewItem();
        }
        private void TreeViewReplaceItem_Click(object sender, RoutedEventArgs e)
        {
            SwapItems();
        }
        private void TreeViewContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (IsItemSelected()) { TreeViewReplaceItem.IsEnabled = true; }
            else { TreeViewReplaceItem.IsEnabled = false; }
        }
        //Search box-----------------------------------------------------------------------------------------------------------------------
        private void SearchOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchItems();
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddItemsToList(ItemListFilter(searchModes[SearchOptions.SelectedItem.ToString()], SearchBox.Text));
            ExpandTreeItems();
        }
        #endregion


        #region Item_Details
        private void DetailsStandardBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)DetailsStandardBox.IsChecked)
            {
                //Make all tasks mandatory for standard equipment
                foreach (CTTask task in database.GetTasks("serial_number", SelectedSN()))
                {
                    if (!task.IsMandatory)
                    {
                        task.IsMandatory = true;
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
        //Task data grid event handlers----------------------------------------------------------------------------------------------------
        private void ContextDateOverride_Click(object sender, RoutedEventArgs e)
        {
            CTTask currentTask = DetailsTasksTable.SelectedItem as CTTask;
            if (currentTask.DateOverride != null)
            {
                currentTask.DateOverride = null;
            }
            else { currentTask.DateOverride = DateTime.Today; }
        }
        private void ContextViewData_Click(object sender, RoutedEventArgs e)
        {
            if (DetailsTasksTable.SelectedItem != null)
            {
                CTTask currentTask = (CTTask)DetailsTasksTable.SelectedItem;
                List<TaskData> currentTaskData = database.GetTaskData(currentTask.TaskId.ToString());
                //Viewer may modify currentTaskData
                CalDataViewer viewer = new(ref currentTaskData, currentTask);
                if (viewer.ShowDialog() == true)
                {
                    foreach (TaskData dbData in database.GetTaskData(currentTask.TaskId.ToString()))
                    {
                        bool delete = true;
                        foreach (TaskData windowData in currentTaskData)
                        {
                            if (windowData.DataID == dbData.DataID)
                            {
                                //If the data is found in a fresh db query, it wasn't deleted in the viewer
                                delete = false;
                            }
                        }
                        if (delete)
                        {
                            database.RemoveTaskData(dbData.DataID.ToString());
                            SaveTasksTable();
                            //Save the new data and prompt for a certificate number update if the item is standard equipment
                            UpdateItemList(true);
                        }
                    }
                }
            }
        }
        private void ContextOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (DetailsTasksTable.SelectedItem != null)
            {
                CTTask task = (CTTask)DetailsTasksTable.SelectedItem;
                CTItem item = database.GetItem("serial_number", task.SerialNumber);
                if (Directory.Exists(task.TaskDirectory))
                { Process.Start("explorer", task.TaskDirectory); }
                else if (Directory.Exists(item.Directory))
                { Process.Start("explorer", item.Directory); }
                else { Process.Start("explorer", config.ListDir); }
            }
        }
        private void TaskDataGridContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            //Change ContextMarkDue_Click depending on if the task is already flagged
            CTTask currentTask = DetailsTasksTable.SelectedItem as CTTask;
            if (IsTaskSelected())
            {
                ContextDateOverride.IsEnabled = DetailsSN.IsEnabled;
                ContextViewData.IsEnabled = true;
                ContextOpenLocation.IsEnabled = true;
                ContextDateOverride.Header = currentTask.DateOverride == null ? "Apply Date Override" : "Clear Date Override";
            }
            else
            {
                ContextDateOverride.IsEnabled = false;
                ContextViewData.IsEnabled = false;
                ContextOpenLocation.IsEnabled = false;
            }
        }
        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            CTItem currentItem = database.GetItem("serial_number", SelectedSN());
            if (Directory.Exists(currentItem.Directory))
            {
                database.SaveTask(new CTTask { SerialNumber = SelectedSN() }, true);
                int taskID = database.GetLastTaskID();
                if (taskID == -1) { return; }
                CTTask task = database.GetTasks("id", taskID.ToString())[0];
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
                if (MessageBox.Show($"Remove ({task.TaskId}){task.TaskTitle}? This cannot be undone.", "Remove Task", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    == MessageBoxResult.Yes)
                {
                    if (Directory.Exists(task.TaskDirectory)) { Directory.Delete(task.TaskDirectory, true); }
                    database.RemoveTask(task.TaskId.ToString());
                    UpdateTasksTable();
                }
            }
        }
        #endregion


        #region Calendar
        //Calendar event handlers----------------------------------------------------------------------------------------------------------
        //Prevent calendar from holding focus, requiring two clicks to escape
        private void ItemCalendar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured is CalendarItem)
            {
                Mouse.Capture(null);
            }
        }
        private void ItemCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            weekTodoLabel.Content = $"Due within {config.MarkDueDays} days of: {ItemCalendar.SelectedDate.Value.ToString(database.dateFormat)}";
            UpdateItemsTable();
        }
        private void MandatoryOnlyBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateItemsTable();
        }
        private void InOperationOnlyBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateItemsTable();
        }
        private void TableMenuGoto_Click(object sender, RoutedEventArgs e)
        {
            if (todoTable.SelectedItem != null)
            {
                ToggleView();
                if (SearchOptions.SelectedItem.ToString() == "Serial Number") { SearchItems(); }
                else { SearchOptions.SelectedItem = "Serial Number"; }
                GoToItem(((Dictionary<string, string>)todoTable.SelectedItem)["SerialNumber"]);
            }
        }
        private void TableMenuCalData_Click(object sender, RoutedEventArgs e)
        {
            if (todoTable.SelectedItem != null)
            {
                CTTask task = database.GetTasks("id", ((Dictionary<string, string>)todoTable.SelectedItem)["TaskID"])[0];
                NewReport(task);
                UpdateItemsTable();
            }
        }
        private void ExportDueList_Click(object sender, RoutedEventArgs e)
        {
            ExportDueListTSV();
        }
        #endregion
    }
}
