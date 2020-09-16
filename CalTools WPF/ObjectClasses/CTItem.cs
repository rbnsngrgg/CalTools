using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace CalTools_WPF
{
    class CTItem
    {
        public string SerialNumber { get; set; }
        public string Location { get; set; }
        public int Interval { get; set; } = 12;
        public string CalVendor { get; set; }
        public string Manufacturer { get; set; }
        private DateTime? lastCal { get; set; } = null;
        public DateTime? LastCal { get { return lastCal; } set { lastCal = value; if (value != null) { CalDateFormat = lastCal.Value.ToString("yyyy-MM-dd"); } } }
        private DateTime? nextCal = null;
        public DateTime? NextCal { get { return nextCal; } set { nextCal = value; if (value != null) { DueDateFormat = nextCal.Value.ToString("yyyy-MM-dd"); } } }
        public bool Mandatory { get; set; } = true;
        public string Directory { get; set; }
        public string Description { get; set; }
        public bool InService { get; set; } = true;
        public DateTime? InServiceDate { get; set; } = null;
        public DateTime? OutOfServiceDate { get; set; } = null;
        public bool TaskDue { get; set; } = true;
        public string Model { get; set; }
        public string Comment { get; set; }
        public DateTime? TimeStamp { get; set; } = null;
        public string ItemGroup { get; set; }
        public string VerifyOrCalibrate { get; set; } = "CALIBRATION";
        public string CertificateNumber { get; set; }
        public bool StandardEquipment { get; set; } = false;
        public string CalDateFormat { get; set; }
        public string DueDateFormat { get; set; }
        public enum DatabaseColumns
        {
            SerialNumber = 0,
            Location,
            CalVendor,
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
            Debug.WriteLine(Interval);
            Debug.WriteLine(CalVendor);
            Debug.WriteLine(Manufacturer);
            if (LastCal == null) { Debug.WriteLine("LastCal Null"); } else { Debug.WriteLine(LastCal); }
            if (NextCal == null) { Debug.WriteLine("NextCal Null"); } else { Debug.WriteLine(NextCal); }
            Debug.WriteLine(Mandatory);
            Debug.WriteLine(Directory);
            Debug.WriteLine(Description);
            Debug.WriteLine(InService);
            if (InServiceDate == null) { Debug.WriteLine("In Service Date Null"); } else { Debug.WriteLine(InServiceDate); }
            if (OutOfServiceDate == null) { Debug.WriteLine("Out of Service Date Null"); } else { Debug.WriteLine(OutOfServiceDate); }
            Debug.WriteLine(TaskDue);
            Debug.WriteLine(Model);
            Debug.WriteLine(Comment);
            if (TimeStamp == null) { Debug.WriteLine("Timestamp Null"); } else { Debug.WriteLine(TimeStamp); }
            Debug.WriteLine(ItemGroup);
            Debug.WriteLine(VerifyOrCalibrate);
            Debug.WriteLine(CertificateNumber);
            Debug.WriteLine(StandardEquipment);
            Debug.WriteLine("----------------------------------------------------------------------");
        }
    }
}
