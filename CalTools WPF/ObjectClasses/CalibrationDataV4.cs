using System;

namespace CalTools_WPF.ObjectClasses
{
    public class CalibrationDataV4
    {
#nullable enable
        public int? ID { get; set; } = null;
        public string SerialNumber { get; set; } = "";
        public State? StateBefore { get; set; } = null;
        public State? StateAfter { get; set; } = null;
        public ActionTakenV5? ActionTaken { get; set; } = null;
        public DateTime? CalibrationDate { get; set; } = null;
        public DateTime? DueDate { get; set; } = null;
        public string Procedure { get; set; } = "";
        //StandardEquipment should be JSON serialization of CalibrationItem class. Certificate number is required.
        public string StandardEquipment { get; set; } = "";
        public FindingsV5? findings = new();
        public string Remarks { get; set; } = "";
        public string Technician { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public enum DatabaseColumns
        {
            ColID,
            ColSerialNumber,
            ColStateBeforeAction,
            ColStateAfterAction,
            ColActionTaken,
            ColCalibrationDate,
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
}