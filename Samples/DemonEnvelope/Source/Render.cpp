#include "Render.h"
#include "FileSystem.h"
#include "Level.h"

using namespace DE;

void Render::SetupPasses()
{
    IslanderPassConfig pass = {};
    pass._flags = ISLANDER_RENDER_FLAGS_CLEAR;
    pass._renderTarget = -1;

    passlist = IslanderCreatePassList();
    IslanderAppendNamedPassList(passlist, "Diffuse", pass);
}

void Render::SetupCamera()
{

}

void Render::LoadShaders(const std::string& dir, ISLANDER_DEVICE device)
{
    IslanderShaderSemantic semantic[3];
    semantic[0]._desc = const_cast<char*>("POSITION");
    semantic[0]._format = ISLANDER_SEMANTIC_FLOAT3;
    semantic[0]._stream = 0;
    semantic[1]._desc = const_cast<char*>("TEXTURE");
    semantic[1]._format = ISLANDER_SEMANTIC_FLOAT2;
    semantic[1]._stream = 0;
    semantic[2]._desc = const_cast<char*>("TILE_ID");
    semantic[2]._format = ISLANDER_SEMANTIC_INT;
    semantic[2]._stream = 0;

    tilemapShader.vertexShader = IslanderLoadVertexShaderEx(device, (dir + std::string("/Tilemap.fx")).c_str(), "TilemapVertex", semantic, 3, nullptr);
    tilemapShader.pixelShader = IslanderLoadPixelShaderEx(device, (dir + std::string("/Tilemap.fx")).c_str(), "TilemapPixel", nullptr);

    spriteBatchShader.vertexShader = IslanderLoadVertexShaderEx(device, (dir + std::string("/SpriteBatch.fx")).c_str(), "SpriteBatchVertex", semantic, 3, nullptr);
    spriteBatchShader.pixelShader = IslanderLoadPixelShaderEx(device, (dir + std::string("/SpriteBatch.fx")).c_str(), "SpriteBatchPixel", nullptr);

    semantic[0]._desc = const_cast<char*>("POSITION");
    semantic[0]._format = ISLANDER_SEMANTIC_FLOAT3;
    semantic[0]._stream = 0;
    semantic[1]._desc = const_cast<char*>("COLOR");
    semantic[1]._format = ISLANDER_SEMANTIC_FLOAT4;
    semantic[1]._stream = 0;

    lineShader.vertexShader = IslanderLoadVertexShaderEx(device, (dir + std::string("/Crimson.fx")).c_str(), "CrimsonLineVertex", semantic, 2, nullptr);
    lineShader.pixelShader = IslanderLoadPixelShaderEx(device, (dir + std::string("/Crimson.fx")).c_str(), "CrimsonLinePixel", nullptr);

    semantic[0]._desc = const_cast<char*>("POSITION");
    semantic[0]._format = ISLANDER_SEMANTIC_FLOAT4;
    semantic[0]._stream = 0;
    semantic[1]._desc = const_cast<char*>("TEXCOORD");
    semantic[1]._format = ISLANDER_SEMANTIC_FLOAT;
    semantic[1]._stream = 0;

    textShader.vertexShader = IslanderLoadVertexShaderEx(device, (dir + std::string("/CrimsonFont.fx")).c_str(), "CrimsonFontVertex", semantic, 2, nullptr);
    textShader.pixelShader = IslanderLoadPixelShaderEx(device, (dir + std::string("/CrimsonFont.fx")).c_str(), "CrimsonFontPixel", nullptr);
}

void Render::CreateConstantBuffers(ISLANDER_DEVICE device, ISLANDER_WINDOW window)
{
    constantData.screenWidth = IslanderWindowWidth(window);
    constantData.screenHeight = IslanderWindowHeight(window);
}

void Render::RenderFrame(ISLANDER_DEVICE device, std::shared_ptr<Level>& level)
{
    IslanderRenderablePass passes[1];
    passes->camera = nullptr;
    passes->renderableBegin = 0;
    passes->renderableCount = 0;

    IslanderSetPassConstantData(passlist, 0, &constantData, sizeof(constantData));

    level->Render(renderable, &passes->renderableCount,
        tilemapShader.pixelShader, tilemapShader.vertexShader,
        spriteBatchShader.pixelShader, spriteBatchShader.vertexShader);

    IslanderSetPassList(device, passlist);
    IslanderRenderScene3D(device, passes, 1, renderable);
}
