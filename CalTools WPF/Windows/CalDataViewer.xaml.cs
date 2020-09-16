using CalTools_WPF.ObjectClasses;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
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
        public TaskData data = new TaskData();
        public Findings findings = new Findings();
        public CalDataViewer(TaskData inputData)
        {
            InitializeComponent();
            data = inputData;

            if (data.findings != null)
            {
                Binding paramBinding = new Binding();
                paramBinding.Source = data.findings.parameters;
                paramBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                FindingsDataGrid.SetBinding(DataGrid.ItemsSourceProperty, paramBinding);
            }

            if (((ActionTaken)data.ActionTaken).Maintenance) 
            {
                MaintenanceSelection.IsSelected = true;
                CalibrationDataPanel.Visibility = Visibility.Collapsed;
                MaintenanceDataPanel.Visibility = Visibility.Visible;
                FillMaintenanceForm(); 
            }
            else if(((ActionTaken)data.ActionTaken).Calibration | ((ActionTaken)data.ActionTaken).Verification) 
            {
                CalibrationSelection.IsSelected = true;
                CalibrationDataPanel.Visibility = Visibility.Visible;
                MaintenanceDataPanel.Visibility = Visibility.Collapsed;
                FillCalForm();
            }
        }

        private void FillCalForm()
        {
            SerialNumberBox.Text = data.SerialNumber;
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
            EquipmentBox.Text = standardEquipment.SerialNumber;
            findings = data.findings;
            RemarksBox.Text = data.Remarks;
            TechnicianBox.Text = data.Technician;
        }
        private void FillMaintenanceForm()
        {
            MaintenanceSerialNumberBox.Text = data.SerialNumber;
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Delete this calibration/maintenance data from the database?", "Delete Data", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes) 
            { this.DialogResult = true; } 
            
        }
    }
}
