#include "Game.h"
#include "Tilemap.h"
#include "Level.h"
#include <Window.h>

using namespace DE;

//===================
//GameState_Map class
//====================

void GameState_Map::Initialize(std::shared_ptr<Tileset>& tileset)
{
    _tileSet = tileset;
}

void GameState_Map::Run(float delta, ISLANDER_WINDOW window)
{
    _moveElapsed += delta;

    int px; int py;
    _level->GetEntityPos(_player, &px, &py);

    if (_moveElapsed > 0.1f)
    {
        auto& map = _level->GetTilemap();
        const int scrollX = map.ScrollX();
        const int scrollY = map.ScrollY();

        if (IslanderIsKeyDown(window, Islander::KEY_RIGHT))
        {
            _level->SetEntityPos(_player, px + 1, py);
            IslanderSetKeyUp(window, Islander::KEY_RIGHT);
            _moveElapsed = 0.0f;
        }
        else if (IslanderIsKeyDown(window, Islander::KEY_LEFT))
        {
            _level->SetEntityPos(_player, px - 1, py);
            IslanderSetKeyUp(window, Islander::KEY_LEFT);
            _moveElapsed = 0.0f;
        }
        else if (IslanderIsKeyDown(window, Islander::KEY_UP))
        {
            _level->SetEntityPos(_player, px, py - 1);
            IslanderSetKeyUp(window, Islander::KEY_UP);
            _moveElapsed = 0.0f;
        }
        else if (IslanderIsKeyDown(window, Islander::KEY_DOWN))
        {
            _level->SetEntityPos(_player, px, py + 1);
            IslanderSetKeyUp(window, Islander::KEY_DOWN);
            _moveElapsed = 0.0f;
        }

        // Scroll the map if the player has moved to the next screen.
        _level->GetEntityPos(_player, &px, &py);
        const int srX = (px / 10) * 10;
        const int srY = (py / 10) * 10;
        if (srX != scrollX ||
            srY != scrollY)
        {
            map.SetScrollPos(srX, srY);
        }
    }
}

void GameState_Map::SetLevel(std::shared_ptr<Level>& level)
{
    _level = level;

    _level->ClearEntities();
    _player = level->AddEntity();
    _moveElapsed = 0.0f;
}

//====================
//GameState_Battle class
//====================

void GameState_Battle::Run(float delta)
{

}

//======================
//GameMenu class
//======================

void GameMenu::Run(float delta)
{

}

void GameMenu::Draw(float delta)
{

}

//==================
// Game class
//==================

void Game::Initialize(std::shared_ptr<Tileset>& tileset)
{
    _map.Initialize(tileset);
}

void Game::Start(std::shared_ptr<Level>& level, int px, int py)
{
    _running = true;
    _map.SetLevel(level);
    level->SetEntityPos(_map.Player(), px, py);
}

void Game::Stop()
{
    _running = false;
}

void Game::Run(float delta, ISLANDER_WINDOW window)
{
    if (_running)
    {
        if (_state == State::Map)
        {
            _map.Run(delta, window);
        }
        else if (_state == State::Battle)
        {
            _battle.Run(delta);
        }
    }
}

void Game::Draw(float delta)
{
    _menu.Draw(delta);
}
