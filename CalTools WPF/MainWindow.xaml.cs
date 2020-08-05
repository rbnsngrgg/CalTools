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
namespace CalTools_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CTDatabase database;
        private CTConfig config = new CTConfig();
        public MainWindow()
        {
            config.LoadConfig();
            database = new CTDatabase(config.DbPath);
            InitializeComponent();
            UpdateItemList();
        }

        //Update GUI Elements
        private void UpdateItemList()
        {
            CalibrationItemList.Items.Clear();
            List<CalibrationItem> items = database.GetAllItems("calibration_items");
            foreach(var item in items)
            {
                TreeViewItem treeItem = new TreeViewItem();
                treeItem.Header = item.SerialNumber;
                CalibrationItemList.Items.Add(treeItem);
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
    }
}
