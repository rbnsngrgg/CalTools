using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Security.Permissions;
using System.Text;

namespace CalTools_WPF.ObjectClasses
{
    public class Findings
    {
        public List<Param> parameters = new List<Param>();
        public bool DataFiles { get; set; } = false;
        public List<string> files = new List<string>();
    }

    public class Param
    {
        public string Name { get; set; }
        public float Tolerance { get; set; }
        public bool ToleranceIsPercent { get; set; }
        public string UnitOfMeasure { get; set; }
        public float MeasurementBefore { get; set; }
        public float MeasurementAfter { get; set; }
        public float Setting { get; set; }
        public Param() { }
        public Param(string name)
        {
            Name = name;
        }
        public Param(string name, float tolerance, bool isPercent, string uom, float measureBefore, float measureAfter)
        {
            Name = name;
            Tolerance = tolerance;
            ToleranceIsPercent = isPercent;
            UnitOfMeasure = uom;
            MeasurementBefore = measureBefore;
            MeasurementAfter = measureAfter;
        }
    }
}
