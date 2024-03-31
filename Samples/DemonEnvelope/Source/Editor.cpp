#include "Editor.h"
#include "Network.h"
#include "imgui.h"
#include "Window.h"
#include <EventStream.h>
#include <memory>

using namespace DE;

void Editor::Start(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_)
{
    level = std::shared_ptr<Level>(new Level());
    network = std::shared_ptr<DE::Network>(new DE::Network());

    std::memset(username, 0, sizeof(username));
    std::memset(password, 0, sizeof(password));
    std::memset(levelName, 0, sizeof(levelName));
    std::memset(selectedLevelName, 0, sizeof(selectedLevelName));
    
    std::strcpy(hostname, "localhost");

    level->Setup(lib, tileset_);
}

void Editor::Run(float delta, ISLANDER_WINDOW window)
{
    network->Process();

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

void Editor::Draw(float delta, ISLANDER_DEVICE device, ISLANDER_WINDOW window, CRIMSON_HANDLE crimson, IslanderImguiContext* cxt)
{
    auto evStream = network->EventStream();
    if (evStream.get())
    {
        const int width = IslanderWindowWidth(window);
        const int height = IslanderWindowHeight(window);

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
                network->Login(username, password);
                std::memset(password, 0, sizeof(password));

                if (network->LogSub())
                {
                    network->LogSub()->Signal([this](Oxygen::Message& msg)
                        {
                            network->GetAssets(assets);
                            network->ListLevels(levels);
                        });
                }
            }
        }
    }
    ImGui::End();

    if (network->Connected())
    {
        if (ImGui::Begin("Assets"))
        {
            for (int i = 0; i < assets.size(); i++)
            {
                ImGui::Text(assets[i].c_str());
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

