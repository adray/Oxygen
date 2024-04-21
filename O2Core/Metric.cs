namespace Oxygen
{
    public class Metric
    {
        public string Labels { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }

        public Metric(string name, string label, string type)
        {
            this.Name = name;
            this.Labels = label;
            this.Type = type;
        }
    }

    public class GaugeMetric : Metric
    {
        public double Value { get; set; }

        public GaugeMetric(string name, string label)
            : base(name, label, "gauge")
        {
        }
    }

    public class CounterMetric : Metric
    {
        public double Value { get; set; }

        public CounterMetric(string name, string label)
            : base(name, label, "counter")
        {
        }
    }
}
