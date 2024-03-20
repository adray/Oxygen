#include "Level.h"
#include "Message.h"
#include "DeltaCompress.h"
#include <iostream>

using namespace DE;

void Level::Setup(ISLANDER_POLYGON_LIBRARY lib, ISLANDER_DEVICE device)
{
    _lib = lib;

    tileset = std::shared_ptr<Tileset>(new Tileset());
    tileset->Load(device);
}

void Level::Loaded()
{

}

void Level::OnNewObject(Oxygen::Message& msg)
{
    const int id = msg.ReadInt32();
    std::cout << "NEW_OBJECT" << std::endl;
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

    const auto& data = msg.data();
    std::vector<unsigned char> initialData(data, data + msg.size());
    state.insert(std::pair<int, std::vector<unsigned char>>(id, initialData));

    int hasCustomData = msg.ReadInt32();
    if (hasCustomData)
    {
        const std::string name = msg.ReadString();

        if (name == "TILEMAP")
        {
            _tilemaps.push_back(Tilemap(id));
            const auto& test = _tilemaps.rbegin();
            test->CreateMesh(_lib, 10, 10, 16, 9);
            test->SetTileSet(tileset);
            test->Deserialize(msg);
        }
    }
}

void Level::OnUpdateObject(Oxygen::Message& msg)
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
    state[id] = newData;

    const int msgType = decompressedMessage.ReadInt32();
    std::cout << "UPDATE_OBJECT" << std::endl;
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

    int hasCustomData = decompressedMessage.ReadInt32();
    if (hasCustomData)
    {
        const std::string name = decompressedMessage.ReadString();

        if (name == "TILEMAP")
        {
            for (int i = 0; i < _tilemaps.size(); i++)
            {
                if (_tilemaps[i].ID() == id)
                {
                    const auto& test = _tilemaps.rbegin();
                    test->Deserialize(decompressedMessage);
                    break;
                }
            }
        }
    }
}

void Level::OnDeleteObject(Oxygen::Message& msg)
{

}

void Level::Render(IslanderRenderable* renderables, int* cur_index, const int tilemappixelShader, const int tilemapvertexShader)
{
    IslanderRenderable* renderable = &renderables[*cur_index];
    std::memset(renderable, 0, sizeof(IslanderRenderable));

    for (int i = 0; i < _tilemaps.size(); i++)
    {
        auto& tilemap = _tilemaps[i];

        renderable->mesh.pixelShader = tilemappixelShader;
        renderable->mesh.vertexShader = tilemapvertexShader;
        renderable->mesh.geometryShader = -1;
        renderable->mesh.polydata = tilemap.GetMesh();
        renderable->mesh.parentEntity = -1;
        renderable->mesh.material.slot_data[0] = tilemap.GetTileset()->GetTexture();
        renderable->mesh.material.slot_flags[0] = ISLANDER_RENDERABLE_MATERIAL_SLOT_CUSTOM;

        Tilemap::ConstantBuffer* buffer = tilemap.GetConstantBuffer();
        renderable->mesh.constantBufferData = buffer;
        renderable->mesh.constantBufferDataSize = sizeof(Tilemap::ConstantBuffer);

        tilemap.Update();

        (*cur_index)++;
    }
}

int Level::TileMapHitTest(int x, int y) const
{
    for (int i = 0; i < _tilemaps.size(); i++)
    {
        if (_tilemaps[i].HitTest(x, y) >= 0)
        {
            return i;
        }
    }

    return -1;
}
