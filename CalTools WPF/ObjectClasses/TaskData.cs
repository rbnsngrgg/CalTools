using System;

namespace CalTools_WPF.ObjectClasses
{
    public class TaskData
    {
        #region Private Fields
#nullable enable
        private int? dataID = null;
        private int? taskID = null;
        private string serialNumber = "";
        private State? stateBefore = null;
        private State? stateAfter = null;
        private ActionTaken? actionTaken = null;
        private DateTime? completeDate = null;
        private string procedure = "";
        private string standardEquipment = "";
        private Findings? findings = new Findings();
        private string remarks = "";
        private string technician = "";
        public string timestamp = "";
        #endregion

        #region Getters and Setters
        public int? DataID { get { return dataID; } set { dataID = value; ChangesMade = true; } }
        public int? TaskID { get { return taskID; } set { taskID = value; ChangesMade = true; } }
        public string SerialNumber { get { return serialNumber; } set { serialNumber = value; ChangesMade = true; } }
        public State? StateBefore { get { return stateBefore; } set { stateBefore = value; ChangesMade = true; } }
        public State? StateAfter { get { return stateAfter; } set { stateAfter = value; ChangesMade = true; } }
        public ActionTaken? ActionTaken { get { return actionTaken; } set { actionTaken = value; ChangesMade = true; } }
        public DateTime? CompleteDate
        {
            get { return completeDate; }
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
        public string Procedure { get { return procedure; } set { procedure = value; ChangesMade = true; } }
        //StandardEquipment should be JSON serialization of CalibrationItem class. Certificate number is required.
        public string StandardEquipment { get { return standardEquipment; } set { standardEquipment = value; ChangesMade = true; } }
        public Findings? Findings { get { return findings; } set { findings = value; ChangesMade = true; } }
        public string Remarks { get { return remarks; } set { remarks = value; ChangesMade = true; } }
        public string Technician { get { return technician; } set { technician = value; ChangesMade = true; } }
        public string Timestamp { get { return timestamp; } set { timestamp = value; ChangesMade = true; } }
        public bool ChangesMade { get; set; } = false;
        #endregion

        public enum DatabaseColumns
        {
            ColDataID,
            ColTaskID,
            ColSerialNumber,
            ColStateBeforeAction,
            ColStateAfterAction,
            ColActionTaken,
            ColCompleteDate,
            ColProcedure,
            ColStandardEquipment,
            ColFindings,
            ColRemarks,
            ColTechnician,
            ColEntryTimestamp
        }

    }
#nullable disable
    public struct State
    {
        public bool InTolerance;
        public bool OutOfTolerance;
        public bool Malfunctioning;
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
