#include "Editor.h"
#include "Network.h"
#include "imgui.h"
#include "Window.h"
#include <memory>

using namespace DE;

void Editor::Start(ISLANDER_POLYGON_LIBRARY lib, ISLANDER_DEVICE device)
{
    network = std::shared_ptr<DE::Network>(new DE::Network());

    std::memset(username, 0, sizeof(username));
    std::memset(password, 0, sizeof(password));
    std::memset(levelName, 0, sizeof(levelName));
    
    std::strcpy(hostname, "localhost");

    level.Setup(lib, device);
}

void Editor::Run(float delta, ISLANDER_WINDOW window)
{
    network->Process();

    const int posX = IslanderMouseX(window);
    const int posY = IslanderMouseY(window);

    const int width = IslanderWindowWidth(window);
    const int height = IslanderWindowHeight(window);

    bool click = IslanderGetLeftMouseState(window) == Islander::MOUSE_INPUT_UP && left_down;
    left_down = IslanderGetLeftMouseState(window) == Islander::MOUSE_INPUT_DOWN;
    if (click)
    {
        left_down = false;

        if (palette > -1)
        {
            const int tilemap = level.TileMapHitTest(posX - width / 2, posY - height / 2);
            if (tilemap >= 0)
            {
                auto& map = level.GetTilemap(tilemap);
                const int cell = map.HitTest(posX - width / 2, posY - height / 2);
                if (cell >= 0)
                {
                    map.Set(cell, palette);
                    network->UpdateTilemap(map, level.GetState(map.ID()));
                }
            }
        }
    }
}

void Editor::Draw(float delta, ISLANDER_DEVICE device, IslanderImguiContext* cxt)
{
    if (ImGui::Begin("Network"))
    {
        if (network->Connected())
        {
            ImGui::Text("Connected");
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
        }

        for (int i = 0; i < levels.size(); i++)
        {
            if (ImGui::Button(levels[i].c_str()))
            {
                network->JoinLevel(levels[i], level);
            }
        }
    }
    ImGui::End();

    if (ImGui::Begin("Tiles"))
    {
        if (ImGui::Button("Create TileMap"))
        {
            network->CreateTilemap(100, 100);
        }

        std::shared_ptr<Tileset> tileset = level.TileSet();
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
    ImGui::End();
}

