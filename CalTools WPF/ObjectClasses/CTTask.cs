using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;

namespace CalTools_WPF
{
    class CTTask
    {
        private int taskID = -1;
        private string serialNumber = "";
        private string taskTitle = "";
        private string serviceVendor = "";
        private bool mandatory = true;
        private int interval = 12;
        private DateTime? completeDate = null;
        private DateTime? dueDate = null;
        private bool due = false;
        private string actionType = "";
        private string comments = "";


        public int TaskID { get { return taskID; } set { taskID = value; ChangesMade = true; } }
        public string SerialNumber { get { return serialNumber; } set { serialNumber = value; ChangesMade = true; } }
        public string TaskTitle { get { return taskTitle; } set { taskTitle = value; ChangesMade = true; } }
        public string ServiceVendor { get { return serviceVendor; } set { serviceVendor = value; ChangesMade = true; } }
        public bool Mandatory { get { return mandatory; } set { mandatory = value; ChangesMade = true; } }
        public int Interval { get { return interval; } set { interval = value; DueDate = completeDate.Value.AddMonths(Interval); ChangesMade = true; } }
        public DateTime? CompleteDate { get { return completeDate; }
            set 
            { 
                completeDate = value;
                DueDate = value.Value.AddMonths(Interval);
                if (value != null) 
                { CompleteDateString = completeDate.Value.ToString("yyyy-MM-dd"); }
                ChangesMade = true;
            } 
        }
        public string CompleteDateString { get; private set; }
        public DateTime? DueDate { get { return dueDate; } 
            set 
            { 
                dueDate = value;
                if (value != null)
                { 
                    DueDateString = dueDate.Value.ToString("yyyy-MM-dd");
                } 
                ChangesMade = true;
            }
        }
        public bool Due { get { return due; } set { due = value; ChangesMade = true; } }
        public string DueDateString { get; private set; }
        public string ActionType { get { return actionType; } set { actionType = value; ChangesMade = true; } }
        public string Comments { get { return comments; } set { comments = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; } = false;
        public enum DatabaseColumns
        {
            TaskID,
            SerialNumber,
            TaskTitle,
            ServiceVendor,
            Mandatory,
            Interval,
            CompleteDate,
            DueDate,
            Due,
            ActionType,
            Comments
        }

        public bool CheckDue(int days)
        {
            if ((dueDate - DateTime.UtcNow).Value.Days < days) { Due = true; }
            else { Due = false; }
            return Due;
        }

        public void CheckFolder(string taskFolder)
        {
            if (taskFolder.Split("_")[0] != TaskID.ToString()) { return; }
            DateTime latestFileDate = new DateTime();
            foreach(string file in Directory.GetFiles(taskFolder))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string[] fileSplit = fileName.Split("_");
                bool snMatch = false;

                foreach (string split in fileSplit)
                {
                    DateTime.TryParseExact(split, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out currentFileDate);
                }
            }
        }
    }
}
