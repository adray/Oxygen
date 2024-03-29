#include "Editor.h"
#include "Network.h"
#include "imgui.h"
#include "Window.h"
#include <EventStream.h>
#include <memory>

using namespace DE;

void Editor::Start(ISLANDER_POLYGON_LIBRARY lib, ISLANDER_DEVICE device)
{
    level = std::shared_ptr<Level>(new Level());
    network = std::shared_ptr<DE::Network>(new DE::Network());

    std::memset(username, 0, sizeof(username));
    std::memset(password, 0, sizeof(password));
    std::memset(levelName, 0, sizeof(levelName));
    std::memset(selectedLevelName, 0, sizeof(selectedLevelName));
    
    std::strcpy(hostname, "localhost");

    level->Setup(lib, device);
}

void Editor::Run(float delta, ISLANDER_WINDOW window)
{
    network->Process();

    const int posX = IslanderMouseX(window);
    const int posY = IslanderMouseY(window);

    const int width = IslanderWindowWidth(window);
    const int height = IslanderWindowHeight(window);

    if (network->LoggedIn())
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
                network->UpdateCursor(map.ID(), cursorTile);
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
                if (cell >= 0)
                {
                    map.Set(cell, palette);

                    network->UpdateTilemap(map);
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
            if (user.objectId == map.ID() && map.ID() >= 0)
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
        if (network->LoggedIn())
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

                network->GetAssets(assets);
                network->ListLevels(levels);
            }
        }
    }
    ImGui::End();

    if (network->LoggedIn())
    {
        if (ImGui::Begin("Asssts"))
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
                    network->JoinLevel(selectedLevelName, level);
                }
            }
        }
        ImGui::End();

        if (ImGui::Begin("Tiles"))
        {
            auto& tilemap = level->GetTilemap();
            if (tilemap.ID() >= 0)
            {
                ImGui::Text("Tilemap pos [%i,%i]", tilemap.ScrollX(), tilemap.ScrollY());

                std::shared_ptr<Tileset> tileset = level->TileSet();
                for (int i = 0; i < tileset->NumTiles(); i++)
                {
                    Islander::component_texture texture;
                    tileset->GetTile(i, texture);

                    if (IslanderImguiImageButton(device, cxt, i, texture.index, texture.px, texture.py, texture.sx, texture.sy))
                    {
                        palette = i;
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
                    network->CreateTilemap(width, height);
                }
            }
        }
        ImGui::End();
    }
}

