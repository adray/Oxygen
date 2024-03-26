#pragma once
#include <EventStream.h>

namespace DE
{
    class EventStream : public Oxygen::EventStream
    {
    public:
        
        virtual void OnUserConnected(std::int64_t id, const std::string& name);
        virtual void OnUserDisconnected(std::int64_t id, const std::string& name);
        virtual void OnUserCursorMove(std::int64_t id, int objectId, int subId);
        virtual void OnStreamEnded();

        virtual ~EventStream();
    };
}
