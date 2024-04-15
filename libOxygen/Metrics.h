#pragma once
#include <string>
#include <vector>
#include <memory>

namespace Oxygen
{
    class ClientConnection;
    class Message;
    class Subscriber;

    class Metrics_Pos
    {
    public:
        Metrics_Pos(const std::string& name);
        void Update(double x, double y, double z);
        inline void SetLabels(const std::string& labels) { _labels = labels; }
        void Write(Message& msg);

    private:
        std::string _name;
        std::string _labels;
        double _x, _y, _z;
    };

    class Metrics_Counter
    {
    public:
        Metrics_Counter(const std::string& name);
        inline void Update(double value) { _value = value; }
        inline void Increment(double by) { _value += by; }
        inline void SetLabels(const std::string& labels) { _labels = labels; }
        void Write(Message& msg);

    private:
        std::string _name;
        std::string _labels;
        double _value;
    };

    class Metrics_Gauge
    {
    public:
        Metrics_Gauge(const std::string& name);
        inline void Update(double value) { _value = value; }
        inline void SetLabels(const std::string& labels) { _labels = labels; }
        void Write(Message& msg);

    private:
        std::string _name;
        std::string _labels;
        double _value;
    };

    class Metrics
    {
    public:
        Metrics(ClientConnection* conn);

        void AddMetric(std::shared_ptr<Metrics_Pos>& metric);
        void AddMetric(std::shared_ptr<Metrics_Counter>& metric);
        void AddMetric(std::shared_ptr<Metrics_Gauge>& metric);

    private:

        void ReportMetrics();
        void ReportPosMetric(std::shared_ptr<Metrics_Pos>& metric);
        void ReportGaugeMetric(std::shared_ptr<Metrics_Gauge>& metric);
        void ReportCounterMetric(std::shared_ptr<Metrics_Counter>& metric);

        ClientConnection* _conn;
        std::vector<std::shared_ptr<Metrics_Pos>> _posMetrics;
        std::vector<std::shared_ptr<Metrics_Counter>> _counterMetrics;
        std::vector<std::shared_ptr<Metrics_Gauge>> _gaugeMetrics;
        std::shared_ptr<Subscriber> _subscriber;

        std::shared_ptr<Metrics_Counter> _numCollections;
        std::shared_ptr<Metrics_Counter> _numBytesSent;
        std::shared_ptr<Metrics_Counter> _numBytesReceived;
    };
}
