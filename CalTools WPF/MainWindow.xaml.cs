using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Xml.XPath;
using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;

namespace CalTools_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly string version = "4.0.0";
        private CTDatabase database;
        private CTConfig config = new CTConfig();
        public MainWindow()
        {
            config.LoadConfig();
            database = new CTDatabase(config.DbPath);
            InitializeComponent();
            CalToolsMainWindow.Title = $"CalTools {version}";
            UpdateItemList();
        }

        //Update GUI Elements
        private void UpdateItemList()
        {
            ScanFolders();
            CalibrationItemTree.Items.Clear();
            List<CalibrationItem> items = database.GetAllCalItems();
            foreach(string folder in config.Folders)
            {
                TreeViewItem group = new TreeViewItem();
                group.Header = folder;
                foreach(var item in items)
                {
                    if(item.Directory.Contains(folder))
                    {
                        TreeViewItem treeItem = new TreeViewItem();
                        treeItem.Header = item.SerialNumber;
                        group.Items.Add(treeItem);
                    }
                }
                CalibrationItemTree.Items.Add(group);
            }
        }

        private void UpdateDetails(CalibrationItem item)
        {
            if (item != null)
            {
                DetailsSN.Text = item.SerialNumber;
                DetailsModel.Text = item.Model;
                DetailsDescription.Text = item.Description;
                DetailsLocation.Text = item.Location;
                DetailsManufacturer.Text = item.Manufacturer;
                switch (item.VerifyOrCalibrate)
                {
                    case "MAINTENANCE":
                        {
                            DetailsAction.SelectedItem = ActionMaintenance;
                            break;
                        }
                    case "VERIFICATION":
                        {
                            DetailsAction.SelectedItem = ActionVerification;
                            break;
                        }
                    default:
                        {
                            DetailsAction.SelectedItem = ActionCalibration;
                            break;
                        }
                }
                DetailsVendor.Text = item.CalVendor;
                DetailsIntervalBox.Text = item.Interval.ToString();
                if (item.LastCal != null) { DetailsLastCal.Content = item.LastCal.Value.ToString("yyyy-MM-dd"); } else { DetailsLastCal.Content = ""; }
                if (item.InServiceDate != null) { DetailsOperationDate.Text = item.InServiceDate.Value.ToString("yyyy-MM-dd"); } else { DetailsOperationDate.Clear(); }
                if (item.NextCal != null) { DetailsNextCal.Content = item.NextCal.Value.ToString("yyyy-MM-dd"); } else { DetailsNextCal.Content = ""; }
                DetailsMandatory.IsChecked = item.Mandatory;
                DetailsInOperation.IsChecked = item.InService;
                DetailsItemGroup.Text = item.ItemGroup;
                DetailsComments.Text = item.Comment;
            }
        }

        private void DetailsEditToggle()
        {
            bool enable = !DetailsSN.IsEnabled;
            foreach (UIElement child in DetailsGrid.Children)
            {
                child.IsEnabled = enable;
                DetailsIntervalBox.IsEnabled = enable;
                DetailsIntervalUp.IsEnabled = enable;
                DetailsIntervalDown.IsEnabled = enable;
            }
        }

        private void ScanFolders()
        {
            string itemsFolder = $"{config.CalScansDir}\\Calibration Items\\";
            foreach(string folder in config.Folders)
            {
                string scanFolder = $"{itemsFolder}{folder}";
                if(Directory.Exists(scanFolder))
                {
                    foreach(string itemFolder in Directory.GetDirectories(scanFolder))
                    {
                        string itemSN = System.IO.Path.GetFileName(itemFolder);
                        CalibrationItem calItem = database.GetCalItem("calibration_items","serial_number",itemSN);
                        if (calItem == null) { calItem = new CalibrationItem(itemSN);}
                        calItem.Directory = itemFolder;
                        DateTime? latest = GetLatestCal(calItem.SerialNumber, calItem.Directory);
                        if (latest != null)
                        {
                        }
                    }
                }
            }
        }
        private DateTime? GetLatestCal(string sn, string folder)
        {
            DateTime? calDate = new DateTime();
            foreach(string filePath in Directory.GetFiles(folder))
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
                List<string> fileSplit = new List<string>();
                fileSplit.AddRange(file.Split("_"));
                DateTime fileDate;
                foreach (string split in fileSplit)
                {
                    if(DateTime.TryParseExact(split,database.dateFormat,CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal, out fileDate))
                    {
                        if(fileDate > calDate) { calDate = fileDate; break; }
                    }
                }
            }
            foreach(CalibrationData data in database.GetCalData(sn))
            {
                if(data.CalibrationDate > calDate) { calDate = data.CalibrationDate; Debug.WriteLine(calDate); }
            }
            return calDate;
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

        private bool IsItemSelected()
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            if(selectedItem != null)
            {
                if(!config.Folders.Contains(selectedItem.Header.ToString()))
                { return true; }
            }
            return false;
        }
        private string SelectedSN()
        {
            TreeViewItem selectedItem = (TreeViewItem)CalibrationItemTree.SelectedItem;
            string sn = selectedItem.Header.ToString();
            return sn;
        }

        //GUI Event handlers
        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            if(MainViewGrid.Visibility== Visibility.Visible)
            {
                MainViewGrid.Visibility = Visibility.Collapsed;
                CalendarViewGrid.Visibility = Visibility.Visible;
            }
            else
            {
                CalendarViewGrid.Visibility = Visibility.Collapsed;
                MainViewGrid.Visibility = Visibility.Visible;
            }
        }

        private void CalFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try{ Process.Start("explorer",config.CalListDir); }
            catch(System.Exception ex) { MessageBox.Show($"Error opening calibrations folder: {ex.Message}","Error",MessageBoxButton.OK,MessageBoxImage.Error); }
        }

        private void ReceivingFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer", $"{config.CalListDir}\\receiving"); }
            catch (System.Exception ex) { MessageBox.Show($"Error opening receiving folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void CalibrationItemTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selected;
            if (DetailsSN.IsEnabled) 
            {
                SaveItemButton.Visibility = Visibility.Collapsed;
                DetailsEditToggle();
                EditItemButton.Visibility = Visibility.Visible;
            }
            if(CalibrationItemTree.SelectedItem != null) 
            { 
                selected = (TreeViewItem)CalibrationItemTree.SelectedItem; 
                UpdateDetails(database.GetCalItem("calibration_items", "serial_number", (string)selected.Header)); 
            }
        }


        //Prevent calendar from holding focus, requiring two clicks to escape
        private void ItemCalendar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(Mouse.Captured is CalendarItem)
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
            string sn = "";
            if (DetailsSN.Text.Length > 0)
            {
                CalibrationItem item;
                item = database.GetCalItem("calibration_items", "serial_number", DetailsSN.Text);
                MessageBoxResult result = MessageBoxResult.None;
                if (item == null)
                {
                    item = new CalibrationItem(DetailsSN.Text);
                    result = MessageBox.Show($"Item SN: {DetailsSN.Text} does not exist in the database. Create this item?", "Item Not Found",MessageBoxButton.YesNo,MessageBoxImage.Information);                    
                }
                item.Model = DetailsModel.Text;
                item.Description = DetailsDescription.Text;
                item.Location = DetailsLocation.Text;
                item.Manufacturer = DetailsManufacturer.Text;
                item.VerifyOrCalibrate = DetailsAction.Text;
                item.CalVendor = DetailsVendor.Text;
                item.Interval = int.Parse(DetailsIntervalBox.Text);
                DateTime inservice;
                if (DateTime.TryParseExact(DetailsOperationDate.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out inservice))
                { item.InServiceDate = inservice; };
                item.Mandatory = DetailsMandatory.IsChecked == true;
                item.InService = DetailsInOperation.IsChecked == true;
                item.ItemGroup = DetailsItemGroup.Text;
                item.Comment = DetailsComments.Text;
                if(result==MessageBoxResult.Yes | result==MessageBoxResult.None)
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        string folder = GetNewItemFolder(item.SerialNumber);
                        if (folder != "") 
                        {
                            item.Directory = folder;
                            Directory.CreateDirectory(folder);
                            database.SaveCalItem(item);
                            UpdateDetails(database.GetCalItem("calibration_items", "serial_number", item.SerialNumber));
                            UpdateItemList();
                            sn = item.SerialNumber;
                        }
                    }
                    else
                    { 
                        database.SaveCalItem(item);
                        SaveItemButton.Visibility = Visibility.Collapsed;
                        DetailsEditToggle();
                        EditItemButton.Visibility = Visibility.Visible;
                        sn = item.SerialNumber;
                    }
                }
            }
            GoToItem(sn);
        }
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem selected;
            if (CalibrationItemTree.SelectedItem != null)
            {
                selected = (TreeViewItem)CalibrationItemTree.SelectedItem;
                string directory = database.GetCalItem("calibration_items", "serial_number", (string)selected.Header).Directory;
                Process.Start("explorer",directory);
            }
        }
        private void DetailsIntervalUp_Click(object sender, RoutedEventArgs e)
        {
            if (!DetailsIntervalBox.IsEnabled) { return; }
            int num;
            if(int.TryParse(DetailsIntervalBox.Text, out num))
            {
                num++;
                DetailsIntervalBox.Text = num.ToString();
            }
        }
        private void DetailsIntervalDown_Click(object sender, RoutedEventArgs e)
        {
            if (!DetailsIntervalBox.IsEnabled) { return; }
            int num;
            if (int.TryParse(DetailsIntervalBox.Text, out num))
            {
                num--;
                DetailsIntervalBox.Text = num.ToString();
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateItemList();
        }

        private void NewReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsItemSelected()) { return; }
            CalDataEntry dataEntry = new CalDataEntry();
            dataEntry.SerialNumberBox.Text = SelectedSN();
            if(dataEntry.ShowDialog() == true)
            {
                Debug.WriteLine(JsonConvert.SerializeObject(dataEntry.data));
            }
        }
    }
}
