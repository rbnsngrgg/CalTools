using System;
using System.Collections.Generic;
using System.Text;

namespace CalTools_WPF.ObjectClasses
{
    class CalibrationData
    {
        public string SerialNumber { get; set; }
        public State StateBefore { get; set; }
        public State StateAfter { get; set; }
        public ActionTaken ActionTaken { get; set; }
        public DateTime? CalibrationDate { get; set; } = null;
        public DateTime? DueDate { get; set; } = null;
        public string Procedure { get; set; }
        //StandardEquipment should be JSON serialization of CalibrationItem class. Certificate number is required.
        public string StandardEquipment { get; set; }
        public Findings findings = new Findings();
    }

    struct State
    {
        public bool InTolerance;
        public bool Malfunctioning;
        public bool Operational;
    }
    struct ActionTaken
    {
        public bool Calibration;
        public bool Verification;
        public bool Adjusted;
        public bool Repaired;
    }
}
