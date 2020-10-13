using Newtonsoft.Json;
using System;

namespace CalTools_WPF
{
    class CalibrationItemV4
    {
        public string SerialNumber { get; set; }
        public string Location { get; set; }
        public int Interval { get; set; } = 12;
        public string CalVendor { get; set; }
        public string Manufacturer { get; set; }
        public DateTime? lastCal = null;
        public DateTime? LastCal { get { return lastCal; } set { lastCal = value; if (value != null) { CalDateFormat = lastCal.Value.ToString("yyyy-MM-dd"); } } }
        public DateTime? nextCal = null;
        public DateTime? NextCal { get { return nextCal; } set { nextCal = value; if (value != null) { DueDateFormat = nextCal.Value.ToString("yyyy-MM-dd"); } } }
        public bool Mandatory { get; set; } = true;
        public string Directory { get; set; }
        public string Description { get; set; }
        public bool InService { get; set; } = true;
        public DateTime? InServiceDate { get; set; } = null;
        public DateTime? OutOfServiceDate { get; set; } = null;
        public bool CalDue { get; set; } = true;
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
            serial_number = 0,
            location,
            interval,
            cal_vendor,
            manufacturer,
            lastcal,
            nextcal,
            mandatory,
            directory,
            description,
            inservice,
            inservicedate,
            outofservicedate,
            caldue,
            model,
            comments,
            timestamp,
            item_group,
            verify_or_calibrate,
            standard_equipment,
            certificate_number
        }
        public CalibrationItemV4(string sn)
        {
            this.SerialNumber = sn;
        }

        //Return a JSON string that represents this instance
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
