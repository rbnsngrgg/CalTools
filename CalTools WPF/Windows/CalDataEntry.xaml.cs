﻿using CalTools_WPF.ObjectClasses;
using System;
using System.Collections.Generic;
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
    public partial class CalDataEntry : Window
    {
        public TaskData data = new();
        public List<Findings> parameters = new();
        public List<CTStandardEquipment> standardEquipment = new();
        public CalDataEntry()
        {
            InitializeComponent();
            Binding paramBinding = new();
            paramBinding.Source = parameters;
            paramBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            FindingsDataGrid.SetBinding(DataGrid.ItemsSourceProperty, paramBinding);

            Binding filesParamBinding = new();
            filesParamBinding.Source = data.DataFiles;
            filesParamBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            FilesDataGrid.SetBinding(DataGrid.ItemsSourceProperty, filesParamBinding);

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
                Operational = (bool)OperationalBox1.IsChecked
            };
            data.StateAfter = new State
            {
                InTolerance = (bool)InToleranceBox2.IsChecked,
                Operational = (bool)OperationalBox2.IsChecked
            };
            data.Actions = new ActionTaken
            {
                Calibration = (bool)CalibrationBox.IsChecked,
                Verification = (bool)VerificationBox.IsChecked,
                Adjusted = (bool)AdjustedBox.IsChecked,
                Repaired = (bool)RepairedBox.IsChecked,
                Maintenance = false
            };
            if(!(bool)CalibrationBox.IsChecked &&
                !(bool)VerificationBox.IsChecked &&
                !(bool)AdjustedBox.IsChecked &&
                !(bool)RepairedBox.IsChecked)
            {
                MessageBox.Show("An action must be selected.", "Action Taken", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            DateTime calDate;
            if (!DateTime.TryParseExact(DateBox.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out calDate))
            { MessageBox.Show("The calibration date entered isn't valid.", "Date Format", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }

            if (calDate > DateTime.UtcNow)
            { MessageBox.Show("Entries for future dates are not allowed.", "Future Date", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.CompleteDate = calDate;
            if (ProcedureBox.Text.Length == 0) { MessageBox.Show("\"Procedure\" is required.", "Required Field", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            else { data.Procedure = ProcedureBox.Text; }

            if (!IsStandardEquipmentSelected())
            { if (MessageBox.Show("No Standard Equipment is selected. Continue?", "Blank Field", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No) { return false; } }
            AssignStandardEquipment();

            data.Findings.Clear();
            data.Findings.AddRange(parameters);

            if (RemarksBox.Text.Length == 0 && parameters.Count == 0 && FilesDataGrid.Items.Count == 0)
            { MessageBox.Show("Remarks are required if there are no findings or data files.", "Remarks", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
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
                InTolerance = (bool)OperationalBox1.IsChecked,
                Operational = (bool)OperationalBox1.IsChecked
            };
            data.StateAfter = new State
            {
                InTolerance = (bool)OperationalBox2.IsChecked,
                Operational = (bool)OperationalBox2.IsChecked
            };
            if((bool)RepairedBox.IsChecked == false && (bool)MaintenanceBox.IsChecked == false)
            { MessageBox.Show("An \"Action Taken\" selection is required", "Action Taken", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.Actions = new ActionTaken
            {
                Calibration = false,
                Verification = false,
                Adjusted = false,
                Repaired = (bool)RepairedBox.IsChecked,
                Maintenance = (bool)MaintenanceBox.IsChecked
            };
            DateTime calDate;
            if (!DateTime.TryParseExact(DateBox.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out calDate))
            { MessageBox.Show("The calibration date entered isn't valid.", "Date Format", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }

            if (calDate > DateTime.UtcNow)
            { MessageBox.Show("Entries for future dates are not allowed.", "Future Date", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            data.CompleteDate = calDate;
            if (ProcedureBox.Text.Length == 0) { MessageBox.Show("\"Procedure\" is required.", "Required Field", MessageBoxButton.OK, MessageBoxImage.Exclamation); return false; }
            else { data.Procedure = ProcedureBox.Text; }

            AssignStandardEquipment();

            data.Findings.Clear();
            data.Findings.AddRange(parameters);

            if (RemarksBox.Text.Length == 0 && FilesDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Remarks are required if there are no data files.", "Remarks", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
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
            else if (MaintenanceSelection.IsSelected)
            { if (SaveMaintenanceData()) { this.DialogResult = true; } }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
        private void AddParameter_Click(object sender, RoutedEventArgs e)
        {
            parameters.Add(new Findings($"Parameter {parameters.Count + 1}"));
            FindingsDataGrid.Items.Refresh();
        }
        private void RemoveParameter_Click(object sender, RoutedEventArgs e)
        {
            Findings selectedItem = (Findings)FindingsDataGrid.SelectedItem;
            if (selectedItem != null) { parameters.Remove(selectedItem); }
            FindingsDataGrid.Items.Refresh();
        }
        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new();
            dlg.CheckFileExists = false;
            string defaultFileName = "Select this folder";
            dlg.FileName = defaultFileName;
            if(dlg.ShowDialog().Value)
            {
                if(System.IO.Path.GetFileName(dlg.FileName) == defaultFileName)
                {
                    string folder = System.IO.Directory.GetParent(dlg.FileName).FullName;
                    if(System.IO.Directory.Exists(folder))
                    {
                        data.DataFiles.Add(new TaskDataFile() { Location = folder });
                    }
                }
                else if (System.IO.File.Exists(dlg.FileName))
                {
                    data.DataFiles.Add(new TaskDataFile() { Location = dlg.FileName });
                }
                FilesDataGrid.Items.Refresh();
            }

        }
        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            TaskDataFile selectedItem = (TaskDataFile)FilesDataGrid.SelectedItem;
            if (selectedItem != null) { data.DataFiles.Remove(selectedItem); }
            FilesDataGrid.Items.Refresh();
        }

        //Only numbers in the date box, auto format date
        private void DateBox_PreviewTextInput(object sender, TextCompositionEventArgs e) //Ignore non-numerics, auto-add date formatting characters
        {
            if (!IsNumber(e.Text) | DateBox.Text.Length == 10) { e.Handled = true; }
            if (DateBox.Text.Length == 4 | DateBox.Text.Length == 7) { DateBox.Text += "-"; DateBox.CaretIndex = DateBox.Text.Length; }
        } 
        private void DateBox_PreviewKeyDown(object sender, KeyEventArgs e) //Ignore space, auto-remove date formatting characters on backspace
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
                if (CalibrationSelection.IsSelected)
                { if (SaveCalibrationData()) { e.Handled = true; this.DialogResult = true; } }
                else if (MaintenanceSelection.IsSelected)
                { if (SaveMaintenanceData()) { e.Handled = true; this.DialogResult = true; } }
            }
        }

        //Change the displayed form
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

        //Misc
        private bool IsStandardEquipmentSelected()
        {
            for (int i = 0; i < EquipmentDataGrid.Items.Count; i++)
            {
                CTStandardEquipment item = (CTStandardEquipment)EquipmentDataGrid.Items[i];
                CheckBox checkBox = EquipmentDataGrid.Columns[0].GetCellContent(item) as CheckBox;
                if ((bool)checkBox.IsChecked)
                {
                    return true;
                }
            }
            return false;
        }
        private void AssignStandardEquipment()
        {
            data.StandardEquipment.Clear();
            for (int i = 0; i < EquipmentDataGrid.Items.Count; i++)
            {
                CTStandardEquipment item = (CTStandardEquipment)EquipmentDataGrid.Items[i];
                CheckBox checkBox = EquipmentDataGrid.Columns[0].GetCellContent(item) as CheckBox;
                if ((bool)checkBox.IsChecked)
                {
                    data.StandardEquipment.Add(item);
                }
            }
        }
    }
}
