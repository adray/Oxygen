#include "Level.h"
#include "Message.h"
#include "DeltaCompress.h"
#include <iostream>

using namespace DE;

//==================
// Dialogue
//==================

Dialogue::Dialogue()
    : _hasDialogue(false), _hasName(false), _isShowing(false)
{
}

void Dialogue::Show(const std::string& name, const std::string& dialogue)
{
    _name = name;
    _dialogue = dialogue;

    _hasDialogue = true;
    _hasName = true;
    _isShowing = true;
}

void Dialogue::Show(const std::string& dialogue)
{
    _dialogue = dialogue;
    _hasDialogue = true;
    _isShowing = true;
}

void Dialogue::Hide()
{
    _hasDialogue = false;
    _hasName = false;
    _isShowing = false;
}

//==================
// NPC class
//==================

NPCObject::NPCObject()
    : _px(0), _py(0), _spriteId(-1), _id(0), _version(0)
{
}

void NPCObject::Serialize(Oxygen::Message& msg)
{
    msg.WriteInt32(_px);
    msg.WriteInt32(_py);
    msg.WriteInt32(_spriteId);
}

void NPCObject::Deserialize(Oxygen::Message& msg)
{
    _px = msg.ReadInt32();
    _py = msg.ReadInt32();
    _spriteId = msg.ReadInt32();
}

//==================
// Level class
//==================

void Level::Setup(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_, std::shared_ptr<EntityConfig> entityCfg)
{
    _lib = lib;

    tileset = tileset_;
    _entityCfg = entityCfg;

    _sprites.Initialize(lib);
}

void Level::Loaded()
{
}

void Level::CreateTilemap()
{
    _tilemaps.CreateMesh(_lib, 10, 10, 16, 9);
    _tilemaps.SetTileSet(tileset);
}

void Level::Render(IslanderRenderable* renderables, int* cur_index,
    const int tilemappixelShader, const int tilemapvertexShader,
    const int spriteBatchpixelShader, const int spriteBatchvertexShader)
{
    _tilemaps.Update();

    const int numlayers = _tilemaps.NumLayers();
    for (int i = 0; i < numlayers; i++)
    {
        auto& layer = _tilemaps.GetLayer(i);

        if (layer.Visible())
        {
            IslanderRenderable* renderable = &renderables[*cur_index];
            std::memset(renderable, 0, sizeof(IslanderRenderable));

            renderable->mesh.pixelShader = tilemappixelShader;
            renderable->mesh.vertexShader = tilemapvertexShader;
            renderable->mesh.geometryShader = -1;
            renderable->mesh.polydata = _tilemaps.GetMesh();
            renderable->mesh.parentEntity = -1;
            renderable->mesh.material.slot_data[0] = _tilemaps.GetTileset()->GetTexture();
            renderable->mesh.material.slot_flags[0] = ISLANDER_RENDERABLE_MATERIAL_SLOT_CUSTOM;

            Tilemap_ConstantBuffer* buffer = layer.GetConstantBuffer();
            renderable->mesh.constantBufferData = buffer;
            renderable->mesh.constantBufferDataSize = sizeof(Tilemap_ConstantBuffer);

            (*cur_index)++;
        }
    }

    _sprites.Reset();

    for (int i = 0; i < _numEntities; i++)
    {
        auto& entity = _entities[i];
        if (entity.IsActive() &&
            entity.X() >= _tilemaps.ScrollX() &&
            entity.Y() >= _tilemaps.ScrollY() &&
            entity.X() < _tilemaps.ScrollX() + _tilemaps.ViewWidth() &&
            entity.Y() < _tilemaps.ScrollY() + _tilemaps.ViewHeight())
        {
            Entity_Cfg& cfg = _entityCfg->GetSprite(entity.SpriteId());

            float pos[3] = {
                (entity.X() - _tilemaps.ScrollX()) * 16,
                (entity.Y() - _tilemaps.ScrollY()) * 9,
                0.0f
            };
            float uv[4] =
            {
                cfg._texture.px,
                cfg._texture.py,
                cfg._texture.sx,
                cfg._texture.sy
            };

            _sprites.AddSprite(SpriteBatch_Sprite(pos, uv));
        }
    }

    _sprites.Update();

    IslanderRenderable* renderable = &renderables[*cur_index];
    std::memset(renderable, 0, sizeof(IslanderRenderable));

    renderable->mesh.pixelShader = spriteBatchpixelShader;
    renderable->mesh.vertexShader = spriteBatchvertexShader;
    renderable->mesh.geometryShader = -1;
    renderable->mesh.polydata = _sprites.GetMesh();
    renderable->mesh.parentEntity = -1;
    renderable->mesh.material.slot_data[0] = tileset->GetTexture();
    renderable->mesh.material.slot_flags[0] = ISLANDER_RENDERABLE_MATERIAL_SLOT_CUSTOM;

    SpriteBatch_ConstantBuffer* buffer = _sprites.ConstantData();
    renderable->mesh.constantBufferData = buffer;
    renderable->mesh.constantBufferDataSize = sizeof(SpriteBatch_ConstantBuffer);

    (*cur_index)++;
}

