using CalTools_WPF.ObjectClasses;
using CalTools_WPF.Windows;
using Helpers;
using System;
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
    /// This file is reserved for event handlers in the main window.
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
        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleView();
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
            UpdateDetails(database.GetItem("SerialNumber", SelectedSN()));
            UpdateTasksTable();
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
            SearchItems();
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
        private void MandatoryOnlyBox_Checked(object sender, RoutedEventArgs e)
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

        //On program exit
        private void CalToolsMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            database.CleanUp();
        }
    }
}
