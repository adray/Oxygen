#include "Editor.h"
#include "ScriptingEditor.h"
#include "Network.h"
#include "imgui.h"
#include "Window.h"
#include "FileSystem.h"
#include <EventStream.h>
#include <memory>

using namespace DE;

void Editor::Start(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_, Game* game, const std::string& assetDir)
{
    level = std::shared_ptr<Level>(new Level());
    network = std::shared_ptr<DE::Network>(new DE::Network());
    _assetDir = assetDir;
    _game = game;

    ScanAssetDir();

    std::memset(username, 0, sizeof(username));
    std::memset(password, 0, sizeof(password));
    std::memset(levelName, 0, sizeof(levelName));
    std::memset(selectedLevelName, 0, sizeof(selectedLevelName));
    
    std::strcpy(hostname, "localhost");

    level->Setup(lib, tileset_);
}

void Editor::ScanAssetDir()
{
    assets.clear();

    Asset asset = {};
    asset.onDisk = true;

    void* handle = Islander::FileSystem::GetFirstFile(_assetDir, asset.name);
    
    assets.push_back(asset);

    while (Islander::FileSystem::GetNextFile(handle, asset.name))
    {
        assets.push_back(asset);
    }
}

void Editor::Run(float delta, ISLANDER_WINDOW window)
{
    if (editMode)
    {
        _Run(delta, window);
    }
}

void Editor::_Run(float delta, ISLANDER_WINDOW window)
{
    network->Process();

    _scripting.RunScripts(delta);

    const int posX = IslanderMouseX(window);
    const int posY = IslanderMouseY(window);

    const int width = IslanderWindowWidth(window);
    const int height = IslanderWindowHeight(window);

    if (network->State() == Network_State::JoinedLevel)
    {
        const int tilemap = level->TileMapHitTest(posX - width / 2, posY - height / 2);
        if (tilemap >= 0)
        {
            auto& map = level->GetTilemap();
            const int cell = map.HitTest(posX - width / 2, posY - height / 2);

            if (cell != cursorTile)
            {
                cursorTile = cell;
                update = true;
            }
        }

        cursorTime += delta;
        if (cursorTime >= 0.1f)
        {
            cursorTime = 0.0f;

            if (update)
            {
                auto& map = level->GetTilemap();
                if (map.NumLayers() > 0)
                {
                    network->UpdateCursor(map.GetLayer(0).ID(), cursorTile);
                }
                update = false;
            }
        }

        bool click = IslanderGetLeftMouseState(window) == Islander::MOUSE_INPUT_UP && left_down;
        left_down = IslanderGetLeftMouseState(window) == Islander::MOUSE_INPUT_DOWN;
        if (click)
        {
            left_down = false;

            if (palette > -1)
            {
                auto& map = level->GetTilemap();
                const int cell = map.HitTest(posX - width / 2, posY - height / 2);
                if (cell >= 0 && map.NumLayers() > 0)
                {
                    map.Set(palette_layer, cell, palette);

                    network->UpdateTilemap(map.GetLayer(palette_layer));
                    network->UpdateTilemask(map.GetCollisionMask());
                }
            }
        }

        if (IslanderIsKeyDown(window, Islander::KEY_RIGHT))
        {
            auto& map = level->GetTilemap();
            map.SetScrollPos(map.ScrollX() + 5, map.ScrollY());
            IslanderSetKeyUp(window, Islander::KEY_RIGHT);
        }
        else if (IslanderIsKeyDown(window, Islander::KEY_LEFT))
        {
            auto& map = level->GetTilemap();
            map.SetScrollPos(map.ScrollX() - 5, map.ScrollY());
            IslanderSetKeyUp(window, Islander::KEY_LEFT);
        }
        else if (IslanderIsKeyDown(window, Islander::KEY_UP))
        {
            auto& map = level->GetTilemap();
            map.SetScrollPos(map.ScrollX(), map.ScrollY() - 5);
            IslanderSetKeyUp(window, Islander::KEY_UP);
        }
        else if (IslanderIsKeyDown(window, Islander::KEY_DOWN))
        {
            auto& map = level->GetTilemap();
            map.SetScrollPos(map.ScrollX(), map.ScrollY() + 5);
            IslanderSetKeyUp(window, Islander::KEY_DOWN);
        }
    }
}

void Editor::DrawScriptNode(ScriptObject& sc)
{
    bool update = false;

    ImGui::PushID(sc.ID());
    if (ImGui::TreeNode("Script"))
    {
        ImGui::Text("ID %i", sc.ID());
        
        int x = sc.X();
        int y = sc.Y();
        if (ImGui::InputInt("X", &x)) { sc.SetX(x); update = true; }
        if (ImGui::InputInt("Y", &y)) { sc.SetY(y); update = true; }

        static std::string type[3] = {
            "None",
            "OnCreate",
            "OnTouch"
        };

        if (ImGui::BeginCombo("Trigger", type[(int)sc.Trigger()].c_str()))
        {
            for (int i = 0; i < 3; i++)
            {
                bool selected = i == (int)sc.Trigger();
                if (ImGui::Selectable(type[i].c_str(), &selected))
                {
                    sc.SetTrigger((DE::ScriptTrigger)i);
                    update = true;
                }
            }

            ImGui::EndCombo();
        }

        char buffer[256];
        std::memcpy(buffer, sc.ScriptName().c_str(), sizeof(buffer));

        if (ImGui::InputText("File", buffer, sizeof(buffer)))
        {
            sc.SetScriptName(buffer);
            update = true;
        }
        ImGui::TreePop();
    }
    ImGui::PopID();

    if (update)
    {
        network->UpdateScript(sc);
    }
}

