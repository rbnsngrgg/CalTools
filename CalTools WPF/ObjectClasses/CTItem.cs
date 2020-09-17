using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace CalTools_WPF
{
    class CTItem
    {
        private string serialNumber = "";
        private string location = "";
        private string manufacturer = "";
        private string directory = "";
        private string description = "";
        private bool inService = true;
        private DateTime? inServiceDate = null;
        private bool taskDue = true;
        private string model = "";
        private string comment = "";
        private DateTime? timeStamp = null;
        private string itemGroup = "";
        private string certificateNumber = "";
        private bool standardEquipment = false;

        public string SerialNumber { get {return serialNumber; } set { serialNumber = value;ChangesMade = true; } }
        public string Location { get { return location; } set { location = value; ChangesMade = true; } }
        public string Manufacturer { get { return manufacturer; } set { manufacturer = value; ChangesMade = true; } }
        public string Directory { get { return directory; } set { directory = value; ChangesMade = true; } }
        public string Description { get { return description; } set { description = value; ChangesMade = true; } }
        public bool InService { get { return inService; } set { inService = value; ChangesMade = true; } }
        public DateTime? InServiceDate { get { return inServiceDate; } set { inServiceDate = value; ChangesMade = true; } }
        public bool TaskDue { get { return taskDue; } set { taskDue = value; ChangesMade = true; } }
        public string Model { get { return model; } set { model = value; ChangesMade = true; } }
        public string Comment { get { return comment; } set { comment = value; ChangesMade = true; } }
        public DateTime? TimeStamp { get { return timeStamp; } set { timeStamp = value; ChangesMade = true; } }
        public string ItemGroup { get { return itemGroup; } set { itemGroup = value; ChangesMade = true; } }
        public string CertificateNumber { get { return certificateNumber; } set { certificateNumber = value; ChangesMade = true; } }
        public bool StandardEquipment { get { return standardEquipment; } set { standardEquipment = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; } = false;
        public enum DatabaseColumns
        {
            SerialNumber = 0,
            Location,
            Manufacturer,
            Directory,
            Description,
            InService,
            InServiceDate,
            TaskDue,
            Model,
            Comments,
            Timestamp,
            ItemGroup,
            StandardEquipment,
            CertificateNumber
        }
        public CTItem(string sn)
        {
            this.SerialNumber = sn;
        }

        //Return a JSON string that represents this instance
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        //Display all properties in debug
        public void ShowDebug()
        {
            Debug.WriteLine("----------------------------------------------------------------------");
            Debug.WriteLine(SerialNumber);
            Debug.WriteLine(Location);
            Debug.WriteLine(Manufacturer);
            Debug.WriteLine(Directory);
            Debug.WriteLine(Description);
            Debug.WriteLine(InService);
            if (InServiceDate == null) { Debug.WriteLine("In Service Date Null"); } else { Debug.WriteLine(InServiceDate); }
            Debug.WriteLine(TaskDue);
            Debug.WriteLine(Model);
            Debug.WriteLine(Comment);
            if (TimeStamp == null) { Debug.WriteLine("Timestamp Null"); } else { Debug.WriteLine(TimeStamp); }
            Debug.WriteLine(ItemGroup);
            Debug.WriteLine(CertificateNumber);
            Debug.WriteLine(StandardEquipment);
            Debug.WriteLine("----------------------------------------------------------------------");
        }
    }
}
