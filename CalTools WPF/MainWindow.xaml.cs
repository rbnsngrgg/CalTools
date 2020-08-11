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
            CalibrationItemTree.Items.Clear();
            List<CalibrationItem> items = database.GetAllItems("calibration_items");
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
            DetailsSN.Text = item.SerialNumber;
            DetailsModel.Text = item.Model;
            DetailsDescription.Text = item.Description;
            DetailsLocation.Text = item.Location;
            DetailsManufacturer.Text = item.Manufacturer;
            switch(item.VerifyOrCalibrate)
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
            if (item.LastCal != null) { DetailsLastCal.Text = item.LastCal.Value.ToString("yyyy-MM-dd"); } else { DetailsLastCal.Clear(); }
            if (item.InServiceDate != null) { DetailsOperationDate.Text = item.InServiceDate.Value.ToString("yyyy-MM-dd"); } else { DetailsOperationDate.Clear(); }
            if (item.NextCal != null) { DetailsNextCal.Text = item.NextCal.Value.ToString("yyyy-MM-dd"); } else { DetailsNextCal.Clear(); }
            DetailsMandatory.IsChecked = item.Mandatory;
            DetailsInOperation.IsChecked = item.InService;
            DetailsItemGroup.Text = item.ItemGroup;
            DetailsComments.Text = item.Comment;
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
            EditItemButton.Visibility = Visibility.Collapsed;
            DetailsEditToggle();
            SaveItemButton.Visibility = Visibility.Visible;
        }

        private void SaveItemButton_Click(object sender, RoutedEventArgs e)
        {
            SaveItemButton.Visibility = Visibility.Collapsed;
            DetailsEditToggle();
            EditItemButton.Visibility = Visibility.Visible;
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
    }
}
