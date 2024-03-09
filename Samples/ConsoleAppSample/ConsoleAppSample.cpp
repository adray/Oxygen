
#include <iostream>
#include "ClientConnection.h"
#include "Message.h"
#include "Subscriber.h"
#include "Level.h"

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

void OnLevelLoaded(Oxygen::ClientConnection* conn)
{
    Level* level = new Level();
    level->OpenLevel(conn);
}

void OnNewLevel(Oxygen::ClientConnection* conn)
{
    Oxygen::Message msg("LEVEL_SVR", "LOAD_LEVEL");
    msg.WriteString("myLevel");
    msg.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub(new Oxygen::Subscriber(msg));
    sub->Signal([conn,&sub](Oxygen::Message& msg) {
        std::cout << msg.NodeName() << " " << msg.MessageName() << std::endl;
        conn->RemoveSubscriber(sub);
        if (ACK(msg))
        {
            OnLevelLoaded(conn);
        }
        });
    conn->AddSubscriber(sub);
}

void OnLoggedIn(Oxygen::ClientConnection* conn)
{
    Oxygen::Message msg2("LEVEL_SVR", "NEW_LEVEL");
    msg2.WriteString("myLevel");
    msg2.Prepare();
    std::shared_ptr<Oxygen::Subscriber> sub(new Oxygen::Subscriber(msg2));
    sub->Signal([conn,&sub](Oxygen::Message& msg) {
        std::cout << msg.NodeName() << " " << msg.MessageName() << std::endl;
        conn->RemoveSubscriber(sub);
        ACK(msg);
        OnNewLevel(conn);
        });
    conn->AddSubscriber(sub);
}

int main(int numArgs, char** args)
{
    Oxygen::ClientConnection* conn = new Oxygen::ClientConnection("localhost", Oxygen::DEFAULT_PORT);

    if (conn->IsConnected())
    {
        std::cout << "Connected" << std::endl;

        Oxygen::Message msg("LOGIN_SVR", "LOGIN_API_KEY");
        msg.WriteString(args[1]);
        msg.Prepare();

        std::shared_ptr<Oxygen::Subscriber> sub(new Oxygen::Subscriber(msg));
        sub->Signal([conn,&sub](Oxygen::Message& msg) {
            std::cout << msg.NodeName() << " " << msg.MessageName() << std::endl;
            conn->RemoveSubscriber(sub);
            if (ACK(msg))
            {
                OnLoggedIn(conn);
            }
            });
        conn->AddSubscriber(sub);

    }
    
    while (true)
    {
        conn->Process(true);
    }

    std::getchar();
}
