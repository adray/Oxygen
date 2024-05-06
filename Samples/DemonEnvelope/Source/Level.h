#pragma once
#include "Tilemap.h"
#include "Scripting.h"
#include "SpriteBatch.h"
#include "Level_Entity.h"
#include <vector>
#include <unordered_map>
#include <array>

namespace DE
{
    class Dialogue
    {
    public:
        Dialogue();
        void Show(const std::string& name, const std::string& dialogue);
        void Show(const std::string& dialogue);
        void Hide();

        inline bool HasName() const { return _hasName; }
        inline bool HasDialogue() const { return _hasDialogue; }
        inline std::string Name() const { return _name; }
        inline std::string DialogueText() const { return _dialogue; }

        inline bool IsShowing() const { return _isShowing; }

    private:
        bool _isShowing;
        bool _hasName;
        bool _hasDialogue;
        std::string _name;
        std::string _dialogue;
    };

    class Level
    {
    public:
        void Setup(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_, std::shared_ptr<EntityConfig> entityCfg);
        void Loaded();
        void Render(IslanderRenderable* renderables, int* cur_index,
            const int tilemappixelShader, const int tilemapvertexShader,
            const int spriteBatchpixelShader, const int spriteBatchvertexShader);
        void CreateTilemap();

        inline std::shared_ptr<Tileset> TileSet() { return tileset; }
        bool TileMapHitTest(int x, int y) const;
        Tilemap& GetTilemap() { return _tilemaps; }
        void Reset();

        void AddScript(ScriptObject& script);
        void DeleteScript(int id);
        ScriptObject* GetScript(int id);
        inline std::vector<ScriptObject>& Scripts() { return _scripts; }
        void StartScripts();
        void RunScripts(float delta);
        const Scripting& ScriptSystem() const { return _scripting; }

        void ClearEntities();
        int AddEntity();
        void RemoveEntity(int entity);
        void SetEntityPos(int entity, int px, int py);
        void GetEntityPos(int entity, int* px, int* py);

        inline Dialogue& GetDialogue() { return _dialogue; }

    private:
        Tilemap _tilemaps;
        Scripting _scripting;
        std::vector<ScriptObject> _scripts;
        std::shared_ptr<Tileset> tileset;
        std::shared_ptr<EntityConfig> _entityCfg;
        ISLANDER_POLYGON_LIBRARY _lib;
        SpriteBatch _sprites;
        std::array<Level_Entity, 128> _entities;
        int _numEntities;
        Dialogue _dialogue;
    };
}
