using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

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
            certificate_number
        }
        public CalibrationItem(string sn)
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
