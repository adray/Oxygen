using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
	internal class MetricsServer : Node
	{
		private readonly NodeTimer collectionTimer = new NodeTimer(30000);
		private readonly List<Request> clients = new List<Request>();

		public MetricsServer() : base("METRIC_SVR")
		{
			Metrics.Start();

			this.AddTimer(collectionTimer);
		}

		public override void AddMetric(Metric metric)
		{
			base.AddMetric(metric);

			Metrics.AddMetric(metric);
		}

		public override void OnTimer(NodeTimer timer)
		{
			base.OnTimer(timer);

			if (timer == collectionTimer)
			{
				foreach (var client in clients)
				{
					client.Send(new Message(this.Name, "METRIC_COLLECTION"));
				}

				Metrics.Collect();
			}
		}

		public override void OnClientDisconnected(Client client)
		{
			base.OnClientDisconnected(client);

			for (int i = 0; i < clients.Count; i++)
			{
				if (clients[i].Client == client)
				{
					clients.RemoveAt(i);
					break;
				}
			}
		}

		public override void OnRecieveMessage(Request request)
		{
			base.OnRecieveMessage(request);

			if (!Authorizer.IsAuthorized(request))
			{
				return;
			}

			var msg = request.Message;

			if (msg.MessageName == "REPORT_METRIC")
			{
				string metricName = msg.ReadString();
				string metricType = msg.ReadString();
				string metricLabels = msg.ReadString();

				string? username = request.Client.GetProperty("USER_NAME") as string;

				if (username != null)
				{
					if (string.IsNullOrEmpty(metricLabels))
					{
						metricLabels = $"user={username}";
					}
					else
					{
						metricLabels += $";user={username}";
					}
				}

				if (metricType == "gauge")
				{
					double value = msg.ReadDouble();

					Metrics.ReportGaugeMetric(value, metricName, metricLabels);

					SendAck(request, msg.MessageName);
				}
				else if (metricType == "counter")
				{
					double value = msg.ReadDouble();

					Metrics.ReportCounterMetric(value, metricName, metricLabels);

					SendAck(request, msg.MessageName);
				}
				else if (metricType == "pos")
				{
					double posX = msg.ReadDouble();
					double posY = msg.ReadDouble();
					double posZ = msg.ReadDouble();

					double[] pos = new double[]
					{
						posX, posY, posZ
					};

					Metrics.ReportPosMetric(pos, metricName, metricLabels);

					SendAck(request, msg.MessageName);
				}
				else
				{
					SendNack(request, 200, "Metric type is not valid.", msg.MessageName);
				}
			}
			else if (msg.MessageName == "METRIC_COLLECTION")
			{
				clients.Add(request);
			}
			else
			{
				SendNack(request, 100, "Invalid request", msg.MessageName);
			}
		}
	}
}
