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

        inline const Message& Request() { return _request; };
        void Signal(const std::function<void (Message&)>& callback);
        void NewMessage(const Message& msg);

    private:
        Message _request;
        std::vector<std::function<void(Message&)>> _callback;
    };
}
