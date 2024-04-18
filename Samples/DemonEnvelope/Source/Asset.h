#pragma once
#include <string>

namespace DE
{
    struct Asset
    {
        std::string name;
        bool onDisk;
        bool onServer;

        Asset() : onDisk(false), onServer(false) {}
    };
}
