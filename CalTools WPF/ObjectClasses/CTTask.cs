using CalTools_WPF.ObjectClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CalTools_WPF
{
    public class CTTask : ICTObject
    {
        #region Private Fields
        private int taskId = -1;
        private string serialNumber = "";
        private string taskTitle = "CALIBRATION";
        private string serviceVendor = "";
        private bool mandatory = true;
        private int interval = 12;
        private DateTime? completeDate;
        private DateTime? dueDate;
        private bool isDue = true;
        private string actionType = "CALIBRATION";
        private string taskDirectory = "";
        private string remarks = "";
        private DateTime? dateOverride;
        private IDirectoryWrapper directoryWrapper = new DirectoryWrapper();
        #endregion

        #region Getters and Setters
        public int TaskId { get => taskId; set { taskId = value; ChangesMade = true; } }
        public string SerialNumber { get => serialNumber; set { serialNumber = value; ChangesMade = true; } }
        public string TaskTitle { get => taskTitle; set { taskTitle = value; ChangesMade = true; } }
        public string ServiceVendor { get => serviceVendor; set { serviceVendor = value; ChangesMade = true; } }
        public bool IsMandatory { get => mandatory; set { mandatory = value; ChangesMade = true; } }
        public int Interval { get => interval; set { interval = value; if (CompleteDate != null) { DueDate = completeDate.Value.AddMonths(Interval); } ChangesMade = true; } }
        public DateTime? CompleteDate
        {
            get => completeDate;
            set
            {
                if (completeDate != value & completeDate != null)
                { CompleteDateChanged = true; }
                completeDate = value;

                if (value != null)
                { DueDate = value.Value.AddMonths(Interval); }
                else { DueDate = null; IsDue = true; }
                ChangesMade = true;
            }
        }
        public string CompleteDateString { get => CompleteDate.HasValue ? CompleteDate.Value.ToString("yyyy-MM-dd") : ""; }
        public DateTime? DueDate
        {
            get => dueDate; set
            {
                dueDate = value;
                ChangesMade = true;
            }
        }
        public string DueDateString { get => DueDate.HasValue ? DueDate.Value.ToString("yyyy-MM-dd") : ""; }
        public bool IsDue { get => isDue; set { if (isDue != value) { isDue = value; ChangesMade = true; } } }
        public string ActionType { get => actionType; set { actionType = value; ChangesMade = true; } }
        public string TaskDirectory { get => taskDirectory; set { if (taskDirectory != value) { ChangesMade = true; } taskDirectory = value; } }
        public string Remarks { get => remarks; set { remarks = value; ChangesMade = true; } }
        public DateTime? DateOverride
        {
            get => dateOverride;
            set
            {
                dateOverride = value;
                ChangesMade = true;
            }
        }
        public string DateOverrideString { get => DateOverride.HasValue ? DateOverride.Value.ToString("yyyy-MM-dd") : ""; }
        public bool ChangesMade { get; set; }
        public bool CompleteDateChanged { get; set; }
        public string[] ActionTypes { get; private set; } = new string[] { "CALIBRATION", "MAINTENANCE", "VERIFICATION" };
        #endregion

        //For use with the DetailsTasksTable. Used to populate the datagrid combobox with vendors. Transient
        public List<string> ServiceVendorList { get; set; } = new();
        //---------------------------------------------------------------------------------------------------------------------------------

        public CTTask()
        {
            directoryWrapper = new DirectoryWrapper();
        }

        public CTTask(IDirectoryWrapper wrapper = null)
        {
            if (wrapper != null)
            {
                directoryWrapper = wrapper;
            }
        }

        public CTTask(Dictionary<string,string> parameters, IDirectoryWrapper wrapper = null)
        {
            if (wrapper != null)
            {
                directoryWrapper = wrapper;
            }
            ParseParameters(parameters);
        }

        public void ParseParameters(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("id")) { TaskId = int.Parse(parameters["id"]); }
            SerialNumber = parameters["serial_number"];
            TaskTitle = parameters["task_title"];
            ServiceVendor = parameters["service_vendor"];
            IsMandatory = parameters["is_mandatory"] == "1";
            Interval = int.Parse(parameters["interval"]);
            CompleteDate = parameters["complete_date"] != "" ?
                DateTime.ParseExact(parameters["complete_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture) :
                null;
            IsDue = parameters["is_due"] == "1";
            ActionType = parameters["action_type"];
            TaskDirectory = parameters["directory"];
            Remarks = parameters["remarks"];
            DateOverride = parameters["date_override"] != "" ?
                DateTime.ParseExact(parameters["date_override"], "yyyy-MM-dd", CultureInfo.InvariantCulture) :
                null;
            ChangesMade = false;
        }


        //Methods for checking the completion dates of TaskData and task folders
        private bool TaskFolderStartsWithId(string folder)
        {
            //Input full path of folder. Return true if folder name is properly formatted with TaskID, false otherwise.
            return Path.GetFileName(folder).Split("_")[0] == TaskId.ToString();
        }
        private DateTime GetFileDateIfExists(string file)
        {
            DateTime fileDate = default;
            string[] fileSplit = file.Split("_");
            bool snMatch = false;

            foreach (string split in fileSplit)
            {
                if (fileDate == default) { DateTime.TryParseExact(split, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate); }
                if (split == SerialNumber)
                { snMatch = true; }
            }
            return snMatch ? fileDate : new DateTime();
        }
        private DateTime LatestDateFromFolder(string taskFolder)
        {
            DateTime latestFileDate = new();
            if (TaskFolderStartsWithId(taskFolder))
            {
                List<string> filesAndFolders = new();
                filesAndFolders.AddRange(directoryWrapper.GetFiles(taskFolder));
                filesAndFolders.AddRange(directoryWrapper.GetDirectories(taskFolder));
                foreach (string file in filesAndFolders)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    DateTime fileDate = GetFileDateIfExists(fileName);
                    if (fileDate > latestFileDate) { latestFileDate = fileDate; }
                }
            }
            return latestFileDate;
        }
        private DateTime LatestDateFromTaskData(ref List<TaskData> taskDataList)
        {
            DateTime latestData = default;
            foreach (TaskData data in taskDataList)
            {
                if (data.CompleteDate != null) { if (data.CompleteDate > latestData) { latestData = (DateTime)data.CompleteDate; } }
            }
            return latestData;
        }
        public bool IsTaskDueWithinDays(int days, DateTime checkDate) //Check whether task is due within (days) days of checkDate
        {
            if (dueDate == null || (dueDate - checkDate).Value.Days < days)
            {
                return true;
            }
            return false;
        }
        public bool SetDueWithinDays(int days, DateTime checkDate)
        {
            IsDue = IsTaskDueWithinDays(days, checkDate);
            return IsDue;
        }
        public void SetCompleteDateFromData(string taskFolder, List<TaskData> taskDataList)
        {
            DateTime latestFileDate = LatestDateFromFolder(taskFolder);
            DateTime latestDataDate = LatestDateFromTaskData(ref taskDataList);
            if (latestFileDate == latestDataDate && latestDataDate == new DateTime()) { CompleteDate = null; }
            else if (latestFileDate > latestDataDate) { CompleteDate = latestFileDate; }
            else if (latestDataDate > latestFileDate) { CompleteDate = latestDataDate; }
        }
        public string GetTaskFolderIfExists()
        {
            if (Directory.Exists(TaskDirectory)) { return TaskDirectory; }
            else
            {
                bool folderFound = false;
                string itemFolder = directoryWrapper.GetParent(TaskDirectory).FullName;
                foreach (string folder in directoryWrapper.GetDirectories(itemFolder))
                {
                    if (TaskFolderStartsWithId(folder)) { TaskDirectory = folder; folderFound = true; break; }
                }
                if (!folderFound) { TaskDirectory = ""; }
                return TaskDirectory;
            }
        }//Check if task folder is valid. Try to find the task folder if not. Return valid directory or empty string
    }
}
