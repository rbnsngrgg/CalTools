using System;
using System.Globalization;
using System.Windows;

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
            { MessageBox.Show("The date is not in a valid \"yyyy-MM-dd\" format", "Date Format", MessageBoxButton.OK, MessageBoxImage.Exclamation); }
        }

        private void InfoCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TaskBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TaskBox.SelectedItem == null) { InfoOKButton.IsEnabled = false; }
            else { InfoOKButton.IsEnabled = true; }
        }
    }
}
