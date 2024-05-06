#pragma once
#include <memory>
#include <API.h>
#include "Party.h"

namespace DE
{
    class Tileset;
    class Tilemap;
    class Level;
    class Level_Entity;
    class ItemConfig;
    class Dialogue;

    class GameState_Map
    {
    public:
        void Initialize(std::shared_ptr<Tileset>& tileset);
        void SetLevel(std::shared_ptr<Level>& level);
        void Run(float delta, ISLANDER_WINDOW window);
        void Close();

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
        GameMenu();
        void Start();
        void Run(float delta, ISLANDER_WINDOW window, Party& party, std::shared_ptr<ItemConfig>& items);
        void Draw(float delta, ISLANDER_WINDOW window, CRIMSON_HANDLE crimson, Party& party, std::shared_ptr<ItemConfig>& items);
    private:
        enum class MenuType
        {
            Main = 0,
            Party = 1,
            Items = 2,
            Character = 3,
            Equip = 4,
            Use = 5
        };

        struct ContextMenuItem
        {
            int _type;
            std::string _text;
        };

        void BuildItemContextMenu(Party& party, std::shared_ptr<ItemConfig>& items);

        void DiscardItem(Party& party);
        void UnequipItem(Party& party, std::shared_ptr<ItemConfig>& items);
        void EquipItem(Party& party, std::shared_ptr<ItemConfig>& items);
        void UseItem(Party& party, std::shared_ptr<ItemConfig>& items);
        void EnterMenu(MenuType type, Party& party);
        int GetFlags(int pos);

        bool _open;
        int _selIndex;
        int _selMax;
        int _selMin;
        int _charIndex;
        int _itemIndex;
        bool _contextMenu;
        int _contextIndex;
        std::vector< ContextMenuItem> _contextMenuItems;
        MenuType _type;
    };

    class Game
    {
    public:
        void Initialize(std::shared_ptr<Tileset>& tileset, std::shared_ptr<ItemConfig>& items);
        void Start(std::shared_ptr<Level>& level, int px, int py);
        void Stop();
        void Run(float delta, ISLANDER_WINDOW window);
        void Draw(float detla, ISLANDER_WINDOW window, CRIMSON_HANDLE crimson);
        bool IsRunning() const { return _running; }
        Party& GetParty() { return _party; }

    private:
        void DrawDialogue(CRIMSON_HANDLE crimson);

        enum class State
        {
            Map,
            Battle
        };

        std::shared_ptr<Level> _level;
        State _state;
        GameState_Map _map;
        GameState_Battle _battle;
        GameMenu _menu;
        Party _party;
        std::shared_ptr<ItemConfig> _items;
        bool _running;
    };
}
