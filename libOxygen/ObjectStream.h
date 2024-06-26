#pragma once
#include "Subscriber.h"

namespace Oxygen
{
    struct Object
    {
        int id;
        int version;
        double pos[3];
        double scale[3];
        double rot[3];
        int numCustomDataBytes;
    };

    class ObjectStream : public Subscriber
    {
    public:
        ObjectStream();

        virtual void OnNewMessage(Message& msg);

        virtual void OnNewObject(const Object& ev, Message& msg);
        virtual void OnUpdateObject(const Object& ev, Message& msg);
        virtual void OnDeleteObject(int id);
        virtual void OnStreamEnded();

        Message BuildAddMessage(const Object& obj);
        void PrepareAddMessage(Message* msg, const Object& obj);

        Message BuildUpdateMessage(const Object& obj);
        void PrepareUpdateMessage(Message* msg, const Object& obj);

        virtual ~ObjectStream();

    private:
        void NewObject(Message& msg);
        void UpdateObject(Message& msg);
        void DeleteObject(Message& msg);

        std::unordered_map<int, std::vector<unsigned char>> state;
        int _customDataPos;
    };
}