void Editor::Draw(float delta, ISLANDER_DEVICE device, ISLANDER_WINDOW window, CRIMSON_HANDLE crimson, IslanderImguiContext* cxt)
{
    if (network->Connected())
    {
        if (ImGui::Begin("Game"))
        {
            if (editMode && ImGui::Button("Play"))
            {
                editMode = false;
                _game->Start(level, 0, 0);
            }
            else if (!editMode && ImGui::Button("Stop"))
            {
                editMode = true;
                _game->Stop();
            }
        }
        ImGui::End();
    }

    if (_game->IsRunning())
    {
        if (ImGui::Begin("Debug"))
        {
            auto& party = _game->GetParty();
            for (int i = 0; i < party.NumMembers(); i++)
            {
                auto& member = party.FindMemberByIndex(i);
                int level = member.Level();
                ImGui::InputInt(member.Name().c_str(), &level);
                member.SetLevel(level);
            }

            auto& pack = party.Pack();
            for (int i = 0; i < pack.NumItems(); i++)
            {
                auto& item = pack.At(i);
                ImGui::Text("ID:%i x%i Equipped:%i", item.ID(), item.NumItems(), item.NumEquipped());
            }
        }
        ImGui::End();
    }

    if (!editMode)
    {
        DrawScriptLog(level->ScriptSystem());
        return;
    }

    const int width = IslanderWindowWidth(window);
    const int height = IslanderWindowHeight(window);

    auto evStream = network->EventStream();
    if (evStream.get())
    {
        for (const Oxygen::EventStream::User& user : evStream->Users())
        {
            auto& map = level->GetTilemap();
            const int numlayers = map.NumLayers();
            for (int i = 0; i < numlayers; i++)
            {
                if (user.objectId == map.GetLayer(i).ID())
                {
                    float px, py, sx, sy;
                    if (map.GetTileBounds(user.subId, &px, &py, &sx, &sy))
                    {
                        px /= width;
                        py /= height;
                        sx /= width;
                        sy /= height;

                        CrimsonSetPos(crimson, px + 0.5f, py + 0.5f);

                        float colour[4] = { 1.0f, 0.0f, 0.0f, 0.5f };
                        float border[4] = { 1.0f, 1.0f, 1.0f, 1.0f };
                        CrimsonFilledRect(crimson, sx, sy, colour, 1.0f, border);
                    }
                }
            }
        }
    }

    if (showCollider && level->GetTilemap().NumLayers() > 0)
    {
        auto& map = level->GetTilemap();
        auto& mask = map.GetCollisionMask();
        for (int i = 0; i < map.ViewWidth(); i++)
        {
            for (int j = 0; j < map.ViewHeight(); j++)
            {
                const int cell = (i + map.ScrollX()) + (j + map.ScrollY()) * map.Width();

                float px, py, sx, sy;
                if (map.GetTileBounds(cell, &px, &py, &sx, &sy) && mask.Get(cell))
                {
                    px /= width;
                    py /= height;
                    sx /= width;
                    sy /= height;

                    CrimsonSetPos(crimson, px + 0.5f, py + 0.5f);

                    float colour[4] = { 0.0f, 0.0f, 1.0f, 0.5f };
                    float border[4] = { 1.0f, 1.0f, 1.0f, 1.0f };
                    CrimsonFilledRect(crimson, sx, sy, colour, 1.0f, border);
                }
            }
        }
    }

    DrawScriptingEditor(_scripting, _scriptBuilder);

    if (ImGui::Begin("Options"))
    {
        if (ImGui::Button("Bordered mode"))
        {
            IslanderSetWindowStyle(window, ISLANDER_WINDOW_STYLE_BORDER);
        }

        if (ImGui::Button("Borderless mode"))
        {
            IslanderSetWindowStyle(window, ISLANDER_WINDOW_STYLE_BORDERLESS);
        }
    }
    ImGui::End();

    if (ImGui::Begin("Network"))
    {
        if (network->State() != Network_State::Disconnected)
        {
            ImGui::Text("Logged In");
        }
        else
        {
            ImGui::InputText("Hostname", hostname, sizeof(hostname));
            ImGui::InputText("Username", username, sizeof(username));
            ImGui::InputText("Password", password, sizeof(password), ImGuiInputTextFlags_Password);

            if (ImGui::Button("Login"))
            {
                network->Connect(hostname);
                network->StartAssetService(_assetDir);
                network->Login(username, password, assets, levels);
                std::memset(password, 0, sizeof(password));
            }
        }
    }
    ImGui::End();

    if (network->Connected())
    {
        if (ImGui::Begin("Assets"))
        {
            if (ImGui::Button("Refresh"))
            {
                ScanAssetDir();
                network->GetAssets(assets);
            }
            ImGui::SameLine();
            if (ImGui::Button("Bake"))
            {
                // todo
                Islander::FileSystem::RunProcess("", "");
            }

            for (int i = 0; i < assets.size(); i++)
            {
                const Asset& asset = assets[i];
                const std::string name = asset.name;

                ImGui::PushID(name.c_str());
                ImGui::Text(name.c_str());
                if (asset.onServer)
                {
                    ImGui::SameLine();
                    if (ImGui::Button("Download"))
                    {
                        network->DownloadAsset(name);
                    }
                }
                if (asset.onDisk)
                {
                    ImGui::SameLine();
                    if (ImGui::Button("Upload"))
                    {
                        network->UploadAsset(name);
                    }
                }
                ImGui::PopID();
            }
        }
        ImGui::End();

        if (ImGui::Begin("Levels"))
        {
            ImGui::InputText("Level Name", levelName, sizeof(levelName));
            if (ImGui::Button("New Level"))
            {
                network->CreateLevel(levelName, level);

                levels.clear();
                network->ListLevels(levels);
            }

            if (ImGui::BeginListBox("Levels"))
            {
                for (int i = 0; i < levels.size(); i++)
                {
                    const char* name = levels[i].c_str();
                    bool selected = strcmp(name, selectedLevelName) == 0;
                    if (ImGui::Selectable(name, &selected))
                    {
                        memcpy(selectedLevelName, name, strlen(name)+1);
                    }
                }
                ImGui::EndListBox();
            }

            if (strlen(selectedLevelName) > 0)
            {
                ImGui::Text(selectedLevelName);
                ImGui::SameLine();
                if (ImGui::Button("Join"))
                {
                    network->CloseLevel();
                    level->Reset();
                    std::memset(levelName, 0, sizeof(levelName));

                    if (network->CloseSub())
                    {
                        network->CloseSub()->Signal([this](Oxygen::Message& msg)
                            {
                                network->JoinLevel(selectedLevelName, level);
                            });
                    }
                    else
                    {
                        network->JoinLevel(selectedLevelName, level);
                    }
                }
            }
        }
        ImGui::End();

        if (ImGui::Begin("Scene"))
        {
            auto& tilemap = level->GetTilemap();
            if (tilemap.NumLayers() > 0)
            {
                if (ImGui::TreeNode("Tilemap"))
                {
                    if (ImGui::BeginPopupContextItem())
                    {
                        if (ImGui::Selectable("New script"))
                        {
                            network->CreateScript(tilemap.ID(), 0, 0);
                        }

                        ImGui::EndPopup();
                    }

                    for (int i = 0; i < tilemap.NumLayers(); i++)
                    {
                        ImGui::Text("Layer %i", i);
                    }

                    for (auto& sc : level->Scripts())
                    {
                        if (sc.ParentID() == tilemap.ID())
                        {
                            DrawScriptNode(sc);
                        }
                    }

                    ImGui::TreePop();
                }
            }

            // Draw any scripts which are unattached (however that happened?)
            for (auto& sc : level->Scripts())
            {
                if (sc.ParentID() == -1)
                {
                    DrawScriptNode(sc);
                }
            }
        }
        ImGui::End();

        if (ImGui::Begin("Tiles"))
        {
            auto& tilemap = level->GetTilemap();
            if (tilemap.NumLayers() > 0)
            {
                ImGui::Text("Tilemap pos [%i,%i]", tilemap.ScrollX(), tilemap.ScrollY());

                for (int i = 0; i < tilemap.NumLayers(); i++)
                {
                    ImGui::PushID(i);
                    Tilemap_Layer& layer = tilemap.GetLayer(i);
                    ImGui::Text("Layer %i", layer.Layer());
                    ImGui::SameLine();

                    bool visible = layer.Visible();
                    if (ImGui::Checkbox("Visible", &visible))
                    {
                        layer.SetVisible(visible);
                    }
                    ImGui::PopID();
                }

                ImGui::Checkbox("Show Collider", &showCollider);

                std::shared_ptr<Tileset> tileset = level->TileSet();
                for (int i = 0; i < tileset->NumTiles(); i++)
                {
                    Tileset_Tile tile;
                    tileset->GetTile(i, tile);

                    if (IslanderImguiImageButton(device, cxt, i, tile._texture.index, tile._texture.px, tile._texture.py, tile._texture.sx, tile._texture.sy))
                    {
                        palette = i;
                        palette_layer = tile._layer;
                    }
                    ImGui::SameLine();
                }
            }
            else
            {
                static int width = 100;
                static int height = 100;
                ImGui::InputInt("Width", &width);
                ImGui::InputInt("Height", &height);

                if (ImGui::Button("Create TileMap"))
                {
                    network->CreateTilemap(width, height, 2);
                }
            }
        }
        ImGui::End();
    }
}

