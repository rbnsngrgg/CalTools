using CalTools_WPF.ObjectClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CalTools_WPF
{
    public class CTTask
    {
        public List<string> ActionTypes { get; private set; } = new List<string> { "CALIBRATION", "MAINTENANCE", "VERIFICATION" };
        #region Private Fields
        private int taskID = -1;
        private string serialNumber = "";
        private string taskTitle = "CALIBRATION";
        private string serviceVendor = "";
        private bool mandatory = true;
        private int interval = 12;
        private DateTime? completeDate = null;
        private DateTime? dueDate = null;
        private bool due = true;
        private string actionType = "CALIBRATION";
        private string taskDirectory = "";
        private string comment = "";
        private DateTime? manualFlag = null;
        #endregion

        #region Getters and Setters
        public int TaskID { get => taskID; set { taskID = value; ChangesMade = true; } }
        public string SerialNumber { get => serialNumber; set { serialNumber = value; ChangesMade = true; } }
        public string TaskTitle { get => taskTitle; set { taskTitle = value; ChangesMade = true; } }
        public string ServiceVendor { get => serviceVendor; set { serviceVendor = value; ChangesMade = true; } }
        public bool Mandatory { get => mandatory; set { mandatory = value; ChangesMade = true; } }
        public int Interval { get => interval; set { interval = value; if (CompleteDate != null) { DueDate = completeDate.Value.AddMonths(Interval); } ChangesMade = true; } }
        public DateTime? CompleteDate
        {
            get => completeDate;
            set
            {
                if(completeDate != value & completeDate != null)
                { CompleteDateChanged = true; }
                completeDate = value;

                if (value != null)
                { DueDate = value.Value.AddMonths(Interval); CompleteDateString = completeDate.Value.ToString("yyyy-MM-dd"); }
                else { DueDate = null; Due = true; CompleteDateString = ""; }
                ChangesMade = true;
            }
        }
        public string CompleteDateString { get; private set; } = "";
        public DateTime? DueDate
        {
            get => dueDate;
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
        public string DueDateString { get; private set; } = "";
        public bool Due { get => due; set { if (due != value) { due = value; ChangesMade = true; } } }
        public string ActionType { get => actionType; set { actionType = value; ChangesMade = true; } }
        public string TaskDirectory { get => taskDirectory; set { if (taskDirectory != value) { ChangesMade = true; } taskDirectory = value; } }
        public string Comment { get => comment; set { comment = value; ChangesMade = true; } }
        public DateTime? DateOverride
        { 
            get => manualFlag;
            set
            {
                manualFlag = value;
                if (value != null) 
                {
                    DateOverrideString = manualFlag.Value.ToString("yyyy-MM-dd");
                }
                else 
                { 
                    DateOverrideString = "";
                    DueDate = completeDate.Value.AddMonths(Interval);
                }
                ChangesMade = true; 
            } 
        }
        public string DateOverrideString { get; private set; } = "";
        public bool ChangesMade { get; set; } = false;
        public bool CompleteDateChanged { get; set; } = false;
        #endregion

        //For use with the DetailsTasksTable. Used to populate the datagrid combobox with vendors. Transient
        public List<string> ServiceVendorList { get; set; }
        //---------------------------------------------------------------------------------------------------------------------------------

        public bool IsTaskDue(int days, DateTime checkDate) //Check whether task is due within (days) days of checkDate
        {
            CheckManualFlag();
            if (dueDate == null) { Due = true; return Due; }
            if ((dueDate - checkDate).Value.Days < days) { Due = true; }
            else { Due = false; }
            return Due;
        }
        //Methods for checking the completion dates of TaskData and task folders
        public void CheckDates(string taskFolder, List<TaskData> taskDataList)
        {
            DateTime latestFileDate = CheckFolder(taskFolder);
            DateTime latestDataDate = CheckTaskData(ref taskDataList);
            if (latestFileDate == latestDataDate & latestDataDate == new DateTime()) { if (CompleteDate != null) { CompleteDate = null; }; }
            else if (latestFileDate > latestDataDate) { if (CompleteDate != latestFileDate) { CompleteDate = latestFileDate; } }
            else if (latestDataDate > latestFileDate) { if (CompleteDate != latestDataDate) { CompleteDate = latestDataDate; } }
            CheckManualFlag();
        }
        private DateTime CheckFolder(string taskFolder)
        {
            if (!FolderIsValid(taskFolder)) { return new DateTime(); }
            DateTime latestFileDate = new();
            List<string> filesAndFolders = new();
            filesAndFolders.AddRange(Directory.GetFiles(taskFolder));
            filesAndFolders.AddRange(Directory.GetDirectories(taskFolder));
            foreach (string file in filesAndFolders)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                DateTime fileDate = CheckFile(fileName);
                if (fileDate > latestFileDate) { latestFileDate = fileDate; }
            }
            return latestFileDate;
        }
        private DateTime CheckFile(string file)
        {
            DateTime fileDate = new();
            string[] fileSplit = file.Split("_");
            bool snMatch = false;

            foreach (string split in fileSplit)
            {
                if (fileDate.Year == 1) { DateTime.TryParseExact(split, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate); }
                if (split == SerialNumber)
                { snMatch = true; }
            }
            if (snMatch) { return fileDate; }
            else { return new DateTime(); }
        }
        private void CheckManualFlag()
        {
            if (DateOverride == null) {CompleteDate = CompleteDate; return; }
            if(DateOverride <= CompleteDate) { DateOverride = null; CompleteDate = CompleteDate; }
            else if (DateOverride < DueDate) { DueDate = DateOverride; }
        }
        private DateTime CheckTaskData(ref List<TaskData> taskDataList)
        {
            DateTime latestData = new();
            foreach (TaskData data in taskDataList)
            {
                if (data.CompleteDate != null) { if (data.CompleteDate > latestData) { latestData = (DateTime)data.CompleteDate; } }
            }
            return latestData;
        }
        private bool FolderIsValid(string folder)
        {
            //Input full path of folder. Return true if folder name is properly formatted with TaskID, false otherwise.
            if (Path.GetFileName(folder).Split("_")[0] != TaskID.ToString()) { return false; }
            else { return true; }
        }
        public string GetTaskFolder()
        {
            if (Directory.Exists(TaskDirectory)) { return TaskDirectory; }
            else
            {
                bool folderFound = false;
                string itemFolder = Directory.GetParent(TaskDirectory).FullName;
                foreach (string folder in Directory.GetDirectories(itemFolder))
                {
                    if (FolderIsValid(folder)) { TaskDirectory = folder; folderFound = true; break; }
                }
                if (!folderFound) { TaskDirectory = ""; }
                return TaskDirectory;
            }
        }//Check if task folder is valid. Try to find the task folder if not. Return valid directory or empty string
    }
}
