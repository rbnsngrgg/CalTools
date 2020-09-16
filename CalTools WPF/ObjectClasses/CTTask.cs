using System;
using System.Collections.Generic;
using System.Text;

namespace CalTools_WPF
{
    class CTTask
    {
        public int TaskID { get; set; }
        public string SerialNumber { get; set; }
        public string TaskTitle { get; set; }
        public string ServiceVendor { get; set; }
        public bool Mandatory { get; set; } = true;
        public int Interval { get; set; }
        private DateTime? completeDate = null;
        public DateTime? CompleteDate { get { return completeDate; } set { completeDate = value; if (value != null) { CompleteDateString = completeDate.Value.ToString("yyyy-MM-dd"); } } }
        public string CompleteDateString { get; set; }
        private DateTime? dueDate = null;
        public DateTime? DueDate { get { return dueDate; } set { dueDate = value; if (value != null) { DueDateString = dueDate.Value.ToString("yyyy-MM-dd"); } } }
        public bool Due { get; set; } = false;
        public string DueDateString { get; set; }
        public string ActionType { get; set; }
        public string Comments { get; set; }
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
    }
}