bool Level::TileMapHitTest(int x, int y) const
{
    if (_tilemaps.HitTest(x, y) >= 0)
    {
        return true;
    }

    return false;
}

void Level::Reset()
{
    _tilemaps.Clear();
    _scripts.clear();
    _dialogue.Hide();
    ClearEntities();
}

void Level::AddScript(ScriptObject& script)
{
    _scripts.push_back(script);
}

void Level::DeleteScript(int id)
{
    const auto& it = std::find_if(_scripts.begin(), _scripts.end(), [&id](ScriptObject& obj) {
            return obj.ID() == id;
        });

    if (it != _scripts.end())
    {
        _scripts.erase(it);
    }
}

ScriptObject* Level::GetScript(int id)
{
    for (int i = 0; i < _scripts.size(); i++)
    {
        ScriptObject* script = &_scripts[i];
        if (script->ID() == id)
        {
            return script;
        }
    }

    return nullptr;
}

void Level::StartScripts()
{
    _scripting.ClearScripts();

    for (auto& script : _scripts)
    {
        script.CompileScript("../../../../Assets");

        unsigned char* program = script.Program();
        if (!program)
        {
            std::cout << "Failed to start Script '" << script.ScriptName() << "'" << std::endl;
            continue;
        }

        if (script.Trigger() == ScriptTrigger::None)
        {
            _scripting.AddScript(program, this, -1);
            script.SetTriggered(true);
        }
        else
        {
            script.SetTriggered(false);
        }
    }
}

void Level::RunScripts(float delta)
{
    _scripting.RunScripts(delta);
}

void Level::ClearEntities()
{
    _numEntities = 0;
}

int Level::AddEntity()
{
    int id = -1;
    for (int i = 0; i < _numEntities; i++)
    {
        if (!_entities[i].IsActive())
        {
            id = i;
            break;
        }
    }

    if (id == -1 && _numEntities < _entities.size())
    {
        id = _numEntities;
        _numEntities++;
    }

    if (id != -1)
    {
        _entities[id].SetActive(true);
    }

    return id;
}

void Level::RemoveEntity(int entity)
{
    _entities[entity].SetActive(false);
}

void Level::SetEntityPos(int entity, int px, int py)
{
    Level_Entity& ent = _entities[entity];
    if (px >= 0 && px < _tilemaps.Width() &&
        py >= 0 && py < _tilemaps.Height())
    {
        auto& mask = _tilemaps.GetCollisionMask();
        if (!mask.Get(px + py * _tilemaps.Width()))
        {
            bool hit = false;
            for (int i = 0; i < _entities.size(); i++)
            {
                Level_Entity& other = _entities[i];
                if (other.IsActive() && other.X() == px && other.Y() == py)
                {
                    hit = true;
                    break;
                }
            }

            if (!hit)
            {
                ent.SetPos(px, py);

                if ((ent.GetFlags() & (int)EntityFlags::CanTrigger) == (int)EntityFlags::CanTrigger)
                {
                    for (auto& script : _scripts)
                    {
                        if (!script.Program())
                        {
                            continue;
                        }

                        if (script.IsTriggered())
                        {
                            continue;
                        }

                        if (script.Trigger() == ScriptTrigger::OnTouch &&
                            px == script.X() && py == script.Y())
                        {
                            _scripting.AddScript(script.Program(), this, -1);
                            script.SetTriggered(true);
                        }
                    }
                }
            }
        }
    }
}

void Level::GetEntityPos(int entity, int* px, int* py)
{
    Level_Entity& ent = _entities[entity];
    *px = ent.X();
    *py = ent.Y();
}

void Level::SetEntitySprite(int entity, int spriteId)
{
    Level_Entity& ent = _entities[entity];
    ent.SetSpriteId(spriteId);
}

void Level::SetEntityFlags(int entity, EntityFlags flags)
{
    auto& ent = _entities[entity];
    int curFlags = ent.GetFlags();
    ent.SetFlags(curFlags | (int)flags);
}

void Level::AddNPC(NPCObject& npc)
{
    _npc.push_back(npc);
}

void Level::DeleteNPC(int id)
{
    const auto& it = std::find_if(_npc.begin(), _npc.end(), [&id](NPCObject& obj)
        {
            return obj.ID() == id;
        });

    if (it != _npc.end())
    {
        _npc.erase(it);
    }
}

NPCObject* Level::GetNPC(int id)
{
    const auto& it = std::find_if(_npc.begin(), _npc.end(), [&id](NPCObject& obj)
        {
            return obj.ID() == id;
        });

    if (it != _npc.end())
    {
        return &(*it);
    }

    return nullptr;
}
