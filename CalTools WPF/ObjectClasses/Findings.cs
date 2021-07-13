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
        public void ParseParameters(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("id") && parameters["id"] != "")
            { Id = int.Parse(parameters["id"]); }

            if (parameters.ContainsKey("task_data_id") && parameters["task_data_id"] != "")
            { DataId = int.Parse(parameters["task_data_id"]); }

            Name = parameters["name"];
            Tolerance = float.Parse(parameters["tolerance"]);
            ToleranceIsPercent = parameters["tolerance_is_percent"] == "1";
            UnitOfMeasure = parameters["unit_of_measure"];
            MeasurementBefore = float.Parse(parameters["measurement_before"]);
            MeasurementAfter = float.Parse(parameters["measurement_after"]);
            Setting = float.Parse(parameters["setting"]);
        }
    }
}
