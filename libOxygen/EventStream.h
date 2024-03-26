#pragma once
#include <vector>
#include "Subscriber.h"

namespace Oxygen
{
    class EventStream : public Subscriber
    {
    public:
        struct User
        {
            std::int64_t id;
            std::string name;
            int objectId;
            int subId;
        };

        EventStream();

        virtual void OnNewMessage(Message& msg);
        virtual void OnUserConnected(std::int64_t id, const std::string& name);
        virtual void OnUserDisconnected(std::int64_t id, const std::string& name);
        virtual void OnUserCursorMove(std::int64_t id, int objectId, int subId);
        virtual void OnStreamEnded();

        inline const std::vector<User>& Users() const { return _users; }

        ~EventStream();

    private:
        std::vector<User> _users;

        void UserConnected(Message& msg);
        void UserDisonnected(Message& msg);
        void UserCursorMoved(Message& msg);
    };
}
