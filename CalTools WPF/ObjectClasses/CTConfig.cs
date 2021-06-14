using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;

namespace CalTools_WPF
{
    class CTConfig
    {
        private readonly string configName = "CTConfig.xml";
        //Values to be loaded from config
        public string DbName { get; set; }
        public string DbPath { get; set; }
        public string ListDir { get; set; }
        public string ItemScansDir { get; set; }
        public int MarkDueDays { get; set; }
        public string CerificateFileName { get; set; }
        public List<string> Folders { get; set; } = new List<string>();
        public List<string> Procedures { get; set; } = new List<string>();

        public static bool CreateConfig(string configPath)
        {
            try
            {
                //Generate file and write first lines
                string[] lines = { "<CalTools_Config Folders = \"PRODUCTION EQUIPMENT,ENGINEERING EQUIPMENT,QUALITY EQUIPMENT,Ref Only,Removed from Service\"" +
                        " Procedures = \"019-0065\">",
                        "\t<Database DbName = \"Test Equipment Calibration List.db\"/>",
                        "\t<Directories ListDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\" " +
                        "ItemScansDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Item Scans\"/>",
                        "\t<DueItems MarkDueDays = \"30\"/>",
                    "</CalTools_Config>" };
#if DEBUG
                lines = new string[]{ "<CalTools_Config Folders = \"PRODUCTION EQUIPMENT,ENGINEERING EQUIPMENT,QUALITY EQUIPMENT,Ref Only,Removed from Service,Debug Items\"" +
                        " Procedures = \"019-0065\">",
                        "\t<Database DbName = \"debug_Test Equipment Calibration List.db\"/>",
                        "\t<Directories ListDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\" " +
                        "ItemScansDir = \"\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Item Scans\"/>",
                        "\t<DueItems MarkDueDays = \"30\"/>",
                    "</CalTools_Config>" };
#endif
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
                XmlDocument config = new();
                config.Load(configPath);
                if(config.LastChild.Attributes[0].Name == "Theme") { throw new System.Exception("Old config version"); }
                Folders.AddRange(config.LastChild.Attributes[0].Value.Split(","));
                Procedures.AddRange(config.LastChild.Attributes[1].Value.Split(","));
                DbName = config.LastChild.ChildNodes[0].Attributes[0].Value;
                ListDir = config.LastChild.ChildNodes[1].Attributes[0].Value;
                ItemScansDir = config.LastChild.ChildNodes[1].Attributes[1].Value;
                MarkDueDays = int.Parse(config.LastChild.ChildNodes[2].Attributes[0].Value);

                DbPath = Path.Combine(ListDir, DbName);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading config file: {ex.Message}. " +
                    $"The config may be incompatible with this version of CalTools. " +
                    $"Rename or remove the config, then restart CalTools.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
