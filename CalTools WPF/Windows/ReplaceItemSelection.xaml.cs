using System.Windows;
using System.Windows.Controls;

namespace CalTools_WPF.Windows
{
    /// <summary>
    /// Interaction logic for ReplaceItemSelection.xaml
    /// </summary>
    public partial class ReplaceItemSelection : Window
    {
        public ReplaceItemSelection()
        {
            InitializeComponent();
        }

        private void ReplaceSelectOK_Click(object sender, RoutedEventArgs e) => this.DialogResult = true;

        private void ReplaceSelectCancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;

        private void ReplaceSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReplaceSelectOK.IsEnabled) { ReplaceSelectOK.IsEnabled = true; }
        }
    }
}
