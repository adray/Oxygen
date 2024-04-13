using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
	internal static class Metrics
	{
		private const string MetricFolder = "Metrics";
		private const string PosMetricHeader = "timestamp,x,y,z,labels";
		private const string GaugeMetricHeader = "timestamp,value,labels";
		private const string CounterMetricHeader = "timestamp,value,labels";

		public static void Start()
		{
			if (!Directory.Exists(MetricFolder))
			{
				Directory.CreateDirectory(MetricFolder);
			}
		}

		private static StreamWriter? OpenWriter(string name, string header)
		{
			string path = MetricFolder + "\\" + name + ".csv";
			bool append = File.Exists(path);

			StreamWriter? writer = null;
			try
			{
				writer = new StreamWriter(path, append);
			}
			catch
			{
				Logger.Instance.Log("Unable to write to metrics file {0}.", path);
			}

			if (!append && writer != null)
			{
				writer.WriteLine(header);
			}

			return writer;
		}

		private static long GetTimestamp()
		{
			return DateTime.Now.Ticks;
		}

		public static void ReportPosMetric(double[] pos, string name, string labels)
		{
			using (StreamWriter? writer = OpenWriter(name, PosMetricHeader))
				if (writer != null)
				{
					writer.WriteLine("{0},{1},{2},{3},{4}", GetTimestamp(), pos[0], pos[1], pos[2], labels);
				}
		}

		public static void ReportGaugeMetric(double value, string name, string labels)
		{
			using (StreamWriter? writer = OpenWriter(name, GaugeMetricHeader))
				if (writer != null)
				{
					writer.WriteLine("{0},{1},{2}", GetTimestamp(), value, labels);
				}
		}

		public static void ReportCounterMetric(double value, string name, string labels)
		{
			using (StreamWriter? writer = OpenWriter(name, CounterMetricHeader))
				if (writer != null)
				{
					writer.WriteLine("{0},{1},{2}", GetTimestamp(), value, labels);
				}
		}
	}
}
