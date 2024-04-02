#pragma once
#include <memory>
#include <vector>
#include <string>
#include "Level.h"

namespace DE
{
    class Network;

    class Editor
    {
    public:
        void Start(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_);
        void Run(float delta, ISLANDER_WINDOW window);
        void Draw(float delta, ISLANDER_DEVICE device, ISLANDER_WINDOW window, CRIMSON_HANDLE crimson, IslanderImguiContext* cxt);
        inline std::shared_ptr<Level>& GetLevel() { return level; }

    private:
        std::shared_ptr<DE::Network> network;
        std::vector<std::string> assets;
        std::vector<std::string> levels;
        std::shared_ptr<Level> level;
        Scripting _scripting;
        ScriptBuilder _scriptBuilder;
        char selectedLevelName[256];
        char levelName[256];
        char username[256];
        char password[256];
        char hostname[256];
        int palette = -1;
        int palette_layer = 0;
        bool left_down = false;
        float cursorTime = 0;
        int cursorTile = -1;
        bool update = false;
    };
}
