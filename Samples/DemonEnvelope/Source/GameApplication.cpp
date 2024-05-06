#include "GameApplication.h"
#include "API.h"
#include "DearImgui.h"
#include "imgui.h"
#include "Editor.h"
#include "Render.h"
#include "ConfigReader.h"
#include "Game.h"
#include "Item.h"
#include "Sun.h"

#include <iostream>

using namespace DE;

void GameApplication::OnStart()
{
    auto window = IslanderCreateWindow();
    //IslanderSetWindowSize(window, (int)userConfig.ScreenWidth, (int)userConfig.ScreenHeight);
    IslanderSetWindowSize(window, 1366, 768);
    IslanderSetWindowStyle(window, ISLANDER_WINDOW_STYLE_BORDER);
    IslanderSetWindowText(window, "Demon Envelope");

    auto device = IslanderCreateDevice();
    IslanderSetPreferredRenderer(device, (int)ISLANDER_RENDERER_TYPE_D3D11);
    IslanderFontDescription defaultFont;
    defaultFont.filedef = const_cast<char*>("../../../../Assets/Font/Aleo/Aleo-Regular.ttf");
    defaultFont.name = const_cast<char*>("PixgamerRegular");
    IslanderInitializeDevice(device, window, defaultFont);
    //IslanderInitializeGPUPerformanceCounters(device);

    CRIMSON_HANDLE crimson = CrimsonInitialize();
    CrimsonWindow* cWin;
    CrimsonGetWindow(crimson, &cWin);
    cWin->width = IslanderWindowWidth(window);
    cWin->height = IslanderWindowHeight(window);

    CrimsonMouseInput* cInput;
    CrimsonGetMouseInput(crimson, &cInput);

    ISLANDER_SOUND_PLAYER audioPlayer = IslanderCreateSoundPlayer(window);
    assert(IslanderIsSoundPlayerInitialized(audioPlayer));
    int soundEffectCategory = IslanderCreateSoundCategory(audioPlayer);

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();

    // Setup Dear ImGui style
    ImGui::StyleColorsDark();
    //ImGui::StyleColorsClassic();

    IslanderImguiContext context;
    context.imgui_context = ImGui::GetCurrentContext();
    ImGui::GetAllocatorFunctions((ImGuiMemAllocFunc*)&context.alloc_func, (ImGuiMemFreeFunc*)&context.free_func, &context.user_data);

    IslanderImguiCreate(device, &context);

    //io.Fonts->AddFontDefault();
    io.Fonts->AddFontFromFileTTF(defaultFont.filedef, 16);

    IslanderSetSyncInterval(device, 1);

    ISLANDER_POLYGON_LIBRARY lib = IslanderCreatePolyLibrary();
    IslanderAddDirectory(lib, "../../../../Baked");
    ISLANDER_RESOURCE_FILE tilesetTexture = IslanderGetFile(lib, "Tileset.png");
    ISLANDER_RESOURCE_FILE tilesetMaterial = IslanderGetFile(lib, "Tileset.mat");
    ISLANDER_RESOURCE resource = IslanderCreateResource(device, tilesetTexture, tilesetMaterial);
    IslanderLoadResource(device, resource);

    std::shared_ptr<Tileset> tileset = std::shared_ptr<Tileset>(new Tileset());
    ConfigReader tileCfg("../../../../Assets/tiles.cfg");
    tileset->Load(device, tileCfg);

    ConfigReader itemCfg("../../../../Assets/items.cfg");
    ConfigReader attributeCfg("../../../../Assets/attributes.cfg");
    std::shared_ptr<ItemConfig> items = std::shared_ptr<ItemConfig>(new ItemConfig(itemCfg));
    items->LoadAttributes(attributeCfg);
    for (auto& error : items->Errors())
    {
        std::cout << "Error: " << error << std::endl;
    }

    Islander::component_texture texture;
    IslanderFindMaterialTexture(device, "simon", &texture);

    Game* game = new Game();
    game->Initialize(tileset, items);

    Editor editor;
    editor.Start(lib, tileset, game, "../../../../Assets");

    Render* render = new Render();

    render->CreateConstantBuffers(device, window);
    render->LoadShaders("../../../Shaders", device);
    render->SetupPasses();
    render->SetupCamera();

    IslanderUISettings settings = {};
    settings.linePixelShader = render->LinePixelShader();
    settings.lineVertexShader = render->LineVertexShader();
    settings.filledRectPixelShader = render->LinePixelShader();
    settings.filledRectVertexShader = render->LineVertexShader();
    settings.defaultFontName = const_cast<char*>("PixgamerRegular");
    settings.textPixelShader = render->TextPixelShader();
    settings.textVertexShader = render->TextVertexShader();
    IslanderSetUISettings(device, &settings);

    IslanderUITextSetting textSettings = {};
    textSettings.id = 0;
    textSettings.size = 16;
    IslanderSetUITextSettings(device, &textSettings, 1);

    while (IslanderPumpWindow(window))
    {
        editor.Run(1 / 60.0f, window);
        game->Run(1 / 60.0f, window);

        CrimsonFrame(crimson);
        IslanderImguiNewFrame(device, &context);
        ImGui::NewFrame();
        editor.Draw(1 / 60.0f, device, window, crimson, &context);
        game->Draw(1 / 60.0f, window, crimson);

        // Render here
        render->RenderFrame(device, editor.GetLevel());
        IslanderRenderUI(device, crimson);

        ImGui::Render();
        IslanderImguiRender(device, &context);
        IslanderPresent(device);
    }
}
