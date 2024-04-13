#include "EventStream.h"

using namespace Oxygen;

constexpr int USER_CONNECTED = 0;
constexpr int USER_DISCONNECTED = 1;
constexpr int USER_CURSOR_MOVED = 2;
constexpr int END_STREAM = 255;

EventStream::EventStream()
    : Subscriber(Message("LEVEL_SVR", "EVENT_STREAM"))
{
}

void EventStream::OnNewMessage(Message& msg)
{
    const int type = msg.ReadInt32();
    switch (type)
    {
    case USER_CONNECTED:
        UserConnected(msg);
        break;
    case USER_DISCONNECTED:
        UserDisonnected(msg);
        break;
    case USER_CURSOR_MOVED:
        UserCursorMoved(msg);
        break;
    case END_STREAM:
        OnStreamEnded();
        break;
    }
}

void EventStream::OnUserConnected(std::int64_t id, const std::string& name) {}

void EventStream::OnUserDisconnected(std::int64_t id, const std::string& name) {}

void EventStream::OnUserCursorMove(std::int64_t id, int objectId, int subId) {}

void EventStream::OnStreamEnded() {}

void EventStream::UserConnected(Message& msg)
{
    User user;

    user.id = msg.ReadInt64();
    user.name = msg.ReadString();
    user.objectId = -1;
    user.subId = 0;

    _users.push_back(user);

    OnUserConnected(user.id, user.name);
}

void EventStream::UserDisonnected(Message& msg)
{
    User user;

    user.id = msg.ReadInt64();
    user.name = msg.ReadString();

    for (int i = 0; i < _users.size(); i++)
    {
        if (_users[i].id == user.id)
        {
            _users.erase(_users.begin() + i);

            OnUserDisconnected(user.id, user.name);
            break;
        }
    }
}

void EventStream::UserCursorMoved(Message& msg)
{
    std::int64_t id = msg.ReadInt64();
    const int objectId = msg.ReadInt32();
    const int subId = msg.ReadInt32();

    for (int i = 0; i < _users.size(); i++)
    {
        if (_users[i].id == id)
        {
            _users[i].objectId = objectId;
            _users[i].subId = subId;
            break;
        }
    }

    OnUserCursorMove(
        id,
        objectId,
        subId
    );
}

EventStream::~EventStream() {}
