using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalTools_WPF.ObjectClasses
{
    public class TaskData : ICTObject
    {
        #region Private Fields
#nullable enable
        private int dataId = -1;
        private int? taskId;
        private string serialNumber = "";
        private State? stateBefore;
        private State? stateAfter;
        private ActionTaken? actionTaken;
        private DateTime? completeDate;
        private string procedure = "";
        private List<Findings> findings = new();
        private List<CTStandardEquipment> standardEquipment = new();
        private string remarks = "";
        private string technician = "";
        private DateTime timestamp = DateTime.MinValue;
        private List<TaskDataFile> dataFiles = new();
        #endregion

        #region Getters and Setters
        public int DataId { get => dataId; set { dataId = value; ChangesMade = true; } }
        public int? TaskId { get => taskId; set { taskId = value; ChangesMade = true; } }
        public string SerialNumber { get => serialNumber; set { serialNumber = value; ChangesMade = true; } }
        public State? StateBefore { get => stateBefore; set { stateBefore = value; ChangesMade = true; } }
        public State? StateAfter { get => stateAfter; set { stateAfter = value; ChangesMade = true; } }
        public ActionTaken? Actions { get => actionTaken; set { actionTaken = value; ChangesMade = true; } }
        public DateTime? CompleteDate
        {
            get => completeDate;
            set
            {
                if (completeDate != value) { ChangesMade = true; }
                completeDate = value;
            }
        }
        public string CompleteDateString { get => CompleteDate.HasValue ? CompleteDate.Value.ToString("yyyy-MM-dd") : ""; }
        public string Procedure { get => procedure; set { procedure = value; ChangesMade = true; } }
        public List<Findings> Findings { get => findings; set { findings = value; ChangesMade = true; } }
        public List<CTStandardEquipment> StandardEquipment { get => standardEquipment; set { standardEquipment = value; ChangesMade = true; } }
        public string Remarks { get => remarks; set { remarks = value; ChangesMade = true; } }
        public string Technician { get => technician; set { technician = value; ChangesMade = true; } }
        public DateTime Timestamp { get => timestamp; set { timestamp = value; ChangesMade = true; } }
        public string TimestampString { get => timestamp.ToString("yyyy-MM-dd-HH-mm-ss-ffffff", CultureInfo.InvariantCulture); }
        public List<TaskDataFile> DataFiles { get => dataFiles; set { dataFiles = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; }
        #endregion

        public TaskData() { }

        public TaskData(Dictionary<string, string> parameters)
        {
            ParseParameters(parameters);
        }

        public void ParseParameters(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("id")) { DataId = int.Parse(parameters["id"]); }
            TaskId = int.Parse(parameters["task_id"]);
            SerialNumber = parameters["serial_number"];
            StateBefore = new()
            {
                InTolerance = parameters["in_tolerance_before"] == "1",
                Operational = parameters["operational_before"] == "1"
            };
            StateAfter = new()
            {
                InTolerance = parameters["in_tolerance_after"] == "1",
                Operational = parameters["operational_after"] == "1"
            };
            Actions = new()
            {
                Calibration = parameters["calibrated"] == "1",
                Verification = parameters["verified"] == "1",
                Adjusted = parameters["adjusted"] == "1",
                Repaired = parameters["repaired"] == "1",
                Maintenance = parameters["maintenance"] == "1"
            };
            CompleteDate = DateTime.ParseExact(parameters["complete_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Procedure = parameters["procedure"];
            Remarks = parameters["remarks"];
            Technician = parameters["technician"];
            if (parameters.ContainsKey("timestamp") && parameters["timestamp"] != "")
            { Timestamp = DateTime.ParseExact(parameters["timestamp"], "yyyy-MM-dd-HH-mm-ss-ffffff", CultureInfo.InvariantCulture); }
            ChangesMade = false;
        }
    }
#nullable disable
    public struct State
    {
        public bool InTolerance;
        public bool Operational;
    }
    public struct ActionTaken
    {
        public bool Calibration;
        public bool Verification;
        public bool Adjusted;
        public bool Repaired;
        public bool Maintenance;
    }

    public class TaskDataFile : ICTObject
    {
        public string Description { get; set; } = "";
        public string Location { get; set; } = "";

        public TaskDataFile() { }
        public void ParseParameters(Dictionary<string, string> parameters)
        {
            Description = parameters["description"];
            Location = parameters["location"];
        }
    }
}
