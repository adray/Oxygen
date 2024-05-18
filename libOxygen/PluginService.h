#pragma once
#include <string>
#include <memory>
#include <functional>
#include "Subscriber.h"

namespace Oxygen
{
    class ClientConnection;

    class PluginNotificationStream : public Subscriber
    {
    public:
        PluginNotificationStream(const std::string& name, std::function<void(const std::string&, int)> handler);
        virtual void OnNewMessage(Message& msg);
        virtual ~PluginNotificationStream();

    private:
        std::function<void(const std::string&, int)> _handler;
        std::string _name;
    };

    class PluginService
    {
    public:
        PluginService(ClientConnection* conn);
        void SchedulePlugin(const std::string& name);
        bool IsRunning(const std::string& name);
        void SetCompletedHandler(std::function<void(const std::string&)>& handler) { _completedHandler = handler; }

    private:
        void NotificationCallback(const std::string& name, int state);
        void OnTaskCompleted(const std::string& name);

        ClientConnection* _conn;
        std::unordered_map<std::string, std::shared_ptr<PluginNotificationStream>> _streams;
        std::function<void(const std::string&)> _completedHandler;
    };
}
