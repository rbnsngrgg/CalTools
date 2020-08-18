using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CalTools_WPF
{
    /// <summary>
    /// Interaction logic for NewItemFolderSelect.xaml
    /// </summary>
    public partial class NewItemFolderSelect : Window
    {
        public NewItemFolderSelect()
        {
            InitializeComponent();
        }

        private void FolderSelectOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void FolderSelectCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        //Enable the OK button once a selection is made
        private void FolderSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!FolderSelectOK.IsEnabled) { FolderSelectOK.IsEnabled = true; }
        }
    }
}
