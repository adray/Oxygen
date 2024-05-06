#include "Game.h"
#include "Tilemap.h"
#include "Level.h"
#include "Item.h"
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
    _level->RunScripts(delta);

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
    _level->StartScripts();
    _player = level->AddEntity();
    _moveElapsed = 0.0f;
}

void GameState_Map::Close()
{
    _level->ClearEntities();
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

constexpr int BUTTON_PARTY = 0;
constexpr int BUTTON_ITEM = 1;
constexpr int BUTTON_RESEVERED1 = 2;
constexpr int BUTTON_RESEVERED2 = 3;
constexpr int BUTTON_RESEVERED3 = 4;
constexpr int BUTTON_SAVE = 5;
constexpr int BUTTON_EXIT = 6;
constexpr int BUTTON_ITEM_BEGIN = 100;
constexpr int BUTTON_ITEM_END = 200;
constexpr int BUTTON_ITEM_CANCEL = 201;
constexpr int BUTTON_ITEM_USE = 202;
constexpr int BUTTON_ITEM_EQUIP = 203;
constexpr int BUTTON_ITEM_DISCARD = 204;

GameMenu::GameMenu()
    :
    _open(false),
    _selIndex(0),
    _selMax(0),
    _selMin(0),
    _charIndex(0),
    _contextIndex(0),
    _type(MenuType::Main),
    _contextMenu(false),
    _itemIndex(0)
{
}

void GameMenu::Start()
{
    _open = false;
}

void GameMenu::EnterMenu(MenuType type, Party& party)
{
    _contextMenuItems.clear();
    _contextMenu = false;

    _type = type;

    switch (type)
    {
    case MenuType::Main:
        _selMax = BUTTON_EXIT;
        _selMin = BUTTON_PARTY;
        break;
    case MenuType::Party:
    case MenuType::Equip:
    case MenuType::Use:
        _selMax = party.NumMembers() - 1;
        _selMin = 0;
        break;
    case MenuType::Items:
        _selMax = BUTTON_ITEM_BEGIN + party.Pack().NumItems() - 1;
        _selMin = BUTTON_ITEM_BEGIN;
        break;
    case MenuType::Character:
        _selMax = 0;
        _selMin = 0;
        break;
    }
    
    _selIndex = _selMin;
}

void GameMenu::BuildItemContextMenu(Party& party, std::shared_ptr<ItemConfig>& items)
{
    _contextMenu = true;
    _contextIndex = 0;
    _contextMenuItems.clear();

    auto& pack = party.Pack();
    auto& item = pack.At(_selIndex - _selMin);
    auto& config = items->Get(item.ID());

    if (config.CanConsume())
    {
        int target = config.GetIntegerAttribute("Target");
        int numTargets = config.GetIntegerAttribute("NumTargets");
        if (target == TARGET_FRIENDLY && numTargets == 1)
        {
            ContextMenuItem item;
            item._text = "Use";
            item._type = BUTTON_ITEM_USE;
            _contextMenuItems.push_back(item);
        }
    }

    if (config.CanEquip())
    {
        ContextMenuItem item;
        item._text = "Equip";
        item._type = BUTTON_ITEM_EQUIP;
        _contextMenuItems.push_back(item);
    }

    if (!config.IsImportant())
    {
        ContextMenuItem item;
        item._text = "Discard";
        item._type = BUTTON_ITEM_DISCARD;
        _contextMenuItems.push_back(item);
    }

    {
        ContextMenuItem item;
        item._text = "Cancel";
        item._type = BUTTON_ITEM_CANCEL;
        _contextMenuItems.push_back(item);
    }
}

void GameMenu::UnequipItem(Party& party, std::shared_ptr<ItemConfig>& items)
{
    const int index = _itemIndex - BUTTON_ITEM_BEGIN;
    auto& item = party.Pack().At(index);
    auto& itemCfg = items->Get(item.ID());

    int itemId = -1;
    auto& member = party.FindMemberByIndex(_selIndex);
    switch (itemCfg.Type())
    {
    case EquipmentType::Helm:
        itemId = member.Helm();
        member.SetHelm(-1);
        break;
    case EquipmentType::Footwear:
        itemId = member.Footwear();
        member.SetFootwear(-1);
        break;
    case EquipmentType::Chest:
        itemId = member.Chest();
        member.SetChest(-1);
        break;
    case EquipmentType::Legs:
        itemId = member.Legs();
        member.SetLegs(-1);
        break;
    case EquipmentType::Gloves:
        itemId = member.Gloves();
        member.SetGloves(-1);
        break;
    case EquipmentType::Weapon:
        itemId = member.PrimaryWeapon();
        member.SetPrimaryWeapon(-1);
        break;
    }

    if (itemId > -1)
    {
        party.Pack().For(itemId).Unequip(1);
    }
}

void GameMenu::EquipItem(Party& party, std::shared_ptr<ItemConfig>& items)
{
    UnequipItem(party, items);

    const int index = _itemIndex - BUTTON_ITEM_BEGIN;
    auto& item = party.Pack().At(index);
    auto& itemCfg = items->Get(item.ID());
    
    if (item.SetEquipped(1))
    {
        auto& member = party.FindMemberByIndex(_selIndex);
        switch (itemCfg.Type())
        {
        case EquipmentType::Helm:
            member.SetHelm(item.ID());
            break;
        case EquipmentType::Chest:
            member.SetChest(item.ID());
            break;
        case EquipmentType::Footwear:
            member.SetFootwear(item.ID());
            break;
        case EquipmentType::Gloves:
            member.SetGloves(item.ID());
            break;
        case EquipmentType::Legs:
            member.SetLegs(item.ID());
            break;
        case EquipmentType::Weapon:
            member.SetPrimaryWeapon(item.ID());
            break;
        }
    }
}

void GameMenu::UseItem(Party& party, std::shared_ptr<ItemConfig>& items)
{
    const int index = _itemIndex - BUTTON_ITEM_BEGIN;
    auto& item = party.Pack().At(index);
    auto& itemCfg = items->Get(item.ID());

    int heal = itemCfg.GetIntegerAttribute("Heal");
    if (heal > 0)
    {
        auto& mem = party.FindMemberByIndex(_selIndex);
        mem.SetHP(mem.HP() + heal);
        item.Decrement();
    }
}

void GameMenu::DiscardItem(Party& party)
{
    auto& pack = party.Pack();
    auto& item = pack.At(_selIndex - BUTTON_ITEM_BEGIN);
    pack.RemoveItem(item.ID(), item.NumItems());
}

void GameMenu::Run(float delta, ISLANDER_WINDOW window, Party& party, std::shared_ptr<ItemConfig>& items)
{
    if (!_open)
    {
        if (IslanderIsKeyDown(window, Islander::KEY_ENTER))
        {
            _open = true;
            EnterMenu(MenuType::Main, party);
            IslanderSetKeyUp(window, Islander::KEY_ENTER);
        }
    }
    else
    {
        if (IslanderIsKeyDown(window, Islander::KEY_UP))
        {
            if (_contextMenu)
            {
                _contextIndex = std::max(0, _contextIndex - 1);
            }
            else
            {
                _selIndex = std::max(_selMin, _selIndex - 1);
            }
            IslanderSetKeyUp(window, Islander::KEY_UP);
        }

        if (IslanderIsKeyDown(window, Islander::KEY_DOWN))
        {
            if (_contextMenu)
            {
                _contextIndex = std::min((int)_contextMenuItems.size() - 1, _contextIndex + 1);
            }
            else
            {
                _selIndex = std::min(_selMax, _selIndex + 1);
            }
            IslanderSetKeyUp(window, Islander::KEY_DOWN);
        }

        if (IslanderIsKeyDown(window, Islander::KEY_ENTER))
        {
            switch (_type)
            {
            case MenuType::Main:
                switch (_selIndex)
                {
                case BUTTON_PARTY:
                    EnterMenu(MenuType::Party, party);
                    break;
                case BUTTON_ITEM:
                    EnterMenu(MenuType::Items, party);
                    break;
                case BUTTON_EXIT:
                    _open = false;
                    break;
                }
                break;
            case MenuType::Party:
                _charIndex = _selIndex;
                party.FindMemberByIndex(_charIndex).CalculateStats(party.Pack(), items);
                EnterMenu(MenuType::Character, party);
                break;
            case MenuType::Equip:
                EquipItem(party, items);
                EnterMenu(MenuType::Items, party);
                break;
            case MenuType::Use:
                UseItem(party, items);
                EnterMenu(MenuType::Items, party);
                break;
            case MenuType::Items:
                if (_contextMenu)
                {
                    auto& item = _contextMenuItems[_contextIndex];
                    if (item._type == BUTTON_ITEM_EQUIP)
                    {
                        _itemIndex = _selIndex;
                        EnterMenu(MenuType::Equip, party);
                    }
                    else if (item._type == BUTTON_ITEM_USE)
                    {
                        _itemIndex = _selIndex;
                        EnterMenu(MenuType::Use, party);
                    }
                    else if (item._type == BUTTON_ITEM_CANCEL)
                    {
                        _contextMenu = false;
                    }
                    else if (item._type == BUTTON_ITEM_DISCARD)
                    {
                        DiscardItem(party);
                        _selMax = party.Pack().NumItems() + BUTTON_ITEM_BEGIN - 1;
                        _contextMenu = false;
                    }
                }
                else
                {
                    BuildItemContextMenu(party, items);
                }
                break;
            }

            IslanderSetKeyUp(window, Islander::KEY_ENTER);
        }

        if (IslanderIsKeyDown(window, Islander::KEY_ESCAPE))
        {
            switch (_type)
            {
            case MenuType::Main:
                _open = false;
                break;
            case MenuType::Party:
            case MenuType::Items:
                EnterMenu(MenuType::Main, party);
                break;
            case MenuType::Character:
                EnterMenu(MenuType::Party, party);
                break;
            case MenuType::Equip:
                EnterMenu(MenuType::Items, party);
                break;
            }

            IslanderSetKeyUp(window, Islander::KEY_ESCAPE);
        }
    }
}

int GameMenu::GetFlags(int pos)
{
    return CRIMSON_BUTTON_FLAGS_NONE | (_selIndex == pos ? CRIMSON_BUTTON_FLAGS_SELECTED : CRIMSON_BUTTON_FLAGS_NONE);
}

void GameMenu::Draw(float delta, ISLANDER_WINDOW window, CRIMSON_HANDLE crimson, Party& party, std::shared_ptr<ItemConfig>& items)
{
    if (_open)
    {
        if (_type == MenuType::Main)
        {
            float colour[4] = { 0.6f, 0.6f, 0.6f, 1.0f };
            float border[4] = { 0.0f,0.0f,0.0f, 1.0f };

            CrimsonSetPos(crimson, 0.52f, 0.55f);
            CrimsonFilledRect(crimson, 0.12f, 0.35f, colour, 1.0f, border);

            CrimsonSetPos(crimson, 0.52f, 0.55f);
            CrimsonButton(crimson, BUTTON_PARTY, "Party", 0.12f, 0.05f, GetFlags(0));

            CrimsonSetPos(crimson, 0.52f, 0.60f);
            CrimsonButton(crimson, BUTTON_ITEM, "Items", 0.12f, 0.05f, GetFlags(1));

            CrimsonSetPos(crimson, 0.52f, 0.65f);
            CrimsonButton(crimson, BUTTON_RESEVERED1, "---", 0.12f, 0.05f, GetFlags(2));

            CrimsonSetPos(crimson, 0.52f, 0.70f);
            CrimsonButton(crimson, BUTTON_RESEVERED2, "---", 0.12f, 0.05f, GetFlags(3));

            CrimsonSetPos(crimson, 0.52f, 0.75f);
            CrimsonButton(crimson, BUTTON_RESEVERED3, "---", 0.12f, 0.05f, GetFlags(4));

            CrimsonSetPos(crimson, 0.52f, 0.80f);
            CrimsonButton(crimson, BUTTON_SAVE, "Save", 0.12f, 0.05f, GetFlags(5));

            CrimsonSetPos(crimson, 0.52f, 0.85f);
            CrimsonButton(crimson, BUTTON_EXIT, "Exit", 0.12f, 0.05f, GetFlags(6));
        }
        else if (_type == MenuType::Party ||
            _type == MenuType::Equip ||
            _type == MenuType::Use)
        {
            float colour[4] = { 0.6f, 0.6f, 0.6f, 1.0f };
            float border[4] = { 0.0f,0.0f,0.0f, 1.0f };
            float selColour[4] = { 0.0f, 0.0f, 0.0f, 1.0f };

            int buttonIndex = 0;
            for (int i = 0; i < party.NumMembers(); i++)
            {
                constexpr float gap = 0.08f;
                auto& member = party.FindMemberByIndex(i);

                if (member.Active())
                {
                    CrimsonSetPos(crimson, 0.52f, 0.52f + i * gap);
                    CrimsonFilledRect(crimson, 0.4f, 0.07f, colour, 1.0f, border);

                    CrimsonSetPos(crimson, 0.52f, 0.52f + i * gap);
                    CrimsonText(crimson, member.Name().c_str(), 0.1f, 0.07f, 0, CRIMSON_TEXT_FLAGS_NONE);

                    std::stringstream ss; ss << "Lv." << member.Level();

                    CrimsonSetPos(crimson, 0.65f, 0.52f + i * gap);
                    CrimsonText(crimson, ss.str().c_str(), 0.05f, 0.07f, 0, CRIMSON_TEXT_FLAGS_NONE);

                    ss.str(""); ss << "HP." << member.HP() << "/" << member.GetStats().MaxHP();

                    CrimsonSetPos(crimson, 0.7f, 0.52f + i * gap);
                    CrimsonText(crimson, ss.str().c_str(), 0.05f, 0.07f, 0, CRIMSON_TEXT_FLAGS_NONE);

                    ss.str(""); ss << "XP." << member.XP();

                    CrimsonSetPos(crimson, 0.8f, 0.52f + i * gap);
                    CrimsonText(crimson, ss.str().c_str(), 0.1f, 0.07f, 0, CRIMSON_TEXT_FLAGS_NONE);

                    if (_selIndex == i)
                    {
                        CrimsonSetPos(crimson, 0.90f, 0.52f + i * gap);
                        CrimsonFilledRect(crimson, 0.02f, 0.07f, selColour, 1.0f, border);
                    }
                }
            }

            CrimsonSetPos(crimson, 0.52f, 0.88f);
            CrimsonFilledRect(crimson, 0.4f, 0.07f, colour, 1.0f, border);

            CrimsonSetPos(crimson, 0.53f, 0.89f);
            CrimsonText(crimson, "Back - Escape", 0.1f, 0.05f, 0, CRIMSON_TEXT_FLAGS_NONE);
        }
        else if (_type == MenuType::Items)
        {
            float colour[4] = { 0.6f, 0.6f, 0.6f, 1.0f };
            float border[4] = { 0.0f,0.0f,0.0f, 1.0f };
            float selColour[4] = { 0.0f, 0.0f, 0.0f, 1.0f };

            auto& pack = party.Pack();

            const int maxItemsShown = 7;
            int startIndex = std::max(0, _selIndex - _selMin - maxItemsShown + 1);
            int numItems = std::min(maxItemsShown, pack.NumItems() - startIndex);

            for (int i = 0; i < numItems; i++)
            {
                constexpr float gap = 0.05f;
                auto& item = pack.At(i + startIndex);

                CrimsonSetPos(crimson, 0.52f, 0.52f + i * gap);
                CrimsonFilledRect(crimson, 0.4f, 0.04f, colour, 1.0f, border);

                auto& config = items->Get(item.ID());

                std::stringstream ss; ss << config.Name();

                CrimsonSetPos(crimson, 0.52f, 0.52f + i * gap);
                CrimsonText(crimson, ss.str().c_str(), 0.1f, 0.04f, 0, CRIMSON_TEXT_FLAGS_NONE);
                
                ss.str(""); ss << "x" << item.NumItems();

                CrimsonSetPos(crimson, 0.82f, 0.52f + i * gap);
                CrimsonText(crimson, ss.str().c_str(), 0.1f, 0.04f, 0, CRIMSON_TEXT_FLAGS_NONE);

                if (_selIndex == i + _selMin + startIndex)
                {
                    CrimsonSetPos(crimson, 0.90f, 0.52f + i * gap);
                    CrimsonFilledRect(crimson, 0.02f, 0.04f, selColour, 1.0f, border);
                }
            }

            CrimsonSetPos(crimson, 0.52f, 0.88f);
            CrimsonFilledRect(crimson, 0.4f, 0.07f, colour, 1.0f, border);

            if (pack.NumItems() > 0)
            {
                auto& item = pack.At(_selIndex - _selMin);
                auto& config = items->Get(item.ID());

                CrimsonSetPos(crimson, 0.53f, 0.89f);
                CrimsonText(crimson, config.Description().c_str(), 0.1f, 0.05f, 0, CRIMSON_TEXT_FLAGS_NONE);
            }
        }
        else if (_type == MenuType::Character)
        {
            float colour[4] = { 0.6f, 0.6f, 0.6f, 1.0f };
            float border[4] = { 0.0f,0.0f,0.0f, 1.0f };
            float selColour[4] = { 0.0f, 0.0f, 0.0f, 1.0f };

            auto& member = party.FindMemberByIndex(_charIndex);

            CrimsonSetPos(crimson, 0.52f, 0.52f);
            CrimsonFilledRect(crimson, 0.4f, 0.07f, colour, 1.0f, border);

            CrimsonSetPos(crimson, 0.53f, 0.52f);
            CrimsonText(crimson, member.Name().c_str(), 0.1f, 0.05f, 0, CRIMSON_TEXT_FLAGS_NONE);

            const int numTypes = 7;
            std::string equipmentName[numTypes] = {
                "Head",
                "Chest",
                "Legs",
                "Gloves",
                "Footwear",
                "Weapon 1",
                "Weapon 2"
            };
            int equipmentValue[numTypes] = {
                member.Helm(),
                member.Chest(),
                member.Legs(),
                member.Gloves(),
                member.Footwear(),
                member.PrimaryWeapon(),
                member.SecondaryWeapon()
            };

            CrimsonSetPos(crimson, 0.53f, 0.6f);
            CrimsonFilledRect(crimson, 0.25f, 0.04f * numTypes, colour, 1.0f, border);

            for (int i = 0; i < numTypes; i++)
            {
                float posX = 0.53f;
                float posY = 0.6f + i * 0.04f;

                CrimsonSetPos(crimson, posX, posY);
                CrimsonText(crimson, equipmentName[i].c_str(), 0.1f, 0.08f, 0, CRIMSON_TEXT_FLAGS_NONE);

                CrimsonSetPos(crimson, posX + 0.08f, posY);
                std::string name;

                if (equipmentValue[i] == -1)
                {
                    name = "----";
                }
                else
                {
                    auto& cfg = items->Get(equipmentValue[i]);
                    name = cfg.Name();
                }

                CrimsonText(crimson, name.c_str(), 0.1f, 0.05f, 0, CRIMSON_TEXT_FLAGS_NONE);
            }

            const auto& stats = member.GetStats();
            const int numStats = 4;
            const std::string statNames[numStats] = {
                "HP",
                "DP",
                "Attack",
                "Defence"
            };
            int statsValues[numStats] = {
                stats.MaxHP(),
                stats.MaxDP(),
                stats.Attack(),
                stats.Defence()
            };

            CrimsonSetPos(crimson, 0.8f, 0.6f);
            CrimsonFilledRect(crimson, 0.15f, 0.04f * numStats, colour, 1.0f, border);

            for (int i = 0; i < numStats; i++)
            {
                float posX = 0.8f;
                float posY = 0.6f + i * 0.04f;

                CrimsonSetPos(crimson, posX, posY);
                CrimsonText(crimson, statNames[i].c_str(), 0.1f, 0.08f, 0, CRIMSON_TEXT_FLAGS_NONE);

                std::stringstream ss; ss << statsValues[i];

                CrimsonSetPos(crimson, posX + 0.08f, posY);
                CrimsonText(crimson, ss.str().c_str(), 0.1f, 0.05f, 0, CRIMSON_TEXT_FLAGS_NONE);
            }
        }

        if (_contextMenu)
        {
            float colour[4] = { 0.6f, 0.6f, 0.6f, 1.0f };
            float border[4] = { 0.0f,0.0f,0.0f, 1.0f };
            float selColour[4] = { 0.0f, 0.0f, 0.0f, 1.0f };

            const size_t numItems = _contextMenuItems.size();
            const float width = 0.1f;
            const float height = 0.04f;
            const float posY = 0.95f - height * numItems;
            const float posX = 0.80f;

            CrimsonSetPos(crimson, posX, posY);
            CrimsonFilledRect(crimson, width, height * numItems, colour, 1.0f, border);

            for (size_t i = 0; i < _contextMenuItems.size(); i++)
            {
                CrimsonSetPos(crimson, posX, posY + height * i);
                CrimsonText(crimson, _contextMenuItems[i]._text.c_str(), width, height, 0, CRIMSON_TEXT_FLAGS_NONE);

                if (_contextIndex == i)
                {
                    CrimsonSetPos(crimson, posX + width - 0.02f, posY + height * i);
                    CrimsonFilledRect(crimson, 0.02f, height, selColour, 1.0f, border);
                }
            }
        }
    }
}

//==================
// Game class
//==================

void Game::Initialize(std::shared_ptr<Tileset>& tileset, std::shared_ptr<ItemConfig>& items)
{
    _map.Initialize(tileset);
    _items = items;
}

void Game::Start(std::shared_ptr<Level>& level, int px, int py)
{
    _running = true;
    _menu.Start();
    _map.SetLevel(level);
    _level = level;
    level->SetEntityPos(_map.Player(), px, py);
}

void Game::Stop()
{
    _running = false;
    _map.Close();
    _level.reset();
}

void Game::Run(float delta, ISLANDER_WINDOW window)
{
    if (_running)
    {
        if (_state == State::Map)
        {
            auto& dialogue = _level->GetDialogue();
            if (dialogue.IsShowing())
            {
                if (IslanderIsKeyDown(window, Islander::KEY_ENTER)) { dialogue.Hide(); IslanderSetKeyUp(window, Islander::KEY_ENTER); }
            }

            _menu.Run(delta, window, _party, _items);
            _map.Run(delta, window);
        }
        else if (_state == State::Battle)
        {
            _battle.Run(delta);
        }
    }
}

void Game::DrawDialogue(CRIMSON_HANDLE crimson)
{
    auto& dialogue = _level->GetDialogue();
    if (dialogue.IsShowing())
    {
        float colour[4] = { .7f, .7f, .7f, 1.0f };
        float border[4] = { 0.0f, 0.0f, 0.0f, 1.0f };

        CrimsonSetPos(crimson, 0.53f, 0.8f);
        CrimsonFilledRect(crimson, 0.42f, 0.1f, colour, 1.0f, border);

        CrimsonSamePos(crimson);
        CrimsonText(crimson, dialogue.DialogueText().c_str(), 0.42f, 0.1f, 0, CRIMSON_TEXT_FLAGS_WRAP);

        if (dialogue.HasName())
        {
            CrimsonSetPos(crimson, 0.55f, 0.75f);
            CrimsonFilledRect(crimson, 0.1f, 0.05f, colour, 1.0f, border);

            CrimsonSetPos(crimson, 0.56f, 0.75f);
            CrimsonText(crimson, dialogue.Name().c_str(), 0.08f, 0.05f, 0, CRIMSON_TEXT_FLAGS_NONE);
        }
    }
}

void Game::Draw(float delta, ISLANDER_WINDOW window, CRIMSON_HANDLE crimson)
{
    if (_running)
    {
        _menu.Draw(delta, window, crimson, _party, _items);
        DrawDialogue(crimson);
    }
}
