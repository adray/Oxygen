#pragma once
#include <memory>
#include <API.h>

namespace DE
{
    class Tileset;
    class Tilemap;
    class Level;
    class Level_Entity;

    class GameState_Map
    {
    public:
        void Initialize(std::shared_ptr<Tileset>& tileset);
        void SetLevel(std::shared_ptr<Level>& level);
        void Run(float delta, ISLANDER_WINDOW window);

        inline int Player() const { return _player; }

    private:
        std::shared_ptr<Tileset> _tileSet;
        std::shared_ptr<Level> _level;
        int _player;
        float _moveElapsed;
    };

    class GameState_Battle
    {
    public:
        void Run(float delta);

    };

    class GameMenu
    {
    public:
        void Run(float delta);
        void Draw(float delta);


    };

    class Game
    {
    public:
        void Initialize(std::shared_ptr<Tileset>& tileset);
        void Start(std::shared_ptr<Level>& level, int px, int py);
        void Stop();
        void Run(float delta, ISLANDER_WINDOW window);
        void Draw(float detla);

    private:
        enum class State
        {
            Map,
            Battle
        };

        State _state;
        GameState_Map _map;
        GameState_Battle _battle;
        GameMenu _menu;
        bool _running;
    };
}
