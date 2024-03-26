#include "EventStream.h"
#include <iostream>

using namespace DE;

void EventStream::OnUserConnected(std::int64_t id, const std::string& name)
{
    std::cout << "User connected ID: " << id << " Name: " << name << std::endl;
}

void EventStream::OnUserDisconnected(std::int64_t id, const std::string& name)
{
    std::cout << "User disconnected ID: " << id << " Name: " << name << std::endl;
}

void EventStream::OnUserCursorMove(std::int64_t id, int objectId, int subId)
{
    //std::cout << "User moved cursor ID: " << id << " ObjectID: " << objectId << " SubID: " << subId << std::endl;
}

void EventStream::OnStreamEnded()
{
    std::cout << "Event stream closed" << std::endl;

}

EventStream::~EventStream()
{

}
