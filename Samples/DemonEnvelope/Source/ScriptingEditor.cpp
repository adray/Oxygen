#include "ScriptingEditor.h"
#include "imgui.h"

using namespace DE;

void DE::DrawScriptLog(const Scripting& scripting)
{
    if (ImGui::Begin("SunScript Log"))
    {
        ImGui::BeginTabBar("Log");
        static int tab = 0;
        for (int i = 0; i < scripting.NumScripts(); i++)
        {
            std::stringstream ss; ss << "Script " << (i + 1);
            if (ImGui::TabItemButton(ss.str().c_str()))
            {
                tab = i;
            }
        }
        ImGui::EndTabBar();

        if (tab >= 0 && scripting.NumScripts() > 0)
        {
            auto& log = scripting.Log(tab);
            ImGui::Text(log.c_str());
        }
    }
    ImGui::End();
}

void DE::DrawScriptingEditor(Scripting& scripting, ScriptBuilder& script)
{
    if (ImGui::Begin("SunScript"))
    {
        if (ImGui::Button("Run"))
        {
            unsigned char* program;
            script.Compile(&program);

            scripting.ClearScripts();
            scripting.AddScript(program, nullptr, -1);
        }

        auto& nodes = script.Nodes();
        std::list<std::shared_ptr<ScriptNode>>::iterator delIt = nodes.end();
        std::list<std::shared_ptr<ScriptNode>>::iterator upIt = nodes.begin();
        std::list<std::shared_ptr<ScriptNode>>::iterator downIt = nodes.end();
        int index = 0;
        auto it = nodes.begin();
        while (it != nodes.end())
        {
            auto& node = *it;
            ImGui::PushID(index);
            if (ImGui::CollapsingHeader(node->Name().c_str()))
            {
                if (ImGui::Button("Delete"))
                {
                    delIt = it;
                }
                ImGui::SameLine();
                if (ImGui::Button("Up"))
                {
                    upIt = it;
                }
                ImGui::SameLine();
                if (ImGui::Button("Down"))
                {
                    downIt = it;
                }
                
                node->Draw();
            }
            index++;
            ImGui::PopID();
            
            it++;
        }

        if (delIt != nodes.end())
        {
            nodes.erase(delIt);
        }
        else if (upIt != nodes.begin())
        {
            std::list<std::shared_ptr<ScriptNode>>::iterator cur = upIt;
            upIt--;
            std::swap(*cur, *upIt);
        }
        else if (downIt != nodes.end())
        {
            std::list<std::shared_ptr<ScriptNode>>::iterator cur = downIt;
            downIt++;
            if (downIt != nodes.end())
            {
                std::swap(*downIt, *cur);
            }
        }

        if (ImGui::BeginCombo("Add", "New Item"))
        {
            if (ImGui::Selectable("Print"))
            {
                script.Nodes().push_back(std::shared_ptr<PrintNode>(new PrintNode()));
            }
            if (ImGui::Selectable("Wait"))
            {
                script.Nodes().push_back(std::shared_ptr<WaitNode>(new WaitNode()));
            }
            if (ImGui::Selectable("String"))
            {
                script.Nodes().push_back(std::shared_ptr<StringNode>(new StringNode()));
            }
            if (ImGui::Selectable("Integer"))
            {
                script.Nodes().push_back(std::shared_ptr<IntegerNode>(new IntegerNode()));
            }

            ImGui::EndCombo();
        }
    }
    ImGui::End();
}
