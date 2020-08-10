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
            database.Disconnect();
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
            foreach(var item in database.GetAllItems("calibration_items"))
            {
                if(e.NewValue.ToString().Contains(item.SerialNumber))
                {
                    UpdateDetails(item);
                    break;
                }
            }
            database.Disconnect();
        }

        //Prevent calendar from holding focus, requiring two clicks to escape
        private void ItemCalendar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(Mouse.Captured is CalendarItem)
            {
                Mouse.Capture(null);
            }
        }
    }
}
