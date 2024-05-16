#include "Level_Entity.h"
#include "ConfigReader.h"

using namespace DE;

//===================
// Level_Entity class
//===================

Level_Entity::Level_Entity()
    :
    _flags(0),
    _px(0),
    _py(0),
    _tileId(0),
    _active(false),
    _spriteId(0)
{
}

void Level_Entity::SetPos(int px, int py)
{
    _px = px;
    _py = py;
}


void EntityConfig::Load(ConfigReader& cfg, ISLANDER_DEVICE device)
{
    while (cfg.ReadNextRow())
    {
        const int id = std::strtol(cfg.Get("Id").c_str(), 0, 10);
        const std::string name = cfg.Get("name");

        Entity_Cfg entity = {};
        entity.name = name;
        IslanderFindMaterialTexture(device, name.c_str(), &entity._texture);

        _config.insert(std::pair<int, Entity_Cfg>(id, entity));
    }
}

