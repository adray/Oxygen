#pragma once
#include "API.h"

namespace DE
{
    class Level;

    class Render
    {
    public:
        void SetupPasses();
        void SetupCamera();
        void RenderFrame(ISLANDER_DEVICE device, std::shared_ptr<Level>& level);
        void LoadShaders(const std::string& dir, ISLANDER_DEVICE device);
        void CreateConstantBuffers(ISLANDER_DEVICE device, ISLANDER_WINDOW window);

        inline int LineVertexShader() const { return lineShader.vertexShader; }
        inline int LinePixelShader() const { return lineShader.pixelShader; }

        inline int TextVertexShader() const { return textShader.vertexShader; }
        inline int TextPixelShader() const { return textShader.pixelShader; }

    private:
        ISLANDER_PASS_LIST passlist;
        IslanderRenderable renderable[128];

        struct ConstantData
        {
            float screenWidth;
            float screenHeight;
            float padding[2];
        };

        ConstantData constantData;

        struct Shader {
            int vertexShader;
            int pixelShader;
        };

        Shader tilemapShader;
        Shader lineShader;
        Shader spriteBatchShader;
        Shader textShader;
    };
};
