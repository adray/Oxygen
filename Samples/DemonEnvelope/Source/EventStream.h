#pragma once
#include <EventStream.h>

namespace DE
{
    class Network;

    class EventStream : public Oxygen::EventStream
    {
    public:
        
        EventStream(Network& network);

        virtual void OnUserConnected(std::int64_t id, const std::string& name);
        virtual void OnUserDisconnected(std::int64_t id, const std::string& name);
        virtual void OnUserCursorMove(std::int64_t id, int objectId, int subId);
        virtual void OnStreamEnded();

        virtual ~EventStream();
    private:
        Network& _network;
    };
}
