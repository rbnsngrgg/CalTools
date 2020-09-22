using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;
using System.IO.Enumeration;
using CalTools_WPF.ObjectClasses;

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
        private string comment = "";

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

                if (value != null)
                { DueDate = value.Value.AddMonths(Interval); CompleteDateString = completeDate.Value.ToString("yyyy-MM-dd"); }
                else { DueDate = null;CompleteDateString = ""; }
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
                else { DueDateString = ""; }
                ChangesMade = true;
            }
        }
        public bool Due { get { return due; } set { if (due != value) { due = value; ChangesMade = true; } } }
        public string DueDateString { get; private set; }
        public string ActionType { get { return actionType; } set { actionType = value; ChangesMade = true; } }
        public string Comment { get { return comment; } set { comment = value; ChangesMade = true; } }
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

        //Methods for checking the completion dates of TaskData and task folders
        public void CheckDates(string taskFolder, List<TaskData> taskDataList)
        {
            DateTime latestFileDate = CheckFolder(taskFolder);
            DateTime latestDataDate = CheckTaskData(ref taskDataList);
            if (latestFileDate == latestDataDate & latestDataDate == new DateTime()) { CompleteDate = null; }
            else if(latestFileDate > latestDataDate) { CompleteDate = latestFileDate; }
            else if (latestDataDate > latestFileDate) { CompleteDate = latestDataDate; }

        }

        private DateTime CheckFolder(string taskFolder)
        {
            if (taskFolder.Split("_")[0] != TaskID.ToString()) { return new DateTime(); }
            DateTime latestFileDate = new DateTime();
            foreach(string file in Directory.GetFiles(taskFolder))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                DateTime fileDate = CheckFile(fileName);
                if(fileDate > latestFileDate) { latestFileDate = fileDate; }
            }
            return latestFileDate;
        }
        private DateTime CheckFile(string file)
        {
            DateTime fileDate = new DateTime();
            string[] fileSplit = file.Split("_");
            bool snMatch = false;

            foreach (string split in fileSplit)
            {
                DateTime.TryParseExact(split, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate);
                if(split == SerialNumber)
                { snMatch = true; }
            }
            if (snMatch) { return fileDate; }
            else { return new DateTime(); }
        }
        private DateTime CheckTaskData(ref List<TaskData> taskDataList)
        {
            DateTime latestData = new DateTime();
            foreach(TaskData data in taskDataList)
            {
                if (data.CompleteDate != null) { if (data.CompleteDate > latestData) { latestData = (DateTime)data.CompleteDate; } }
            }
            return latestData;
        }
    }
}
