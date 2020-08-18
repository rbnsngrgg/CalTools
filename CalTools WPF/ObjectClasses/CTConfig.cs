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
        public Dictionary<string, string> ReportCells { get; set; } = new Dictionary<string, string>();
        public List<string> Folders { get; set; } = new List<string>();
        public List<string> Procedures { get; set; } = new List<string>();

        //TODO: Add list of procedures to config, to be selected from when creating a new CalibrationData object
        public bool CreateConfig(string configPath)
        {
            try
            {
                //Generate file and write first lines
                string[] lines = { "<CalTools_Config FirstRun = \"True\" Folders = \"PRODUCTION EQUIPMENT,ENGINEERING EQUIPMENT,QUALITY EQUIPMENT,Ref Only,Removed from Service\"" +
                        " Procedures = \"019-0065\">",
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
                Folders.AddRange(config.LastChild.Attributes[1].Value.Split(","));
                Procedures.AddRange(config.LastChild.Attributes[2].Value.Split(","));
                FirstRun = bool.Parse(config.LastChild.Attributes[0].Value);
                DbName = config.LastChild.ChildNodes[0].Attributes[0].Value;
                CalListDir = config.LastChild.ChildNodes[1].Attributes[0].Value;
                TempFilesDir = config.LastChild.ChildNodes[1].Attributes[1].Value;
                CalScansDir = config.LastChild.ChildNodes[1].Attributes[2].Value;
                MarkCalDue = int.Parse(config.LastChild.ChildNodes[2].Attributes[0].Value);
                DueInCalendar = int.Parse(config.LastChild.ChildNodes[2].Attributes[1].Value);

                ReportCells.Add("CerificateFileName", config.LastChild.ChildNodes[3].Attributes[0].Value);
                ReportCells.Add("ManufacturerCell", config.LastChild.ChildNodes[3].Attributes[1].Value);
                ReportCells.Add("ModelCell", config.LastChild.ChildNodes[3].Attributes[2].Value);
                ReportCells.Add("SerialNumberCell", config.LastChild.ChildNodes[3].Attributes[3].Value);
                ReportCells.Add("DescriptionCell", config.LastChild.ChildNodes[3].Attributes[4].Value);
                ReportCells.Add("CalibrationCell", config.LastChild.ChildNodes[3].Attributes[5].Value);
                ReportCells.Add("VerificationCell", config.LastChild.ChildNodes[3].Attributes[6].Value);
                ReportCells.Add("CalibrationDateCell", config.LastChild.ChildNodes[3].Attributes[7].Value);
                ReportCells.Add("OperationDateCell", config.LastChild.ChildNodes[3].Attributes[8].Value);
                ReportCells.Add("DueDateCell", config.LastChild.ChildNodes[3].Attributes[9].Value);
                ReportCells.Add("ProcedureCell", config.LastChild.ChildNodes[3].Attributes[10].Value);
                ReportCells.Add("LocationCell", config.LastChild.ChildNodes[3].Attributes[11].Value);
                ReportCells.Add("CertDateCell", config.LastChild.ChildNodes[3].Attributes[12].Value);

                Debug.WriteLine(Procedures[0]);
                Debug.WriteLine(CalScansDir);

                DbPath = Path.Combine(CalListDir,DbName);
            }
            catch(System.Exception ex)
            {
                MessageBox.Show($"Error loading config file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
