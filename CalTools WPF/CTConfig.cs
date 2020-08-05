using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Dynamic;
using System.Windows;
using System.Diagnostics;

namespace CalTools_WPF
{
    class CTConfig
    {
        private string configName = "CTConfig.xml";
        //Values to be loaded from config
        public string DbName { get; set; }
        public string DbPath { get; set; }
        public string CalListDir { get; set; }
        public string TempFilesDir { get; set; }
        public string CalScansDir { get; set; }
        public bool FirstRun { get; set; }
        public int MarkCalDue { get; set; }
        public int DueInCalendar { get; set; }
        public string CerificateFileName { get; set; }
        public Dictionary<string,string> ReportCells { get; set; }

        public CTConfig()
        {
            ReportCells = new Dictionary<string, string>();
        }

        public bool CreateConfig(string configPath)
        {
            try
            {
                //Generate file and write first lines
                string[] lines = { "<CalTools_Config FirstRun = \"True\">",
                        "\t<Database DbName = \"debug_Test Equipment Calibration List.db\"/>",
                        "\t<Directories CalListDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\" " +
                        "TempFilesDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Template Files\" " +
                        "CalScansDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Calibration Scans\"/>",
                        "\t<DueItems MarkCalDue = \"30\" DueInCalendar = \"60\"/>",
                        "\t<Report_Template " +
                        "CertificateFileName = \"ReportTemplate.xlsx\" " +
                        "ManufacturerCell = \"C5\" " +
                        "ModelCell = \"C6\" " +
                        "SerialNumberCell = \"C7\" " +
                        "DescriptionCell = \"C8\" " +
                        "CalibrationCell = \"J12\" " +
                        "VerificationCell = \"J13\" " +
                        "CalibrationDateCell = \"D18\" " +
                        "OperationDateCell = \"D19\" " +
                        "DueDateCell = \"D20\" " +
                        "ProcedureCell = \"I18\" " +
                        "LocationCell = \"I19\" " +
                        "CertDateCell = \"J42\"/>",
                    "</CalTools_Config>" };
                File.WriteAllLines(configPath, lines);
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error creating config file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public void LoadConfig()
        {
            string configPath = $"{Directory.GetCurrentDirectory()}\\{configName}";
            if (!File.Exists(configPath))
            {
                CreateConfig(configPath);
            }
            try
            {
                XmlDocument config = new XmlDocument();
                config.Load(configPath);

                FirstRun = bool.Parse(config.FirstChild.Attributes[0].Value);
                DbName = config.FirstChild.ChildNodes[0].Attributes[0].Value;
                CalListDir = config.FirstChild.ChildNodes[1].Attributes[0].Value;
                TempFilesDir = config.FirstChild.ChildNodes[1].Attributes[1].Value;
                CalScansDir = config.FirstChild.ChildNodes[1].Attributes[2].Value;
                MarkCalDue = int.Parse(config.FirstChild.ChildNodes[2].Attributes[0].Value);
                DueInCalendar = int.Parse(config.FirstChild.ChildNodes[2].Attributes[1].Value);

                ReportCells.Add("CerificateFileName", config.FirstChild.ChildNodes[3].Attributes[0].Value);
                ReportCells.Add("ManufacturerCell", config.FirstChild.ChildNodes[3].Attributes[1].Value);
                ReportCells.Add("ModelCell", config.FirstChild.ChildNodes[3].Attributes[2].Value);
                ReportCells.Add("SerialNumberCell", config.FirstChild.ChildNodes[3].Attributes[3].Value);
                ReportCells.Add("DescriptionCell", config.FirstChild.ChildNodes[3].Attributes[4].Value);
                ReportCells.Add("CalibrationCell", config.FirstChild.ChildNodes[3].Attributes[5].Value);
                ReportCells.Add("VerificationCell", config.FirstChild.ChildNodes[3].Attributes[6].Value);
                ReportCells.Add("CalibrationDateCell", config.FirstChild.ChildNodes[3].Attributes[7].Value);
                ReportCells.Add("OperationDateCell", config.FirstChild.ChildNodes[3].Attributes[8].Value);
                ReportCells.Add("DueDateCell", config.FirstChild.ChildNodes[3].Attributes[9].Value);
                ReportCells.Add("ProcedureCell", config.FirstChild.ChildNodes[3].Attributes[10].Value);
                ReportCells.Add("LocationCell", config.FirstChild.ChildNodes[3].Attributes[11].Value);
                ReportCells.Add("CertDateCell", config.FirstChild.ChildNodes[3].Attributes[12].Value);

                DbPath = Path.Combine(CalListDir,DbName);
            }
            catch(System.Exception ex)
            {
                MessageBox.Show($"Error loading config file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
