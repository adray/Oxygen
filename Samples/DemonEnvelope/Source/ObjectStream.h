#pragma once
#include <ObjectStream.h>
#include <memory>

namespace DE
{
    class Network;
    class Level;

    class ObjectStream : public Oxygen::ObjectStream
    {
    public:
        ObjectStream(std::shared_ptr<Level>& level, Network& network);
        virtual void OnNewObject(const Oxygen::Object& ev, Oxygen::Message& msg);
        virtual void OnUpdateObject(const Oxygen::Object& ev, Oxygen::Message& msg);
        virtual void OnDeleteObject(int id);
        virtual void OnStreamEnded();
        virtual ~ObjectStream();
    private:
        std::shared_ptr<Level> _level;
        Network& _network;
    };
}
