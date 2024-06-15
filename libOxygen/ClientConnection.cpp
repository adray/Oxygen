#include "ClientConnection.h"
#include "Message.h"
#include "Subscriber.h"
#include "Security.h"

#include <WinSock2.h>
#include <ws2tcpip.h>
#include <sstream>
#include <thread>
#include <functional>
#include <queue>
#include <mutex>
#include <condition_variable>
#include <iostream>

#pragma comment(lib, "Ws2_32.lib")

using namespace std;
using namespace Oxygen;

namespace Oxygen
{
    template<typename T>
    class ReaderWriterQueue
    {
    public:
        inline void Enqueue(const T& item)
        {
            std::unique_lock<decltype(_lock)>(_lock);
            _queue.push(item);
        }

        inline T Dequeue()
        {
            std::unique_lock<decltype(_lock)>(_lock);
            const T front = _queue.front();
            _queue.pop();
            return front;
        }

        inline size_t Size() { return _queue.size(); }

    private:
        std::queue<T> _queue;
        std::mutex _lock;
    };

    //============================================================

    class WaitHandle
    {
    public:

        WaitHandle();

        void WaitOne(std::unique_lock<std::mutex>& lock);
        void WaitOne(std::unique_lock<std::mutex>& lock, int timeout);
        void Set();

    private:
        std::mutex mutex;
        std::condition_variable condition;
        bool writeData;
    };

    WaitHandle::WaitHandle() : writeData(false) {}

    void WaitHandle::WaitOne(std::unique_lock<std::mutex>& lock, int timeout)
    {
        std::unique_lock<std::mutex> temp(mutex);
        lock.swap(temp);

        // Acquire the lock and then release the lock when wait is called
        // Then wait until can process data, when the lock is then reacquired
        // Set the process flag and release the lock
        //condition.wait(lock, [this] { return writeData; });
        condition.wait_for(lock, std::chrono::seconds(timeout), [this] { return writeData; });
        writeData = false;
    }

    void WaitHandle::WaitOne(std::unique_lock<std::mutex>& lock)
    {
        std::unique_lock<std::mutex> temp(mutex);
        lock.swap(temp);

        // Acquire the lock and then release the lock when wait is called
        // Then wait until can process data, when the lock is then reacquired
        // Set the process flag and release the lock
        condition.wait(lock, [this] { return writeData; });
        writeData = false;
    }

    void WaitHandle::Set()
    {
        {
            // acquire the lock, once held allow the queue to be processed
            std::unique_lock<decltype(mutex)>lock(mutex);
            writeData = true;
        }

        condition.notify_one();
    }

    //============================================================

    class ClientConnectionImpl
    {
    public:
        ClientConnectionImpl(const std::string& host, int port);

        inline bool Connected() { return connected; }
        void ReadThread();
        void WriteThread();
        void HeartbeatThread();
        void WriteMessage(const Message& msg);
        void AddSubscriber(std::shared_ptr<Subscriber>& subscriber);
        void RemoveSubscriber(const std::shared_ptr<Subscriber>& subscriber);
        void Process(bool wait);
        inline int NumBytesSent() const { return numBytesSent; }
        inline int NumBytesReceived() const { return numBytesReceived; }
        ~ClientConnectionImpl();

    private:
        bool connected;
        bool running;
        SOCKET sock;
        std::vector<std::shared_ptr<Subscriber>> subscribers;
        std::unique_ptr<std::thread> heartbeat;
        std::unique_ptr<std::thread> write;
        std::unique_ptr<std::thread> read;
        ReaderWriterQueue<Message> writeQueue;
        ReaderWriterQueue<Message> readQueue;
        WaitHandle writeWaitHandle;
        WaitHandle readWaitHandle;
        WaitHandle heartbeatHandle;
        int subscriberId;
        int numBytesSent;
        int numBytesReceived;
    };
}

