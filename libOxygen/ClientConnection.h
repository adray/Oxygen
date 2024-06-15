#pragma once
#include <string>
#include <memory>
#include <functional>

namespace Oxygen
{
    constexpr int DEFAULT_PORT = 9888;

    class ClientConnectionImpl;
    class Message;
    class Subscriber;
    class Security;

    class ClientConnection
    {
    public:
        ClientConnection(const std::string& host, int port);
        
        bool IsConnected();

        void WriteMessage(const Message& msg);

        void AddSubscriber(std::shared_ptr<Subscriber> subscriber);
        void RemoveSubscriber(const std::shared_ptr<Subscriber> subscriber);
        void Process(bool wait);

        void Logon(const std::string& username, const std::string& password);
        void LogonHandler(const std::function<void(int errCode, const std::string& text)>& handler);

        int NumBytesSent() const;
        int NumBytesReceived() const;

        ~ClientConnection();
    private:

        struct Handler
        {
            bool active = false;
            std::function<void(int errCode, const std::string& text)> handler;
        };

        void HashPassword(const std::string& password, Message& msg);
        void OnLogonSuccess();
        void OnLogonFailed(int errCode, const std::string& text);

        ClientConnectionImpl* impl;
        Security* security;
        Handler logonHandler;
    };
}
