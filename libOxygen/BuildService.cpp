#include "BuildService.h"

using namespace Oxygen;

BuildService::BuildService(ClientConnection* conn, const std::string& dir)
    : _conn(conn), _dir(dir), _stream(_conn, "BUILD_SVR", "ARTEFACT_DOWNLOAD_STREAM")
{
}

void BuildService::DownloadArtefact(const std::string& name, std::function<void()> callback)
{
    _stream.Download(_dir, name, callback);
}
