using System.Collections.Generic;

namespace CalTools_WPF.ObjectClasses
{
    public class Findings
    {
        public List<Parameter> parameters = new();
        public bool DataFiles { get; set; } = false;
        public List<string> files = new();
    }

    public class Parameter //To be embedded within Findings object
    {
        public int Id { get; set; }
        public int DataId { get; set; }
        public string Name { get; set; }
        public float Tolerance { get; set; }
        public bool ToleranceIsPercent { get; set; }
        public string UnitOfMeasure { get; set; }
        public float MeasurementBefore { get; set; }
        public float MeasurementAfter { get; set; }
        public float Setting { get; set; }
        public Parameter() { }
        public Parameter(string name)
        {
            Name = name;
        }
        public Parameter(string name, float tolerance, bool isPercent, string uom, float measureBefore, float measureAfter)
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
