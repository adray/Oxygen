#include "ObjectStream.h"
#include "DeltaCompress.h"

constexpr int NEW_OBJECT = 0;
constexpr int UPDATE_OBJECT = 1;
constexpr int DELETE_OBJECT = 2;
constexpr int END_STREAM = 255;

using namespace Oxygen;

ObjectStream::ObjectStream()
    : 
    Subscriber(Oxygen::Message("LEVEL_SVR", "OBJECT_STREAM"))
{
    _Request().Prepare();
}

void ObjectStream::OnNewMessage(Message& msg)
{
    const int type = msg.ReadInt32();
    switch (type)
    {
    case NEW_OBJECT: // ADD
        NewObject(msg);
        break;
    case UPDATE_OBJECT: // UPDATE
        UpdateObject(msg);
        break;
    case DELETE_OBJECT: // DELETE
        DeleteObject(msg);
        break;
    case END_STREAM: // END
        state.clear();
        OnStreamEnded();
        break;
    }
}

void ObjectStream::NewObject(Message& msg)
{
    Object ev = {};

    ev.id = msg.ReadInt32();
    ev.pos[0] = msg.ReadDouble();
    ev.pos[1] = msg.ReadDouble();
    ev.pos[2] = msg.ReadDouble();
    ev.scale[0] = msg.ReadDouble();
    ev.scale[1] = msg.ReadDouble();
    ev.scale[2] = msg.ReadDouble();
    ev.rot[0] = msg.ReadDouble();
    ev.rot[1] = msg.ReadDouble();
    ev.rot[2] = msg.ReadDouble();

    const auto& data = msg.data();
    std::vector<unsigned char> initialData(data, data + msg.size());
    state.insert(std::pair<int, std::vector<unsigned char>>(ev.id, initialData));

    int hasCustomData = msg.ReadInt32();
    if (hasCustomData)
    {
        ev.hasCustomData = true;
    }

    OnNewObject(ev, msg);
}

void ObjectStream::UpdateObject(Message& msg)
{
    const int id = msg.ReadInt32();

    const int numBytes = msg.ReadInt32();
    unsigned char* data = new unsigned char[numBytes];
    msg.ReadBytes(numBytes, data);

    std::vector<unsigned char> newData;
    std::vector<unsigned char>& initialData = state[id];
    Oxygen::Decompress(initialData.data(), initialData.size(), data, numBytes, newData);

    delete[] data;

    Oxygen::Message decompressedMessage(newData.data(), newData.size());
    state[id] = newData;

    const int msgType = decompressedMessage.ReadInt32();

    Object ev = { };
    ev.id = decompressedMessage.ReadInt32();
    ev.pos[0] = decompressedMessage.ReadDouble();
    ev.pos[1] = decompressedMessage.ReadDouble();
    ev.pos[2] = decompressedMessage.ReadDouble();
    ev.scale[0] = decompressedMessage.ReadDouble();
    ev.scale[1] = decompressedMessage.ReadDouble();
    ev.scale[2] = decompressedMessage.ReadDouble();
    ev.rot[0] = decompressedMessage.ReadDouble();
    ev.rot[1] = decompressedMessage.ReadDouble();
    ev.rot[2] = decompressedMessage.ReadDouble();

    int hasCustomData = decompressedMessage.ReadInt32();
    if (hasCustomData)
    {
        ev.hasCustomData = true;
    }

    OnUpdateObject(ev, decompressedMessage);
}

void ObjectStream::DeleteObject(Message& msg)
{
    OnDeleteObject(msg.ReadInt32());
}

void ObjectStream::OnNewObject(const Object& ev, Message& msg)
{

}

void ObjectStream::OnUpdateObject(const Object& ev, Message& msg)
{

}

void ObjectStream::OnDeleteObject(int id)
{

}

void ObjectStream::OnStreamEnded()
{

}

Message ObjectStream::BuildAddMessage(const Object& obj)
{
    Oxygen::Message msg("LEVEL_SVR", "ADD_OBJECT");
    msg.WriteDouble(obj.pos[0]); // Pos X
    msg.WriteDouble(obj.pos[1]); // Pos Y
    msg.WriteDouble(obj.pos[2]); // Pos Z
    msg.WriteDouble(obj.scale[0]); // Scale X
    msg.WriteDouble(obj.scale[1]); // Scale Y
    msg.WriteDouble(obj.scale[2]); // Scale Z
    msg.WriteDouble(obj.rot[0]); // Rot X
    msg.WriteDouble(obj.rot[1]); // Rot Y
    msg.WriteDouble(obj.rot[2]); // Rot Z
    msg.WriteInt32(obj.hasCustomData ? 1 : 0);    // custom
    return msg;
}

void ObjectStream::PrepareAddMessage(Message* msg, const Object& obj)
{
    msg->Prepare();
}

Message ObjectStream::BuildUpdateMessage(const Object& obj)
{
    Message msg("LEVEL_SVR", "OBJECT_STREAM");

    msg.WriteInt32(NEW_OBJECT);
    msg.WriteInt32(obj.id);
    msg.WriteDouble(obj.pos[0]); // Pos X
    msg.WriteDouble(obj.pos[1]); // Pos Y
    msg.WriteDouble(obj.pos[2]); // Pos Z
    msg.WriteDouble(obj.scale[0]); // Scale X
    msg.WriteDouble(obj.scale[1]); // Scale Y
    msg.WriteDouble(obj.scale[2]); // Scale Z
    msg.WriteDouble(obj.rot[0]); // Rot X
    msg.WriteDouble(obj.rot[1]); // Rot Y
    msg.WriteDouble(obj.rot[2]); // Rot Z
    msg.WriteInt32(obj.hasCustomData ? 1 : 0);    // custom

    return msg;
}

void ObjectStream::PrepareUpdateMessage(Message* msg, const Object& obj)
{
    std::vector<unsigned char>& stateData = state[obj.id];

    unsigned char* newData;
    int numBytes = Oxygen::Compress(stateData.data(), stateData.size(), msg->data() + 4, msg->size() - 4, &newData);
    if (numBytes > 0)
    {
        Oxygen::Message msg2("LEVEL_SVR", "UPDATE_OBJECT");
        msg2.WriteInt32(obj.id);
        msg2.WriteBytes(numBytes, newData);
        delete[] newData;

        msg2.Prepare();
        *msg = msg2;
    }
}

ObjectStream::~ObjectStream()
{

}
