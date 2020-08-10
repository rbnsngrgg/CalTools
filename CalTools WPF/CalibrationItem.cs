using System;
using System.Collections.Generic;
using System.Text;

namespace CalTools_WPF
{
    class CalibrationItem
    {
        public string SerialNumber { get; set; }
        public string Location { get; set; }
        public int Interval { get; set; } = 12;
        public string CalVendor { get; set; }
        public string Manufacturer { get; set; }
        public DateTime? LastCal { get; set; } = null;
        public DateTime? NextCal { get; set; } = null;
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

        public CalibrationItem(string sn)
        {
            this.SerialNumber = sn;
        }
    }
}
