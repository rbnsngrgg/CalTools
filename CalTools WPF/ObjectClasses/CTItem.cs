using Newtonsoft.Json;
using System;

namespace CalTools_WPF
{
    public class CTItem
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
        public string Remarks { get => comment; set { comment = value; ChangesMade = true; } }
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
        public bool ReplacementAvailable { get; set; } = false;
        public bool StandardEquipment { get => standardEquipment; set { standardEquipment = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; } = false;
        #endregion

        public CTItem(string sn)
        {
            this.SerialNumber = sn;
            this.ChangesMade = false;
        }

        //Return a JSON string that represents this instance
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
        public CTStandardEquipment ToStandardEquipment(DateTime dueDate)
        {
            return new CTStandardEquipment(SerialNumber)
            {
                Manufacturer = Manufacturer,
                Description = Description,
                Model = Model,
                Remarks = Remarks,
                ActionDueDate = dueDate,
                ItemGroup = ItemGroup,
                CertificateNumber = CertificateNumber
            };
        }
    }
    public class CTStandardEquipment
    {
        #region Private Fields
        private int id = -1;
        private string serialNumber = "";
        private string manufacturer = "";
        private string description = "";
        private string model = "";
        private string remarks = "";
        private DateTime? timestamp = null;
        private DateTime actionDueDate = new();
        private string itemGroup = "";
        private string certificateNumber = "";
        #endregion

        #region Getters and Setters
        public int Id { get => id; set { id = value; ChangesMade = true; } }
        public string SerialNumber { get => serialNumber; set { serialNumber = value; ChangesMade = true; } }
        public string Manufacturer { get => manufacturer; set { manufacturer = value; ChangesMade = true; } }
        public string Description { get => description; set { description = value; ChangesMade = true; } }
        public string Model { get => model; set { model = value; ChangesMade = true; } }
        public string Remarks { get => remarks; set { remarks = value; ChangesMade = true; } }
        public DateTime? TimeStamp //{ get => timeStamp; set { timeStamp = value; ChangesMade = true; } }
        {
            get => timestamp;
            set
            {
                timestamp = value;
                ChangesMade = true;
            }
        }
        public DateTime ActionDueDate { get => actionDueDate; set { actionDueDate = value; ChangesMade = true; } }
        public string ItemGroup { get => itemGroup; set { itemGroup = value; ChangesMade = true; } }
        public string CertificateNumber { get => certificateNumber; set { certificateNumber = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; } = false;
        #endregion

        public CTStandardEquipment(string sn, int id = -1)
        {
            SerialNumber = sn;
            this.id = id;
            ChangesMade = false;
        }
    }
}
