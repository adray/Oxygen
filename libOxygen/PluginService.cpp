#include "PluginService.h"
#include "ClientConnection.h"
#include <iostream>

using namespace Oxygen;

constexpr int TASK_STARTED = 0;
constexpr int TASK_COMPLETED = 1;
constexpr int STREAM_ENDED = 255;

Message BuildRequest(const std::string& name)
{
    Message request = Message("PLUGIN_SVR", "NOTIFICATION_STREAM");
    request.WriteString(name);
    return request;
}

PluginNotificationStream::PluginNotificationStream(const std::string& name, std::function<void(const std::string&, int)> handler) 
    :
    Subscriber(BuildRequest(name)),
    _handler(handler),
    _name(name)
{
}

void PluginNotificationStream::OnNewMessage(Message& msg)
{
    const int type = msg.ReadInt32();
    switch (type)
    {
    case TASK_STARTED:
        std::cout << "Task started" << std::endl;
        if (_handler)
        {
            _handler(_name, TASK_STARTED);
        }
        break;
    case TASK_COMPLETED:
        std::cout << "Task completed" << std::endl;
        if (_handler)
        {
            _handler(_name, TASK_COMPLETED);
        }
        break;
    case STREAM_ENDED:
        std::cout << "Stream ended" << std::endl;
        if (_handler)
        {
            _handler(_name, STREAM_ENDED);
        }
        break;
    }
}

PluginNotificationStream::~PluginNotificationStream() {}

PluginService::PluginService(ClientConnection* conn) : _conn(conn) {
}


void PluginService::OnTaskCompleted(const std::string& name)
{
    Message msg = Message("PLUGIN_SVR", "CLOSE_NOTIFICATION_STREAM");
    msg.WriteString(name);

    std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(Subscriber(msg));
    sub->Signal([this, name, sub2 = std::shared_ptr<Subscriber>(sub)](Message msg)
        {
            if (msg.ReadString() == "NACK")
            {
            }

            _conn->RemoveSubscriber(sub2);
        });
    _conn->AddSubscriber(sub);

    if (_completedHandler)
    {
        _completedHandler(name);
    }
}

void PluginService::NotificationCallback(const std::string& name, int state)
{
    const auto& it = _streams.find(name);
    if (it != _streams.end())
    {
        switch (state)
        {
        case STREAM_ENDED:
            _conn->RemoveSubscriber(it->second);
            _streams.erase(it);
            break;
        case TASK_COMPLETED:
            OnTaskCompleted(name);
            break;
        }
    }
}

void PluginService::SchedulePlugin(const std::string& name)
{
    if (_streams.find(name) == _streams.end())
    {
        std::shared_ptr<PluginNotificationStream> notify = std::make_shared<PluginNotificationStream>(PluginNotificationStream(name, [this](const std::string& nm, int s)
            {
                NotificationCallback(nm, s);
            }));
        _streams.insert(std::pair<std::string, std::shared_ptr<PluginNotificationStream>>(name, notify));
        _conn->AddSubscriber(notify);

        Message msg = Message("PLUGIN_SVR", "SCHEDULE_PLUGIN");
        msg.WriteString(name);
        msg.WriteInt32(1);

        std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(Subscriber(msg));
        sub->Signal([this, name, sub2 = std::shared_ptr<Subscriber>(sub)](Message msg)
            {
                if (msg.ReadString() == "NACK")
                {
                    _conn->RemoveSubscriber(_streams[name]);
                }

                _conn->RemoveSubscriber(sub2);
            });
        _conn->AddSubscriber(sub);
    }
}

bool PluginService::IsRunning(const std::string& name)
{
    return _streams.find(name) != _streams.end();
}

