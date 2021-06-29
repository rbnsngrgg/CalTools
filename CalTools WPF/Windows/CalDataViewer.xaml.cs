using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
namespace CalTools_WPF
{
    /// <summary>
    /// Interaction logic for CalDataEntry.xaml
    /// </summary>
    public partial class CalDataViewer : Window
    {
        private readonly List<TaskData> taskDataList;
        private readonly CTTask currentTask;
        public CalDataViewer(ref List<TaskData> inputData, CTTask task)
        {
            InitializeComponent();
            taskDataList = inputData;
            currentTask = task;
            taskDataList.Sort((y, x) => x.CompleteDateString
                .CompareTo(y.CompleteDateString));
            foreach (TaskData data in taskDataList)
            {
                TreeViewItem newItem = new();
                newItem.Header = $"({data.DataId}) {data.CompleteDateString}";
                TaskDataTree.Items.Add(newItem);
            }
            TaskDataTree.Items.Refresh();
            foreach (string file in Directory.GetFiles(currentTask.TaskDirectory))
            {
                TreeViewItem newItem = new();
                newItem.Header = Path.GetFileName(file);
                TaskFilesTree.Items.Add(newItem);
            }
            TaskFilesTree.Items.Refresh();
        }

        private void OpenForm(TaskData data)
        {
            if (data.Findings != null)
            {
                Binding paramBinding = new();
                paramBinding.Source = data.Findings;
                paramBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                FindingsDataGrid.SetBinding(DataGrid.ItemsSourceProperty, paramBinding);

                Binding filesParamBinding = new();
                filesParamBinding.Source = data.DataFiles;
                filesParamBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                FilesDataGrid.SetBinding(DataGrid.ItemsSourceProperty, filesParamBinding);

                Binding equipmentParamBinding = new();
                equipmentParamBinding.Source = data.StandardEquipment;
                equipmentParamBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                EquipmentDataGrid.SetBinding(DataGrid.ItemsSourceProperty, equipmentParamBinding);
            }

            if (((ActionTaken)data.Actions).Maintenance)
            {
                MaintenanceSelection.IsSelected = true;
            }
            else if (((ActionTaken)data.Actions).Calibration | ((ActionTaken)data.Actions).Verification)
            {
                CalibrationSelection.IsSelected = true;
            }
            FillForm(data);
        }
        private void FillForm(TaskData data)
        {
            SerialNumberBox.Text = data.SerialNumber;
            TaskBox.Text = $"({data.TaskId})";
            InToleranceBox1.IsChecked = ((State)data.StateBefore).InTolerance;
            OutOfToleranceBox1.IsChecked = !((State)data.StateBefore).InTolerance;
            MalfunctioningBox1.IsChecked = !((State)data.StateBefore).Operational;
            OperationalBox1.IsChecked = ((State)data.StateBefore).Operational;

            InToleranceBox2.IsChecked = ((State)data.StateAfter).InTolerance;
            OutOfToleranceBox2.IsChecked = !((State)data.StateAfter).InTolerance;
            MalfunctioningBox2.IsChecked = !((State)data.StateAfter).Operational;
            OperationalBox2.IsChecked = ((State)data.StateAfter).Operational;

            CalibrationBox.IsChecked = ((ActionTaken)data.Actions).Calibration;
            VerificationBox.IsChecked = ((ActionTaken)data.Actions).Verification;
            AdjustedBox.IsChecked = ((ActionTaken)data.Actions).Adjusted;
            RepairedBox.IsChecked = ((ActionTaken)data.Actions).Repaired;
            MaintenanceBox.IsChecked = ((ActionTaken)data.Actions).Maintenance;

            DateBox.Text = data.CompleteDateString;
            ProcedureBox.Text = data.Procedure;
            RemarksBox.Text = data.Remarks;
            TechnicianBox.Text = data.Technician;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Save all changes?", "Save Data", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            { this.DialogResult = true; }
        }
        private void TaskDataTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (((TreeView)sender).SelectedItem != null)
            {
                foreach (TaskData data in taskDataList)
                {
                    {
                        string currentItemHeader = ((TreeViewItem)((TreeView)sender).SelectedItem).Header.ToString();
                        if ($"({data.DataId}) {data.CompleteDateString}" == currentItemHeader)
                        { OpenForm(data); break; }
                    }
                }
            }
        }
        private void TaskFilesTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((TreeView)sender).SelectedItem != null)
            {
                string fileName = ((TreeViewItem)((TreeView)sender).SelectedItem).Header.ToString();
                foreach (string file in Directory.GetFiles(currentTask.TaskDirectory))
                {
                    if (file.Contains(fileName)) { Process.Start("explorer", file); }
                }
            }
        }
        private void TaskDataDeleteContext_Click(object sender, RoutedEventArgs e)
        {
            if (TaskDataTree.SelectedItem != null)
            {
                foreach (TaskData data in taskDataList)
                {
                    {
                        string currentItemHeader = ((TreeViewItem)TaskDataTree.SelectedItem).Header.ToString();
                        if ($"({data.DataId}) {data.CompleteDateString}" == currentItemHeader)
                        {
                            taskDataList.Remove(data);
                            TaskDataTree.Items.Remove(TaskDataTree.SelectedItem);
                            TaskDataTree.Items.Refresh();
                            break;
                        }
                    }
                }
            }
        }
        private void TaskFilesDeleteContext_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Delete this file? This operation cannot be undone after this point.", "Delete File", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                 != MessageBoxResult.Yes) { return; }
            if (TaskFilesTree.SelectedItem != null)
            {
                foreach (string file in Directory.GetFiles(currentTask.TaskDirectory))
                {
                    if (Path.GetFileName(file) == ((TreeViewItem)TaskFilesTree.SelectedItem).Header.ToString())
                    {
                        TaskFilesTree.Items.Remove(TaskFilesTree.SelectedItem);
                        TaskFilesTree.Items.Refresh();
                        File.Delete(file);
                        break;
                    }
                }
            }
        }

        private void CalibrationSelection_Selected(object sender, RoutedEventArgs e)
        {
            ToggleVisibility(Visibility.Visible, Visibility.Collapsed);
        }
        private void MaintenanceSelection_Selected(object sender, RoutedEventArgs e)
        {
            ToggleVisibility(Visibility.Collapsed, Visibility.Visible);
        }
        private void ToggleVisibility(Visibility v1, Visibility v2)
        {
            if (MainStackPanel != null)
            {
                InToleranceBox1.Visibility = v1;
                OutOfToleranceBox1.Visibility = v1;
                InToleranceBox2.Visibility = v1;
                OutOfToleranceBox2.Visibility = v1;
                CalibrationBox.Visibility = v1;
                VerificationBox.Visibility = v1;
                AdjustedBox.Visibility = v1;
                MaintenanceBox.Visibility = v2;
                FindingsPanel.Visibility = v1;
            }
        }
    }
}
