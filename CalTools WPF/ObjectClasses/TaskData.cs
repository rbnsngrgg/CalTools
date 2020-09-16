using System;

namespace CalTools_WPF.ObjectClasses
{
    public class TaskData
    {
#nullable enable
        public int? DataID { get; set; } = null;
        public int? TaskID { get; set; } = null;
        public string SerialNumber { get; set; } = "";
        public State? StateBefore { get; set; } = null;
        public State? StateAfter { get; set; } = null;
        public ActionTaken? ActionTaken { get; set; } = null;
        public DateTime? CompleteDate { get; set; } = null;
        public DateTime? DueDate { get; set; } = null;
        public string Procedure { get; set; } = "";
        //StandardEquipment should be JSON serialization of CalibrationItem class. Certificate number is required.
        public string StandardEquipment { get; set; } = "";
        public Findings? findings = new Findings();
        public string Remarks { get; set; } = "";
        public string Technician { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public enum DatabaseColumns
        {
            ColDataID,
            ColTaskID,
            ColSerialNumber,
            ColStateBeforeAction,
            ColStateAfterAction,
            ColActionTaken,
            ColCompleteDate,
            ColDueDate,
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
