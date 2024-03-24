#include "Subscriber.h"

using namespace Oxygen;

Subscriber::Subscriber(const Message& msg)
    : _request(msg)
{

}

void Subscriber::Signal(const std::function<void(Message&)>& callback)
{
    _callback.push_back(callback);
}

void Subscriber::NewMessage(const Message& msg)
{
    OnNewMessage(msg);

    for (auto& callback : _callback)
    {
        Message clone(msg);
        callback(clone);
    }
}

void Subscriber::OnNewMessage(const Message& msg)
{
    // By default this does nothing.
}

Subscriber::~Subscriber()
{
    // By default this does nothing.
}
