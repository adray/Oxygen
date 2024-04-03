#include "Scripting.h"
#include "imgui.h"
#include <functional>
#include <iostream>

using namespace DE;

static void StringInputHelper(const std::string& label, std::string& value)
{
    char buffer[256];
    std::strcpy(buffer, value.c_str());
    ImGui::InputText(label.c_str(), buffer, sizeof(buffer));
    value = buffer;
}

ScriptNode::ScriptNode(const std::string& name)
    :
    _name(name)
{
}

PrintNode::PrintNode()
    :
    ScriptNode("Print")
{
}

void PrintNode::Compile(SunScript::Program* program)
{
    SunScript::EmitPush(program, _param);
    SunScript::EmitFormat(program);
    SunScript::EmitCall(program, "Print");
}

void PrintNode::Draw()
{
    StringInputHelper("Parameter", _param);
}

WaitNode::WaitNode()
    :
    ScriptNode("Wait"),
    _wait(0)
{
}

void WaitNode::Compile(SunScript::Program* program)
{
    SunScript::EmitPush(program, _wait);
    SunScript::EmitYield(program, "Wait");
}

void WaitNode::Draw()
{
    ImGui::InputInt("Parameter", &_wait);
}


StringNode::StringNode()
    :
    ScriptNode("String")
{
}

void StringNode::Compile(SunScript::Program* program)
{
    SunScript::EmitLocal(program, _name);
    SunScript::EmitSet(program, _name, _text);
}

void StringNode::Draw()
{
    StringInputHelper("Name", _name);
    StringInputHelper("Text", _text);
}

IntegerNode::IntegerNode()
    :
    ScriptNode("Integer"),
    _value(0)
{
}

void IntegerNode::Compile(SunScript::Program* program)
{
    SunScript::EmitLocal(program, _name);
    SunScript::EmitSet(program, _name, _value);
}

void IntegerNode::Draw()
{
    StringInputHelper("Name", _name);
    ImGui::InputInt("Value", &_value);
}

ScriptBuilder::ScriptBuilder()
    :
    _program(SunScript::CreateProgram())
{
}

void ScriptBuilder::Compile(unsigned char** program)
{
    SunScript::ResetProgram(_program);

    for (auto& node : _nodes)
    {
        node->Compile(_program);
    }
    SunScript::EmitDone(_program);

    SunScript::GetProgram(_program, program);
}

ScriptBuilder::~ScriptBuilder()
{
    SunScript::ReleaseProgram(_program);
}

static void Handler(SunScript::VirtualMachine* vm)
{
    std::string name;
    SunScript::GetCallName(vm, &name);
    if (name == "Print")
    {
        std::string param;
        int intParam;
        if (SunScript::VM_OK == SunScript::GetParamString(vm, &param))
        {
            std::cout << param << std::endl;
        }
        else if (SunScript::VM_OK == SunScript::GetParamInt(vm, &intParam))
        {
            std::cout << intParam << std::endl;
        }
        else
        {
            std::cout << "Error in print." << std::endl;
        }
    }
    else if (name == "Wait")
    {
        int waitTime;
        if (SunScript::VM_OK == SunScript::GetParamInt(vm, &waitTime))
        {
            std::cout << "Now we wait.. " << waitTime << " seconds!" << std::endl;

            Script* script = reinterpret_cast<Script*>(SunScript::GetUserData(vm));
            script->Wait(waitTime);
        }
    }
}

Script::Script(unsigned char* program)
    :
    _vm(nullptr),
    _suspended(false),
    _program(program),
    _waiting(false),
    _waitTime(0.0f),
    _elapsedTime(0.0f),
    _completed(false)
{
}

void Script::Wait(float time)
{
    _waiting = true;
    _waitTime = time;
    _elapsedTime = 0.0f;
}

void Script::Initialize()
{
    _vm = SunScript::CreateVirtualMachine();
    SunScript::SetHandler(_vm, &Handler);
    SunScript::SetUserData(_vm, this);
}

Script::~Script()
{
    delete[] _program;
    SunScript::ShutdownVirtualMachine(_vm);
}

void Script::RunScript(float delta)
{
    if (_completed)
    {
        return;
    }

    if (_waiting)
    {
        _elapsedTime += delta;
        if (_elapsedTime >= _waitTime)
        {
            _waiting = false;
            _suspended = false;
            std::cout << "Script resuming.." << std::endl;
            const int code = SunScript::ResumeScript(_vm, _program);
            if (code == SunScript::VM_ERROR)
            {
                std::cout << "Script encountered an error." << std::endl;
            }
            else if (code == SunScript::VM_YIELDED)
            {
                std::cout << "Script suspended." << std::endl;
                _suspended = true;
            }
        }
    }
    else if (!_suspended)
    {
        const int code = SunScript::RunScript(_vm, _program);
        if (code == SunScript::VM_ERROR)
        {
            std::cout << "Script encountered an error." << std::endl;
        }
        else if (code == SunScript::VM_YIELDED)
        {
            std::cout << "Script suspended." << std::endl;
            _suspended = true;
        }
    }

    if (!_suspended)
    {
        _completed = true;
        std::cout << "Script ended." << std::endl;
    }
}

void Scripting::AddScript(unsigned char* program)
{
    _scripts.emplace_back(new Script(program))->Initialize();
}

void Scripting::RunScripts(float delta)
{
    for (auto& script : _scripts)
    {
        script->RunScript(delta);
    }
}

void Scripting::ClearScripts()
{
    for (auto& script : _scripts)
    {
        delete script;
    }
    _scripts.clear();
}
