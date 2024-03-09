#include "Level.h"
#include "Message.h"
#include "Subscriber.h"
#include "DeltaCompress.h"
#include <iostream>

static bool ACK(Oxygen::Message& msg)
{
    if ("NACK" == msg.ReadString())
    {
        const int errorCode = msg.ReadInt32();
        const std::string errorMsg = msg.ReadString();
        std::cout << errorCode << ": " << errorMsg << std::endl;
        return false;
    }

    return true;
}

void Level::OnObjectAdded(Oxygen::Message& msg)
{
    std::cout << "NEW OBJECT" << std::endl;

    const int id = msg.ReadInt32();
    std::cout << "ID " << id << std::endl;
    std::cout << "PosX " << msg.ReadDouble() << std::endl;
    std::cout << "PosY " << msg.ReadDouble() << std::endl;
    std::cout << "PosZ " << msg.ReadDouble() << std::endl;
    std::cout << "ScaleX " << msg.ReadDouble() << std::endl;
    std::cout << "ScaleY " << msg.ReadDouble() << std::endl;
    std::cout << "ScaleZ " << msg.ReadDouble() << std::endl;
    std::cout << "RotX " << msg.ReadDouble() << std::endl;
    std::cout << "RotY " << msg.ReadDouble() << std::endl;
    std::cout << "RotZ " << msg.ReadDouble() << std::endl;
    std::cout << "Model ID " << msg.ReadInt32() << std::endl;

    const auto& data = msg.data();
    std::vector<unsigned char> initialData(data, data + msg.size());
    state.insert(std::pair<int, std::vector<unsigned char>>(id, initialData));
}

void Level::OnObjectStreamed(Oxygen::ClientConnection* conn, Oxygen::Message& msg)
{
    int type = msg.ReadInt32();
    switch (type)
    {
    case 0: // NEW
        OnObjectAdded(msg);
        break;
    case 1: // UPDATE
        std::cout << "UPDATE OBJECT" << std::endl;

        DecompressMessage(msg);

        break;
    }
}

void Level::OpenLevel(Oxygen::ClientConnection* conn)
{
    Oxygen::Message msg("LEVEL_SVR", "OBJECT_STREAM");
    msg.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub(new Oxygen::Subscriber(msg));
    sub->Signal([this, conn](Oxygen::Message& msg) {
        std::cout << msg.NodeName() << " " << msg.MessageName() << std::endl;
        OnObjectStreamed(conn, msg);
        });
    conn->AddSubscriber(sub);

    double pos[3] = { -10.0, 50.0, 0.0 };
    AddObject(conn, pos);

    pos[0] = 10.0;
    UpdateObject(conn, 0, pos);
}

void Level::DecompressMessage(Oxygen::Message& msg)
{
    const int id = msg.ReadInt32();

    const int numBytes = msg.ReadInt32();
    unsigned char* data = new unsigned char[numBytes];
    msg.ReadBytes(numBytes, data);

    std::vector<unsigned char> newData;
    std::vector<unsigned char> initialData = state[id];
    Oxygen::Decompress(initialData.data(), initialData.size(), data, numBytes, newData);

    delete[] data;

    Oxygen::Message decompressedMessage(newData.data(), newData.size());

    const int msgType = decompressedMessage.ReadInt32();
    std::cout << "ID " << decompressedMessage.ReadInt32() << std::endl;
    std::cout << "PosX " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "PosY " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "PosZ " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "ScaleX " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "ScaleY " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "ScaleZ " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "RotX " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "RotY " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "RotZ " << decompressedMessage.ReadDouble() << std::endl;
    std::cout << "Model ID " << decompressedMessage.ReadInt32() << std::endl;
}

void Level::UpdateObject(Oxygen::ClientConnection* conn, int id, double* pos)
{
    Oxygen::Message msg("LEVEL_SVR", "UPDATE_OBJECT");
    msg.WriteInt32(id);
    msg.WriteDouble(pos[0]); // pos x
    msg.WriteDouble(pos[1]); // pos y
    msg.WriteDouble(pos[2]); // pos z
    msg.WriteDouble(1.0); // scale x
    msg.WriteDouble(1.0); // scale y
    msg.WriteDouble(1.0); // scale z
    msg.WriteDouble(0.0); // rotation x
    msg.WriteDouble(0.0); // rotation y
    msg.WriteDouble(0.0); // rotation z
    //msg.WriteInt32(0);    // model ID
    msg.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub(new Oxygen::Subscriber(msg));
    sub->Signal([conn, &sub](Oxygen::Message& msg) {
        std::cout << msg.NodeName() << " " << msg.MessageName() << std::endl;
        ACK(msg);
        conn->RemoveSubscriber(sub);
        });
    conn->AddSubscriber(sub);
}

void Level::AddObject(Oxygen::ClientConnection* conn, double* pos)
{
    Oxygen::Message msg("LEVEL_SVR", "ADD_OBJECT");
    msg.WriteString("MODEL");
    msg.WriteDouble(pos[0]); // pos x
    msg.WriteDouble(pos[1]); // pos y
    msg.WriteDouble(pos[2]); // pos z
    msg.WriteDouble(1.0); // scale x
    msg.WriteDouble(1.0); // scale y
    msg.WriteDouble(1.0); // scale z
    msg.WriteDouble(0.0); // rotation x
    msg.WriteDouble(0.0); // rotation y
    msg.WriteDouble(0.0); // rotation z
    msg.WriteInt32(0);    // model ID
    msg.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub(new Oxygen::Subscriber(msg));
    sub->Signal([conn, &sub](Oxygen::Message& msg) {
        std::cout << msg.NodeName() << " " << msg.MessageName() << std::endl;
        ACK(msg);
        conn->RemoveSubscriber(sub);
        });
    conn->AddSubscriber(sub);
}
