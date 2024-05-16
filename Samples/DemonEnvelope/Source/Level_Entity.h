#pragma once
#include <API.h>
#include <unordered_map>
#include <string>

namespace DE
{
    class ConfigReader;

    enum class EntityFlags
    {
        None = 0x0,
        Hidden = 0x1,
        CanTrigger = 0x2
    };

    class Level_Entity
    {
    public:
        Level_Entity();
        void SetPos(int px, int py);
        inline int X() const { return _px; }
        inline int Y() const { return _py; }
        inline bool IsActive() const { return _active; }
        inline void SetActive(bool active) { _active = active; }
        inline void SetTileId(int tile) { _tileId = tile; }
        inline void SetSpriteId(int id) { _spriteId = id; }
        inline void SetFlags(int flags) { _flags = flags; }
        inline int TileId() const { return _tileId; }
        inline int SpriteId() const { return _spriteId; }
        inline int GetFlags() const { return _flags; }

    private:
        bool _active;
        int _px;
        int _py;
        int _tileId;
        int _spriteId;
        int _flags;
    };

    struct Entity_Cfg
    {
        Islander::component_texture _texture;
        std::string name;
    };

    class EntityConfig
    {
    public:
        void Load(ConfigReader& cfg, ISLANDER_DEVICE device);
        inline Entity_Cfg& GetSprite(int id) { return _config[id]; }

    private:
        std::unordered_map<int, Entity_Cfg> _config;
    };
}
