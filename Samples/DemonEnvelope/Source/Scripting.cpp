#include "Scripting.h"
#include "imgui.h"
#include "Message.h"
#include "Sun.h"
#include "Level.h"
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
    Script* script = reinterpret_cast<Script*>(SunScript::GetUserData(vm));

    std::string name;
    SunScript::GetCallName(vm, &name);
    if (name == "Print")
    {
        std::string param;
        int intParam;
        if (SunScript::VM_OK == SunScript::GetParamString(vm, &param))
        {
            script->GetLog() << param << std::endl;
        }
        else if (SunScript::VM_OK == SunScript::GetParamInt(vm, &intParam))
        {
            script->GetLog() << intParam << std::endl;
        }
        else
        {
            script->GetLog() << "Error in print." << std::endl;
        }
    }
    else if (name == "Wait")
    {
        int waitTime;
        if (SunScript::VM_OK == SunScript::GetParamInt(vm, &waitTime))
        {
            script->GetLog() << "Now we wait.. " << waitTime << " seconds!" << std::endl;

            script->Wait(waitTime);
        }
    }
    else if (name == "ShowDialogue")
    {
        std::string param1;
        if (SunScript::VM_OK == SunScript::GetParamString(vm, &param1))
        {
            std::string param2;
            if (SunScript::VM_OK == SunScript::GetParamString(vm, &param2))
            {
                script->Level()->GetDialogue().Show(param1, param2);
            }
            else
            {
                script->Level()->GetDialogue().Show(param1);
            }

            script->SetSuspendReason(ScriptSuspendReason::Dialogue);
        }
    }
    else if (name == "AddEntity")
    {
        const int entity = script->Level()->AddEntity();
        SunScript::PushReturnValue(vm, entity);
    }
    else if (name == "GetPosX")
    {
        int id;
        if (SunScript::VM_OK == SunScript::GetParamInt(vm, &id))
        {
            int px; int py;
            script->Level()->GetEntityPos(id, &px, &py);
            SunScript::PushReturnValue(vm, px);
        }
    }
    else if (name == "GetPosY")
    {
        int id;
        if (SunScript::VM_OK == SunScript::GetParamInt(vm, &id))
        {
            int px; int py;
            script->Level()->GetEntityPos(id, &px, &py);
            SunScript::PushReturnValue(vm, py);
        }
    }
    else if (name == "SetPos")
    {
        int id, x, y;
        if (SunScript::VM_OK == SunScript::GetParamInt(vm, &id) &&
            SunScript::VM_OK == SunScript::GetParamInt(vm, &x) &&
            SunScript::VM_OK == SunScript::GetParamInt(vm, &y))
        {
            script->Level()->SetEntityPos(id, x, y);
        }
    }
}

Script::Script(unsigned char* program)
    :
    _vm(nullptr),
    _suspended(false),
    _program(program),
    _waitTime(0.0f),
    _elapsedTime(0.0f),
    _completed(false),
    _resume(false),
    _level(nullptr),
    _reason(ScriptSuspendReason::None)
{
}

void Script::Wait(float time)
{
    _reason = ScriptSuspendReason::WaitTime;
    _waitTime = time;
    _elapsedTime = 0.0f;
}

void Script::SetSuspendReason(ScriptSuspendReason reason)
{
    _reason = reason;
}

void Script::Initialize(DE::Level* level)
{
    _vm = SunScript::CreateVirtualMachine();
    _level = level;
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

    if (_reason == ScriptSuspendReason::WaitTime)
    {
        _elapsedTime += delta;
        if (_elapsedTime >= _waitTime)
        {
            _resume = true;
        }
    }
    else if (_reason == ScriptSuspendReason::Dialogue)
    {
        if (!_level->GetDialogue().IsShowing())
        {
            _resume = true;
        }
    }
    
    if (_suspended && _resume)
    {
        _suspended = false;
        _resume = false;
        _reason = ScriptSuspendReason::None;
        //_log << "Script resuming.." << std::endl;
        const int code = SunScript::ResumeScript(_vm, _program);
        if (code == SunScript::VM_ERROR)
        {
            _log << "Script encountered an error." << std::endl;
        }
        else if (code == SunScript::VM_YIELDED)
        {
            //_log << "Script suspended." << std::endl;
            _suspended = true;
        }
        else if (code == SunScript::VM_TIMEOUT)
        {
            _suspended = true;
            _resume = true;
        }
    }
    else if (!_suspended)
    {
        _log << "Script started." << std::endl;
        const int code = SunScript::RunScript(_vm, _program, std::chrono::duration<int, std::nano>(1000));
        if (code == SunScript::VM_ERROR)
        {
            _log << "Script encountered an error." << std::endl;
        }
        else if (code == SunScript::VM_YIELDED)
        {
            //_log << "Script suspended." << std::endl;
            _suspended = true;
        }
        else if (code == SunScript::VM_TIMEOUT)
        {
            _suspended = true;
            _resume = true;
        }
    }

    if (!_suspended)
    {
        _completed = true;
        _log << "Script ended." << std::endl;
    }
}

//=================
// Script Object
//=================

ScriptObject::ScriptObject(int id)
    :
    _id(id), _px(0), _py(0),
    _parentId(-1),
    _trigger(ScriptTrigger::None),
    _programData(nullptr), 
    _version(0),
    _isTriggered(false)
{
}

void ScriptObject::Deserialize(Oxygen::Message& msg)
{
    const int scriptTriggerType = msg.ReadInt32();
    _trigger = (ScriptTrigger)scriptTriggerType;
    _px = msg.ReadInt32();
    _py = msg.ReadInt32();
    _parentId = msg.ReadInt32();
    _scriptName = msg.ReadString();
}

void ScriptObject::Serialize(Oxygen::Message& msg)
{
    msg.WriteInt32((int)_trigger);
    msg.WriteInt32(_px);
    msg.WriteInt32(_py);
    msg.WriteInt32(_parentId);
    msg.WriteString(_scriptName);
}

void ScriptObject::CompileScript(const std::string& directory)
{
    SunScript::CompileFile(directory + "\\" + _scriptName, &_programData);
}

//=============
// Scripting
//=============

void Scripting::AddScript(unsigned char* program, DE::Level* level)
{
    _scripts.emplace_back(new Script(program))->Initialize(level);
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

const std::string Scripting::Log(int script) const
{
    return _scripts[script]->GetLog().str();
}
