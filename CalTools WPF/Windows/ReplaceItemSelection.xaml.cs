﻿using System;
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