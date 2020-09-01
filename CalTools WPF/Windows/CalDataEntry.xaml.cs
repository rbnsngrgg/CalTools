﻿using CalTools_WPF.ObjectClasses;
using System;
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
    public partial class CalDataEntry : Window
    {
        public CalibrationData data = new CalibrationData();
        public Findings findings = new Findings();
        public CalDataEntry()
        {
            InitializeComponent();
            Binding paramBinding = new Binding();
            paramBinding.Source = findings.parameters;
            paramBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            FindingsDataGrid.SetBinding(DataGrid.ItemsSourceProperty, paramBinding);

            OperationalBox1.IsChecked = true;
            InToleranceBox1.IsChecked = true;
            OperationalBox2.IsChecked = true;
            InToleranceBox2.IsChecked = true;
            CalibrationBox.IsChecked = true;
        }

        private bool SaveCalibrationData()
        {
            data.SerialNumber = SerialNumberBox.Text;
            data.StateBefore = new State
            {
                InTolerance = (bool)InToleranceBox1.IsChecked,
                OutOfTolerance = (bool)OutOfToleranceBox1.IsChecked,
                Malfunctioning = (bool)MalfunctioningBox1.IsChecked,
                Operational = (bool)OperationalBox1.IsChecked
            };
            data.StateAfter = new State
            {
                InTolerance = (bool)InToleranceBox2.IsChecked,
                OutOfTolerance = (bool)OutOfToleranceBox2.IsChecked,
                Malfunctioning = (bool)MalfunctioningBox2.IsChecked,
                Operational = (bool)OperationalBox2.IsChecked
            };
            data.ActionTaken = new ActionTaken
            {
                Calibration = (bool)CalibrationBox.IsChecked,
                Verification = (bool)VerificationBox.IsChecked,
                Adjusted = (bool)AdjustedBox.IsChecked,
                Repaired = (bool)RepairedBox.IsChecked
            };
            DateTime calDate;
            if (!DateTime.TryParseExact(DateBox.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out calDate))
            { MessageBox.Show("The calibration date entered isn't in a valid \"yyyy-MM-dd\" format.", "Date Format", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }

            if (calDate > DateTime.UtcNow)
            { MessageBox.Show("Entries for future dates are not allowed.", "Future Date", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.CalibrationDate = calDate;
            if (ProcedureBox.Text.Length == 0) { MessageBox.Show("\"Procedure\" is required.", "Required Field", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            else { data.Procedure = ProcedureBox.Text; }

            if (EquipmentBox.Text.Length == 0)
            { if (MessageBox.Show("\"Standard Equipment\" is blank. Continue?", "Blank Field", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No) { return false; } }
            else { data.StandardEquipment = EquipmentBox.Text; }
            data.findings = findings;
            if (RemarksBox.Text.Length == 0 & findings.parameters.Count == 0)
            { MessageBox.Show("Remarks are required if there are no findings parameters.", "Remarks", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.Remarks = RemarksBox.Text;
            if (TechnicianBox.Text.Length == 0) { MessageBox.Show("A technician name is required", "Technician Required", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.Technician = TechnicianBox.Text;
            //ID is auto-generated by sqlite, duedate is calculated upon database entry using the interval
            return true;
        }
        private bool SaveMaintenanceData()
        {
            data.SerialNumber = SerialNumberBox.Text;
            data.StateBefore = new State
            {
                InTolerance = (bool)InToleranceBox1.IsChecked,
                OutOfTolerance = (bool)OutOfToleranceBox1.IsChecked,
                Malfunctioning = (bool)MalfunctioningBox1.IsChecked,
                Operational = (bool)OperationalBox1.IsChecked
            };
            data.StateAfter = new State
            {
                InTolerance = (bool)InToleranceBox2.IsChecked,
                OutOfTolerance = (bool)OutOfToleranceBox2.IsChecked,
                Malfunctioning = (bool)MalfunctioningBox2.IsChecked,
                Operational = (bool)OperationalBox2.IsChecked
            };
            data.ActionTaken = new ActionTaken
            {
                Calibration = (bool)CalibrationBox.IsChecked,
                Verification = (bool)VerificationBox.IsChecked,
                Adjusted = (bool)AdjustedBox.IsChecked,
                Repaired = (bool)RepairedBox.IsChecked
            };
            DateTime calDate;
            if (!DateTime.TryParseExact(DateBox.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out calDate))
            { MessageBox.Show("The calibration date entered isn't in a valid \"yyyy-MM-dd\" format.", "Date Format", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }

            if (calDate > DateTime.UtcNow)
            { MessageBox.Show("Entries for future dates are not allowed.", "Future Date", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.CalibrationDate = calDate;
            if (ProcedureBox.Text.Length == 0) { MessageBox.Show("\"Procedure\" is required.", "Required Field", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            else { data.Procedure = ProcedureBox.Text; }

            if (EquipmentBox.Text.Length == 0)
            { if (MessageBox.Show("\"Standard Equipment\" is blank. Continue?", "Blank Field", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No) { return false; } }
            else { data.StandardEquipment = EquipmentBox.Text; }
            data.findings = findings;
            if (RemarksBox.Text.Length == 0 & findings.parameters.Count == 0)
            { MessageBox.Show("Remarks are required if there are no findings parameters.", "Remarks", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.Remarks = RemarksBox.Text;
            if (TechnicianBox.Text.Length == 0) { MessageBox.Show("A technician name is required", "Technician Required", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.Technician = TechnicianBox.Text;
            //ID is auto-generated by sqlite, duedate is calculated upon database entry using the interval
            return true;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalibrationSelection.IsSelected)
            { if (SaveCalibrationData()) { this.DialogResult = true; } }
            else if(MaintenanceSelection.IsSelected)
                { if (SaveMaintenanceData()) { this.DialogResult = true; } }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void AddParameter_Click(object sender, RoutedEventArgs e)
        {
            findings.parameters.Add(new Param($"Parameter {findings.parameters.Count + 1}"));
            FindingsDataGrid.Items.Refresh();
        }

        private void RemoveParameter_Click(object sender, RoutedEventArgs e)
        {
            Param selectedItem = (Param)FindingsDataGrid.SelectedItem;
            if (selectedItem != null) { findings.parameters.Remove(selectedItem); }
            FindingsDataGrid.Items.Refresh();
        }
        //Only numbers in the date box, auto format date
        private void DateBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsNumber(e.Text) | DateBox.Text.Length == 10) { e.Handled = true; }
            if (DateBox.Text.Length == 4 | DateBox.Text.Length == 7) { DateBox.Text += "-"; DateBox.CaretIndex = DateBox.Text.Length; }
        }

        private void DateBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) { e.Handled = true; }
            else if (e.Key == Key.Back)
            {
                if (DateBox.Text.Length == 6 | DateBox.Text.Length == 9)
                {
                    DateBox.Text = DateBox.Text.Remove(DateBox.Text.Length - 2, 1);
                    DateBox.CaretIndex = DateBox.Text.Length;
                }
            }
        }

        private bool IsNumber(string n)
        {
            string digits = "0123456789";
            if (digits.Contains(n[0])) { return true; }
            else { return false; }
        }

        //Flip mutually-exclusive checkboxes
        private void InToleranceBox1_Checked(object sender, RoutedEventArgs e)
        {
            OutOfToleranceBox1.IsChecked = !(bool)InToleranceBox1.IsChecked;
        }

        private void OutOfToleranceBox1_Checked(object sender, RoutedEventArgs e)
        {
            InToleranceBox1.IsChecked = !(bool)OutOfToleranceBox1.IsChecked;
        }

        private void MalfunctioningBox1_Checked(object sender, RoutedEventArgs e)
        {
            OperationalBox1.IsChecked = !(bool)MalfunctioningBox1.IsChecked;
        }

        private void OperationalBox1_Checked(object sender, RoutedEventArgs e)
        {
            MalfunctioningBox1.IsChecked = !(bool)OperationalBox1.IsChecked;
        }

        private void InToleranceBox2_Checked(object sender, RoutedEventArgs e)
        {
            OutOfToleranceBox2.IsChecked = !(bool)InToleranceBox2.IsChecked;
        }

        private void OutOfToleranceBox2_Checked(object sender, RoutedEventArgs e)
        {
            InToleranceBox2.IsChecked = !(bool)OutOfToleranceBox2.IsChecked;
        }

        private void MalfunctioningBox2_Checked(object sender, RoutedEventArgs e)
        {
            OperationalBox2.IsChecked = !(bool)MalfunctioningBox2.IsChecked;
        }

        private void OperationalBox2_Checked(object sender, RoutedEventArgs e)
        {
            MalfunctioningBox2.IsChecked = !(bool)OperationalBox2.IsChecked;
        }

        private void TechnicianBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SaveCalibrationData()) { e.Handled = true; this.DialogResult = true; }
            }
        }

        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {

        }

        //Change the displayed form
        private void CalibrationSelection_Selected(object sender, RoutedEventArgs e)
        {
            if (CalibrationDataPanel != null)
            { CalibrationDataPanel.Visibility = Visibility.Visible; }
        }

        private void MaintenanceSelection_Selected(object sender, RoutedEventArgs e)
        {
            if (CalibrationDataPanel != null)
            { CalibrationDataPanel.Visibility = Visibility.Collapsed; }
        }
    }
}
