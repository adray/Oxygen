#pragma once
#include <string>
#include <functional>
#include "Message.h"

namespace Oxygen
{
    class Subscriber
    {
    public:

        Subscriber(const Message& msg);

        inline const Message& Request() const { return _request; };
        void Signal(const std::function<void (Message&)>& callback);
        void NewMessage(const Message& msg);
        virtual void OnNewMessage(Message& msg);

        virtual ~Subscriber();
    
    protected:
        inline Message& _Request() { return _request; };

    private:
        Message _request;
        std::vector<std::function<void(Message&)>> _callback;
    };
}
