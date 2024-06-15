#include "Metrics.h"
#include "ClientConnection.h"
#include "Message.h"
#include "Subscriber.h"

using namespace Oxygen;

Metrics_Pos::Metrics_Pos(const std::string& name)
    : _x(0), _y(0), _z(0), _name(name)
{
}

void Metrics_Pos::Update(double x, double y, double z)
{
    _x = x;
    _y = y;
    _z = z;
}

void Metrics_Pos::Write(Message& msg)
{
    msg.WriteString(_name);
    msg.WriteString("pos");
    msg.WriteString(_labels);
    msg.WriteDouble(_x);
    msg.WriteDouble(_y);
    msg.WriteDouble(_z);
}

Metrics_Counter::Metrics_Counter(const std::string& name)
    : _name(name), _value(0)
{
}

void Metrics_Counter::Write(Message& msg) 
{
    msg.WriteString(_name);
    msg.WriteString("counter");
    msg.WriteString(_labels);
    msg.WriteDouble(_value);
}

Metrics_Gauge::Metrics_Gauge(const std::string& name)
    : _name(name), _value(0)
{
}

void Metrics_Gauge::Write(Message& msg)
{
    msg.WriteString(_name);
    msg.WriteString("gauge");
    msg.WriteString(_labels);
    msg.WriteDouble(_value);
}

//===============
// Metrics class
//===============

Metrics::Metrics(ClientConnection* conn)
    :
    _conn(conn)
{
    _subscriber = std::make_shared<Subscriber>(Message("METRIC_SVR", "METRIC_COLLECTION"));
    _conn->AddSubscriber(_subscriber);
    _subscriber->Signal([this](Message& msg)
        {
            ReportMetrics();
        });

    _numCollections = std::make_shared<Metrics_Counter>("oxygen_client_num_metric_collections_counter");
    AddMetric(_numCollections);

    _numBytesReceived = std::make_shared<Metrics_Counter>("oxygen_client_bytes_received_total");
    AddMetric(_numBytesReceived);

    _numBytesSent = std::make_shared<Metrics_Counter>("oxygen_client_bytes_sent_total");
    AddMetric(_numBytesSent);
}

void Metrics::AddMetric(std::shared_ptr<Metrics_Pos>& metric)
{
    _posMetrics.push_back(metric);
}

void Metrics::AddMetric(std::shared_ptr<Metrics_Counter>& metric)
{
    _counterMetrics.push_back(metric);
}

void Metrics::AddMetric(std::shared_ptr<Metrics_Gauge>& metric)
{
    _gaugeMetrics.push_back(metric);
}

void Metrics::ReportMetrics()
{
    _numCollections->Increment(1.0);
    _numBytesSent->Update(_conn->NumBytesSent());
    _numBytesReceived->Update(_conn->NumBytesReceived());

    for (auto& metric : _posMetrics)
    {
        ReportPosMetric(metric);
    }

    for (auto& metric : _counterMetrics)
    {
        ReportCounterMetric(metric);
    }

    for (auto& metric : _gaugeMetrics)
    {
        ReportGaugeMetric(metric);
    }
}

void Metrics::ReportPosMetric(std::shared_ptr<Metrics_Pos>& metric)
{
    Message msg("METRIC_SVR", "REPORT_METRIC");
    metric->Write(msg);

    std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
    sub->Signal([this, sub2 = std::shared_ptr<Subscriber>(sub)](Message& response)
        {
            _conn->RemoveSubscriber(sub2);

            if (response.ReadString() == "NACK")
            {
                // Error
            }
        });

    _conn->AddSubscriber(sub);
}

void Metrics::ReportGaugeMetric(std::shared_ptr<Metrics_Gauge>& metric)
{
    Message msg("METRIC_SVR", "REPORT_METRIC");
    metric->Write(msg);

    std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
    sub->Signal([this, sub2 = std::shared_ptr<Subscriber>(sub)](Message& response)
        {
            _conn->RemoveSubscriber(sub2);

            if (response.ReadString() == "NACK")
            {
                // Error
            }
        });

    _conn->AddSubscriber(sub);
}

void Metrics::ReportCounterMetric(std::shared_ptr<Metrics_Counter>& metric)
{
    Message msg("METRIC_SVR", "REPORT_METRIC");
    metric->Write(msg);

    std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
    sub->Signal([this, sub2 = std::shared_ptr<Subscriber>(sub)](Message& response)
        {
            _conn->RemoveSubscriber(sub2);

            if (response.ReadString() == "NACK")
            {
                // Error
            }
        });

    _conn->AddSubscriber(sub);
}
