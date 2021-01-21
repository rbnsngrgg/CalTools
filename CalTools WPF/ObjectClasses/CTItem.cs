using Newtonsoft.Json;
using System;

namespace CalTools_WPF
{
    class CTItem
    {
        #region Private Fields
        private string serialNumber = "";
        private string location = "";
        private string manufacturer = "";
        private string directory = "";
        private string description = "";
        private bool inService = true;
        private DateTime? inServiceDate = null;
        private string model = "";
        private string comment = "";
        private DateTime? timeStamp = null;
        private string itemGroup = "";
        private string certificateNumber = "";
        private bool standardEquipment = false;
        #endregion

        #region Getters and Setters
        public string SerialNumber { get => serialNumber; set { serialNumber = value; ChangesMade = true; } }
        public string Location { get => location; set { location = value; ChangesMade = true; } }
        public string Manufacturer { get => manufacturer; set { manufacturer = value; ChangesMade = true; } }
        public string Directory { get => directory; set { directory = value; ChangesMade = true; } }
        public string Description { get => description; set { description = value; ChangesMade = true; } }
        public bool InService { get => inService; set { inService = value; ChangesMade = true; } }
        public DateTime? InServiceDate//{ get => inServiceDate; set { inServiceDate = value; ChangesMade = true; } }
        {
            get => inServiceDate;
            set
            {
                inServiceDate = value;

                if (value != null)
                { InServiceDateString = inServiceDate.Value.ToString("yyyy-MM-dd"); }
                else { InServiceDateString = ""; }
                ChangesMade = true;
            }
        }
        public string InServiceDateString { get; private set; } = "";
        public string Model { get => model; set { model = value; ChangesMade = true; } }
        public string Comment { get => comment; set { comment = value; ChangesMade = true; } }
        public DateTime? TimeStamp //{ get => timeStamp; set { timeStamp = value; ChangesMade = true; } }
        {
            get => timeStamp;
            set
            {
                timeStamp = value;

                if (value != null)
                { TimeStampString = timeStamp.Value.ToString("yyyy-MM-dd-HH-mm-ss-ffffff"); }
                else { TimeStampString = ""; }
                ChangesMade = true;
            }
        }
        public string TimeStampString { get; private set; } = "";
        public string ItemGroup { get => itemGroup; set { itemGroup = value; ChangesMade = true; } }
        public string CertificateNumber { get => certificateNumber; set { certificateNumber = value; ChangesMade = true; } }
        public bool StandardEquipment { get => standardEquipment; set { standardEquipment = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; } = false;
        #endregion

        public enum DatabaseColumns
        {
            SerialNumber = 0,
            Location,
            Manufacturer,
            Directory,
            Description,
            InService,
            InServiceDate,
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
    }
}