ClientConnectionImpl::ClientConnectionImpl(const std::string& host, int port)
    : running(true), sock(0L), subscriberId(0), numBytesReceived(0), numBytesSent(0)
{
    connected = true;

    WSADATA wsaData;
    const int error = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (error != 0)
    {
        connected = false;
    }

    if (connected)
    {
        sock = socket(AF_UNSPEC, SOCK_STREAM, IPPROTO_TCP);

        if (sock == INVALID_SOCKET)
        {
            const int errorCode = WSAGetLastError();
            connected = false;
        }
    }

    PADDRINFOA addresses;
    if (connected)
    {
        std::stringstream ss;
        ss << port;

        addrinfo hint;
        std::memset(&hint, 0, sizeof(hint));
        hint.ai_family = AF_UNSPEC;
        hint.ai_socktype = SOCK_STREAM;

        const int retval = getaddrinfo(host.c_str(), ss.str().c_str(), &hint, &addresses);
        if (retval != 0)
        {
            connected = false;
        }
    }

    if (connected)
    {
        connected = false;

        addrinfo* info = addresses;
        while (info)
        {
            sockaddr* addr = info->ai_addr;
            size_t size = info->ai_addrlen;

            const int retval = connect(sock, addr, (int)size);
            if (retval == 0)
            {
                connected = true;
                break;
            }
            
            const int error = WSAGetLastError();
            info = info->ai_next;
        }
    }

    if (connected)
    {
        write.reset(new std::thread(&ClientConnectionImpl::WriteThread, this));
        read.reset(new std::thread(&ClientConnectionImpl::ReadThread, this));
        heartbeat.reset(new std::thread(&ClientConnectionImpl::HeartbeatThread, this));
    }
}

void ClientConnectionImpl::HeartbeatThread()
{
    // Sends a heartbeat message to the server at a fixed interval.

    while (running)
    {
        //std::this_thread::sleep_for(std::chrono::seconds(30));
        std::unique_lock<std::mutex>lock;
        heartbeatHandle.WaitOne(lock, 30);

        Message msg("HEARTBEAT", "");
        msg.Prepare();
        WriteMessage(msg);
    }
}

void ClientConnectionImpl::ReadThread()
{
    const int maxBytes = 2048*32;
    unsigned char* bytes = new unsigned char[maxBytes];

    while (running)
    {
        int consumed = recv(sock, (char*)bytes, 8, MSG_WAITALL);
        if (consumed == -1)
        {
            // Disconnected
            break;
        }

        numBytesReceived += consumed;

        const int totalBytes =
            bytes[0] |
            (bytes[1] << 8) |
            (bytes[2] << 16) |
            (bytes[3] << 24);
        const int id = 
            bytes[4] |
            (bytes[5] << 8) |
            (bytes[6] << 16) |
            (bytes[7] << 24);

        consumed = recv(sock, (char*)bytes, totalBytes, MSG_WAITALL);

        if (consumed == -1)
        {
            // Disconnected
            break;
        }

        numBytesReceived += consumed;

        Message msg(bytes, totalBytes);
        msg.SetId(id);
        readQueue.Enqueue(msg);
        readWaitHandle.Set();
    }

    delete[] bytes;
}

void ClientConnectionImpl::WriteThread()
{
    while (running)
    {
        std::unique_lock<std::mutex>lock;
        writeWaitHandle.WaitOne(lock);

        while (writeQueue.Size() > 0)
        {
            const Message msg = writeQueue.Dequeue();

            const unsigned char* buffer = msg.data();
            const size_t size = msg.size();

            const int error = send(sock, (char*)buffer, (int)size, 0);

            numBytesSent += size;
        }
    }
}

void ClientConnectionImpl::WriteMessage(const Message& msg)
{
    writeQueue.Enqueue(msg);
    writeWaitHandle.Set();
}

void ClientConnectionImpl::AddSubscriber(std::shared_ptr<Subscriber>& subscriber)
{
    subscriber->SetId(subscriberId++);
    WriteMessage(subscriber->Request());

    subscribers.push_back(subscriber);
}

