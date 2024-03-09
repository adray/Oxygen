#pragma once
#include <string>
#include <memory>

namespace Oxygen
{
    constexpr int DEFAULT_PORT = 9888;

    class ClientConnectionImpl;
    class Message;
    class Subscriber;

    class ClientConnection
    {
    public:
        ClientConnection(const std::string& host, int port);
        
        bool IsConnected();

        void WriteMessage(const Message& msg);

        void AddSubscriber(std::shared_ptr<Subscriber>& subscriber);
        void RemoveSubscriber(std::shared_ptr<Subscriber>& subscriber);
        void Process(bool wait);

        ~ClientConnection();
    private:
        ClientConnectionImpl* impl;
    };
}
