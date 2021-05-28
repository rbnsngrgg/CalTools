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
        readonly List<TaskData> taskDataList;
        private readonly CTTask currentTask;
        public Findings findings = new();
        public CalDataViewer(ref List<TaskData> inputData, CTTask task)
        {
            InitializeComponent();
            taskDataList = inputData;
            currentTask = task;
            taskDataList.Sort((y, x) => x.CompleteDateString.CompareTo(y.CompleteDateString));
            foreach (TaskData data in taskDataList)
            {
                TreeViewItem newItem = new();
                newItem.Header = data.CompleteDateString;
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
                paramBinding.Source = data.Findings.parameters;
                paramBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                FindingsDataGrid.SetBinding(DataGrid.ItemsSourceProperty, paramBinding);
            }

            if (((ActionTaken)data.ActionTaken).Maintenance)
            {
                MaintenanceSelection.IsSelected = true;
                CalibrationDataPanel.Visibility = Visibility.Collapsed;
                MaintenanceDataPanel.Visibility = Visibility.Visible;
                FillMaintenanceForm(data);
            }
            else if (((ActionTaken)data.ActionTaken).Calibration | ((ActionTaken)data.ActionTaken).Verification)
            {
                CalibrationSelection.IsSelected = true;
                CalibrationDataPanel.Visibility = Visibility.Visible;
                MaintenanceDataPanel.Visibility = Visibility.Collapsed;
                FillCalForm(data);
            }
        }
        private void FillCalForm(TaskData data)
        {
            SerialNumberBox.Text = data.SerialNumber;
            TaskBox.Text = $"({data.TaskID})";
            InToleranceBox1.IsChecked = ((State)data.StateBefore).InTolerance;
            OutOfToleranceBox1.IsChecked = ((State)data.StateBefore).OutOfTolerance;
            MalfunctioningBox1.IsChecked = ((State)data.StateBefore).Malfunctioning;
            OperationalBox1.IsChecked = ((State)data.StateBefore).Operational;

            InToleranceBox2.IsChecked = ((State)data.StateAfter).InTolerance;
            OutOfToleranceBox2.IsChecked = ((State)data.StateAfter).OutOfTolerance;
            MalfunctioningBox2.IsChecked = ((State)data.StateAfter).Malfunctioning;
            OperationalBox2.IsChecked = ((State)data.StateAfter).Operational;

            CalibrationBox.IsChecked = ((ActionTaken)data.ActionTaken).Calibration;
            VerificationBox.IsChecked = ((ActionTaken)data.ActionTaken).Verification;
            AdjustedBox.IsChecked = ((ActionTaken)data.ActionTaken).Adjusted;
            RepairedBox.IsChecked = ((ActionTaken)data.ActionTaken).Repaired;

            DateBox.Text = data.CompleteDate.Value.ToString("yyyy-MM-dd");
            ProcedureBox.Text = data.Procedure;
            CTItem standardEquipment = JsonConvert.DeserializeObject<CTItem>(data.StandardEquipment);
            if (standardEquipment != null) { EquipmentBox.Text = standardEquipment.SerialNumber; }
            findings = data.Findings;
            RemarksBox.Text = data.Remarks;
            TechnicianBox.Text = data.Technician;
        }
        private void FillMaintenanceForm(TaskData data)
        {
            MaintenanceSerialNumberBox.Text = data.SerialNumber;
            MaintenanceTaskBox.Text = $"({data.TaskID})";
            MaintenanceMalfunctioningBox1.IsChecked = ((State)data.StateBefore).Malfunctioning;
            MaintenanceOperationalBox1.IsChecked = ((State)data.StateBefore).Operational;
            MaintenanceMalfunctioningBox2.IsChecked = ((State)data.StateAfter).Malfunctioning;
            MaintenanceOperationalBox2.IsChecked = ((State)data.StateAfter).Operational;

            MaintenanceBox.IsChecked = ((ActionTaken)data.ActionTaken).Maintenance;
            MaintenanceRepairedBox.IsChecked = ((ActionTaken)data.ActionTaken).Repaired;

            MaintenanceDateBox.Text = data.CompleteDate.Value.ToString("yyyy-MM-dd");
            MaintenanceProcedureBox.Text = data.Procedure;

            CTItem standardEquipment = JsonConvert.DeserializeObject<CTItem>(data.StandardEquipment);
            if (standardEquipment != null) { MaintenanceEquipmentBox.Text = standardEquipment.SerialNumber; }

            MaintenanceRemarksBox.Text = data.Remarks;
            MaintenanceTechnicianBox.Text = data.Technician;
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
                        string currentItemCompleteDate = ((TreeViewItem)((TreeView)sender).SelectedItem).Header.ToString();
                        if (data.CompleteDateString == currentItemCompleteDate)
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
                        string currentItemCompleteDate = ((TreeViewItem)TaskDataTree.SelectedItem).Header.ToString();
                        if (data.CompleteDateString == currentItemCompleteDate)
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
                    if (file.Contains(((TreeViewItem)TaskFilesTree.SelectedItem).Header.ToString()))
                    {
                        TaskFilesTree.Items.Remove(TaskFilesTree.SelectedItem);
                        TaskFilesTree.Items.Refresh();
                        File.Delete(file);
                        break;
                    }
                }
            }
        }
    }
}