void ClientConnectionImpl::RemoveSubscriber(const std::shared_ptr<Subscriber>& subscriber)
{
    const auto& it = std::find(subscribers.begin(), subscribers.end(), subscriber);
    if (it != subscribers.end())
    {
        subscribers.erase(it);
    }
}

void ClientConnectionImpl::Process(bool wait)
{
    std::unique_lock<std::mutex> lock;
    if (wait)
    {
        readWaitHandle.WaitOne(lock);
    }

    while (readQueue.Size() > 0)
    {
        const Message msg = readQueue.Dequeue();

        // Add to a list as NewMessage can add/remove a subscriber..
        std::vector<std::shared_ptr<Subscriber>> activate;
        for (auto& sub : subscribers)
        {
            if (sub->Request().NodeName() == msg.NodeName() &&
                sub->Request().MessageName() == msg.MessageName() &&
                sub->Id() == msg.Id())
            {
                activate.push_back(sub);
            }
        }

        for (auto& sub : activate)
        {
            sub->NewMessage(msg);
        }
    }
}

ClientConnectionImpl::~ClientConnectionImpl()
{
    running = false;
    closesocket(sock);
    heartbeatHandle.Set();
    writeWaitHandle.Set();

    write->join();
    heartbeat->join();
    read->join();
}

// ==========================================================================

ClientConnection::ClientConnection(const std::string& host, int port)
{
    impl = new ClientConnectionImpl(host, port);
    security = new Security();
}

bool ClientConnection::IsConnected()
{
    return impl->Connected();
}

void ClientConnection::WriteMessage(const Message& msg)
{
    impl->WriteMessage(msg);
}

void ClientConnection::AddSubscriber(std::shared_ptr<Subscriber> subscriber)
{
    impl->AddSubscriber(subscriber);
}

void ClientConnection::RemoveSubscriber(const std::shared_ptr<Subscriber> subscriber)
{
    impl->RemoveSubscriber(subscriber);
}

void ClientConnection::Process(bool wait)
{
    impl->Process(wait);
}

void ClientConnection::HashPassword(const std::string& password, Message& msg)
{
    unsigned char* hash;
    unsigned int size;
    security->SHA256(password, &hash, &size);

    msg.WriteBytes(size, hash);

    delete[] hash;
}

void ClientConnection::Logon(const std::string& username, const std::string& password)
{
    Message request("LOGIN_SVR", "LOGIN");
    request.WriteString(username);
    HashPassword(password, request);
    std::shared_ptr<Subscriber> logSub = std::shared_ptr<Subscriber>(new Subscriber(request));
    AddSubscriber(logSub);
    logSub->Signal([this, sub2 = std::shared_ptr<Subscriber>(logSub)](Message& msg) {
        RemoveSubscriber(sub2);
        if (msg.ReadString() == "NACK")
        {
            const int errCode = msg.ReadInt32();
            const std::string errText = msg.ReadString();
            OnLogonFailed(errCode, errText);
        }
        else
        {
            OnLogonSuccess();
        }
        });
}

void ClientConnection::LogonHandler(const std::function<void(int errCode, const std::string& text)>& handler)
{
    logonHandler.active = true;
    logonHandler.handler = handler;
}

void ClientConnection::OnLogonSuccess()
{
    if (logonHandler.active)
    {
        logonHandler.handler(0, "");
    }
}

void ClientConnection::OnLogonFailed(int errCode, const std::string& text)
{
    if (logonHandler.active)
    {
        logonHandler.handler(errCode, text);
    }
}

int ClientConnection::NumBytesSent() const
{
    return impl->NumBytesSent();
}

int ClientConnection::NumBytesReceived() const
{
    return impl->NumBytesReceived();
}

ClientConnection::~ClientConnection()
{
    delete impl;
    impl = nullptr;

    delete security;
    security = nullptr;
}

