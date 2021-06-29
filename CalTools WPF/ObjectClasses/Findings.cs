using System.Collections.Generic;

namespace CalTools_WPF.ObjectClasses
{
    public class Findings : ICTObject
    {
        public int Id { get; set; } = -1;
        public int DataId { get; set; } = -1;
        public string Name { get; set; }
        public float Tolerance { get; set; }
        public bool ToleranceIsPercent { get; set; }
        public string UnitOfMeasure { get; set; }
        public float MeasurementBefore { get; set; }
        public float MeasurementAfter { get; set; }
        public float Setting { get; set; }
        public Findings() { }
        public Findings(string name)
        {
            Name = name;
        }
        public Findings(Dictionary<string, string> values)
        {
            ParseParameters(values);
        }
        public void ParseParameters(Dictionary<string, string> values)
        {
            Id = int.Parse(values["id"]);
            DataId = int.Parse(values["task_data_id"]);
            Name = values["name"];
            Tolerance = float.Parse(values["tolerance"]);
            ToleranceIsPercent = values["tolerance_is_percent"] == "1";
            UnitOfMeasure = values["unit_of_measure"];
            MeasurementBefore = float.Parse(values["measurement_before"]);
            MeasurementAfter = float.Parse(values["measurement_after"]);
            Setting = float.Parse(values["setting"]);
        }
    }
}
