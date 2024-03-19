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
        void Start(ISLANDER_POLYGON_LIBRARY lib, ISLANDER_DEVICE device);
        void Run(float delta, ISLANDER_WINDOW window);
        void Draw(float delta, ISLANDER_DEVICE device, IslanderImguiContext* cxt);
        inline Level& GetLevel() { return level; }

    private:
        std::shared_ptr<DE::Network> network;
        std::vector<std::string> assets;
        std::vector<std::string> levels;
        Level level;
        char levelName[256];
        char username[256];
        char password[256];
        char hostname[256];
        int palette = -1;
        bool left_down = false;
    };
}
