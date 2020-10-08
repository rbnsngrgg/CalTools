using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;

namespace CalTools_WPF
{
    class CTConfig
    {
        private string configName = "CTConfig.xml";
        //Values to be loaded from config
        public string DbName { get; set; }
        public string DbPath { get; set; }
        public string ListDir { get; set; }
        public string TempFilesDir { get; set; }
        public string ItemScansDir { get; set; }
        public string Theme { get; set; }
        public int MarkDueDays { get; set; }
        public int DueInCalendar { get; set; }
        public string CerificateFileName { get; set; }
        public Dictionary<string, string> ReportCells { get; set; } = new Dictionary<string, string>();
        public List<string> Folders { get; set; } = new List<string>();
        public List<string> Procedures { get; set; } = new List<string>();

        public bool CreateConfig(string configPath)
        {
            try
            {
                //Generate file and write first lines
                string[] lines = { "<CalTools_Config Theme = \"Light\" Folders = \"PRODUCTION EQUIPMENT,ENGINEERING EQUIPMENT,QUALITY EQUIPMENT,Ref Only,Removed from Service\"" +
                        " Procedures = \"019-0065\">",
                        "\t<Database DbName = \"debug_Test Equipment Calibration List.db\"/>",
                        "\t<Directories ListDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\" " +
                        "TempFilesDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Template Files\" " +
                        "ItemScansDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Item Scans\"/>",
                        "\t<DueItems MarkDueDays = \"30\" DueInCalendar = \"60\"/>",
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
                Theme = config.LastChild.Attributes[0].Value;
                DbName = config.LastChild.ChildNodes[0].Attributes[0].Value;
                ListDir = config.LastChild.ChildNodes[1].Attributes[0].Value;
                TempFilesDir = config.LastChild.ChildNodes[1].Attributes[1].Value;
                ItemScansDir = config.LastChild.ChildNodes[1].Attributes[2].Value;
                MarkDueDays = int.Parse(config.LastChild.ChildNodes[2].Attributes[0].Value);
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

                DbPath = Path.Combine(ListDir, DbName);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading config file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
