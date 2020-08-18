using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;
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
    /// This file is reserved for GUI actions and event handlers in the main window.
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            config.LoadConfig();
            database = new CTDatabase(config.DbPath);
            InitializeComponent();
            CalToolsMainWindow.Title = $"CalTools {version}";
            UpdateItemList();
            DetailsManufacturer.ItemsSource = manufacturers;
            DetailsLocation.ItemsSource = locations;
            DetailsVendor.ItemsSource = calVendors;
            DetailsItemGroup.ItemsSource = itemGroups;
            List<string> searchOptionsList = new List<string>(searchModes.Keys);
            searchOptionsList.Sort();
            SearchOptions.ItemsSource = (searchOptionsList);
            SearchOptions.SelectedItem = "Serial Number";
        }

        //Update GUI Elements
        private void UpdateItemList()
        {
            string currentItem = SelectedSN();
            ScanFolders();
            AddItemsToList(database.GetAllCalItems());
            UpdateLists();
            GoToItem(currentItem);
        }
        private void AddItemsToList(List<CalibrationItem> items)
        {
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
            DetailsIntervalBox.IsEnabled = enable;
            DetailsIntervalUp.IsEnabled = enable;
            DetailsIntervalDown.IsEnabled = enable;
            DetailsComments.IsEnabled = enable;
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
            if (MainViewGrid.Visibility == Visibility.Visible)
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
            try { Process.Start("explorer", config.CalListDir); }
            catch (System.Exception ex) { MessageBox.Show($"Error opening calibrations folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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
            if (CalibrationItemTree.SelectedItem != null)
            {
                selected = (TreeViewItem)CalibrationItemTree.SelectedItem;
                UpdateDetails(database.GetCalItem("calibration_items", "serial_number", (string)selected.Header));
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
            string sn = "";
            if (DetailsSN.Text.Length > 0)
            {
                CalibrationItem item;
                item = database.GetCalItem("calibration_items", "serial_number", DetailsSN.Text);
                MessageBoxResult result = MessageBoxResult.None;
                if (item == null)
                {
                    item = new CalibrationItem(DetailsSN.Text);
                    result = MessageBox.Show($"Item SN: {DetailsSN.Text} does not exist in the database. Create this item?", "Item Not Found", MessageBoxButton.YesNo, MessageBoxImage.Information);
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
                UpdateListsSingle(item);
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
                Process.Start("explorer", directory);
            }
        }
        private void DetailsIntervalUp_Click(object sender, RoutedEventArgs e)
        {
            if (!DetailsIntervalBox.IsEnabled) { return; }
            int num;
            if (int.TryParse(DetailsIntervalBox.Text, out num))
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
            CalibrationItem calItem = database.GetCalItem("calibration_items", "serial_number", SelectedSN());
            CalDataEntry dataEntry = new CalDataEntry();
            dataEntry.SerialNumberBox.Text = SelectedSN();
            dataEntry.DateBox.Text = DateTime.UtcNow.ToString(database.dateFormat);
            dataEntry.ProcedureBox.ItemsSource = config.Procedures;
            dataEntry.EquipmentBox.ItemsSource = standardEquipment;
            if (dataEntry.ShowDialog() == true)
            {
                dataEntry.data.DueDate = dataEntry.data.CalibrationDate.Value.AddMonths(calItem.Interval);
                dataEntry.data.StandardEquipment = JsonConvert.SerializeObject(database.GetCalItem("calibration_items", "serial_number", dataEntry.EquipmentBox.Text));
                database.SaveCalData(dataEntry.data);
                UpdateItemList();
            }
        }
        private void DetailsStandardBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)DetailsStandardBox.IsChecked)
            {
                DetailsMandatory.IsChecked = true;
                DetailsMandatory.IsEnabled = false;
                DetailsCertificateLabel.Visibility = Visibility.Visible;
                DetailsCertificateNum.Visibility = Visibility.Visible;
            }
            else
            {
                if (DetailsSN.IsEnabled) { DetailsMandatory.IsEnabled = true; }
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
                if(MessageBox.Show($"This will delete {selectedItem} from the database. Any calibration records will remain. Continue?",
                    "Delete Item",MessageBoxButton.YesNo,MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    database.RemoveCalItem(selectedItem);
                    UpdateItemList();
                }
            }
        }

        private void SearchOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchBox.Clear();
            string selection = SearchOptions.SelectedItem.ToString();
            if (selection == "Calibration Due" | selection == "Has Comment" | selection == "Standard Equipment")
            {
                AddItemsToList(ItemListFilter(searchModes[selection],""));
                SearchBox.IsEnabled = false;
                ExpandTreeItems();
            }
            else { SearchBox.IsEnabled = true; AddItemsToList(database.GetAllCalItems()); }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddItemsToList(ItemListFilter(searchModes[SearchOptions.SelectedItem.ToString()], SearchBox.Text));
            ExpandTreeItems();
        }
    }
}
