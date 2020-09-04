using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CalTools_WPF.Windows
{
    /// <summary>
    /// Interaction logic for DropFileInfo.xaml
    /// </summary>
    public partial class DropFileInfo : Window
    {
        public DropFileInfo()
        {
            InitializeComponent();
        }

        private void InfoOKButton_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParseExact(DateBox.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _))
            { this.DialogResult = true; }
            else
            { MessageBox.Show("The date is not in a valid \"yyyy-MM-dd\" format","Date Format",MessageBoxButton.OK,MessageBoxImage.Exclamation); }
        }

        private void InfoCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
