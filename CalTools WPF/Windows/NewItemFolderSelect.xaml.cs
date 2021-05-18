using System.Windows;
using System.Windows.Controls;

namespace CalTools_WPF
{
    /// <summary>
    /// Interaction logic for NewItemFolderSelect.xaml
    /// </summary>
    public partial class NewItemFolderSelect : Window
    {
        private bool FolderSelected = false;
        private bool SnEntered = false;
        public NewItemFolderSelect(string serialNumber = "")
        {
            InitializeComponent();
        }

        private void FolderSelectOK_Click(object sender, RoutedEventArgs e) => this.DialogResult = true;
        private void FolderSelectCancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;

        //Enable the OK button once a selection is made
        private void FolderSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FolderSelected = true;
            ButtonEnable();
        }


        private void FolderSelectSerialNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(FolderSelectSerialNumber.Text != "")
            {
                SnEntered = true;
            }
            else
            {
                SnEntered = false;
            }
            ButtonEnable();
        }
        private void ButtonEnable()
        {
            FolderSelectOK.IsEnabled = SnEntered & FolderSelected;
        }
    }
}
