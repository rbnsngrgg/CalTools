using System;
using System.Collections.Generic;

namespace CalTools_WPF.ObjectClasses
{
    public class TaskData
    {
        #region Private Fields
#nullable enable
        private int dataID = -1;
        private int? taskID = null;
        private string serialNumber = "";
        private State? stateBefore = null;
        private State? stateAfter = null;
        private ActionTaken? actionTaken = null;
        private DateTime? completeDate = null;
        private string procedure = "";
        private List<Parameter> findings = new();
        private List<CTStandardEquipment> standardEquipment = new();
        private string remarks = "";
        private string technician = "";
        public string timestamp = "";
        private List<Tuple<string, string>> dataFiles = new();
        #endregion

        #region Getters and Setters
        public int DataID { get => dataID; set { dataID = value; ChangesMade = true; } }
        public int? TaskID { get => taskID; set { taskID = value; ChangesMade = true; } }
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
#pragma warning disable CS8629 // Nullable value type may be null.
                if (value != null)
                { CompleteDateString = completeDate.Value.ToString("yyyy-MM-dd"); }
#pragma warning restore CS8629 // Nullable value type may be null.
                else { CompleteDateString = ""; }
            }
        }
        public string CompleteDateString { get; private set; } = "";
        public string Procedure { get => procedure; set { procedure = value; ChangesMade = true; } }
        public List<Parameter> Findings { get => findings; set { findings = value; ChangesMade = true; } }
        public List<CTStandardEquipment> StandardEquipment { get => standardEquipment; set { standardEquipment = value; ChangesMade = true; } }
        public string Remarks { get => remarks; set { remarks = value; ChangesMade = true; } }
        public string Technician { get => technician; set { technician = value; ChangesMade = true; } }
        public string Timestamp { get => timestamp; set { timestamp = value; ChangesMade = true; } }
        public List<Tuple<string, string>> DataFiles { get => dataFiles; set { dataFiles = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; }
        #endregion

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
}
