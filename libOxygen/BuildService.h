#pragma once
#include <string>
#include <functional>
#include "DownloadStream.h"

namespace Oxygen
{
    class BuildService
    {
    public:
        BuildService(ClientConnection* conn, const std::string& dir);

        void DownloadArtefact(const std::string& name, std::function<void()> callback);

    private:
        ClientConnection* _conn;
        std::string _dir;
        DownloadStream _stream;
    };
}
