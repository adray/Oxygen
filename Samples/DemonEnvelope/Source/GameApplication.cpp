#include "GameApplication.h"
#include "API.h"
#include "DearImgui.h"
#include "imgui.h"
#include "Editor.h"
#include "Render.h"

using namespace DE;

void GameApplication::OnStart()
{
    auto window = IslanderCreateWindow();
    //IslanderSetWindowSize(window, (int)userConfig.ScreenWidth, (int)userConfig.ScreenHeight);
    IslanderSetWindowSize(window, 1366, 768);

    auto device = IslanderCreateDevice();
    IslanderSetPreferredRenderer(device, (int)ISLANDER_RENDERER_TYPE_D3D11);
    IslanderFontDescription defaultFont;
    defaultFont.filedef = const_cast<char*>("../../../../Assets/Font/Aleo/Aleo-Regular.ttf");
    defaultFont.name = const_cast<char*>("Aleo-Regular");
    IslanderInitializeDevice(device, window, defaultFont);
    //IslanderInitializeGPUPerformanceCounters(device);

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
    io.Fonts->AddFontFromFileTTF(defaultFont.filedef, 14);

    IslanderSetSyncInterval(device, 1);

    ISLANDER_POLYGON_LIBRARY lib = IslanderCreatePolyLibrary();
    IslanderAddDirectory(lib, "../../../../Baked");
    ISLANDER_RESOURCE_FILE tilesetTexture = IslanderGetFile(lib, "Tileset.png");
    ISLANDER_RESOURCE_FILE tilesetMaterial = IslanderGetFile(lib, "Tileset.mat");
    ISLANDER_RESOURCE resource = IslanderCreateResource(device, tilesetTexture, tilesetMaterial);
    IslanderLoadResource(device, resource);

    Editor editor;
    editor.Start(lib, device);

    Render* render = new Render();

    render->CreateConstantBuffers(device, window);
    render->LoadShaders("../../../Shaders", device);
    render->SetupPasses();
    render->SetupCamera();

    while (IslanderPumpWindow(window))
    {
        editor.Run(1 / 60.0f, window);

        IslanderImguiNewFrame(device, &context);
        ImGui::NewFrame();
        editor.Draw(1 / 60.0f, device, &context);

        // Render here
        render->RenderFrame(device, editor.GetLevel());

        ImGui::Render();
        IslanderImguiRender(device, &context);
        IslanderPresent(device);
    }
}
