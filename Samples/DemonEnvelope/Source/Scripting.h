#pragma once
#include <SunScript.h>
#include <vector>
#include <list>
#include <memory>

namespace DE
{
    class ScriptNode
    {
    public:
        ScriptNode(const std::string& name);
        inline const std::string& Name() const { return _name; };
        virtual void Compile(SunScript::Program* program) = 0;
        virtual void Draw() = 0;
        virtual ~ScriptNode() {};
    private:
        std::string _name;
    };

    class PrintNode : public ScriptNode
    {
    public:
        PrintNode();
        void Compile(SunScript::Program* program);
        void Draw();
    private:
        std::string _param;
    };

    class WaitNode : public ScriptNode
    {
    public:
        WaitNode();
        void Compile(SunScript::Program* program);
        void Draw();

    private:
        int _wait;
    };

    class StringNode : public ScriptNode
    {
    public:
        StringNode();
        void Compile(SunScript::Program* program);
        void Draw();

    private:
        std::string _name;
        std::string _text;
    };

    class IntegerNode : public ScriptNode
    {
    public:
        IntegerNode();
        void Compile(SunScript::Program* program);
        void Draw();

    private:
        std::string _name;
        int _value;
    };

    class ScriptBuilder
    {
    public:
        ScriptBuilder();
        void Compile(unsigned char** program);
        std::list<std::shared_ptr<ScriptNode>>& Nodes() { return _nodes; }
        ~ScriptBuilder();
    private:
        SunScript::Program* _program;
        std::list<std::shared_ptr<ScriptNode>> _nodes;
    };

    class Script
    {
    public:
        Script(unsigned char* program);
        void Initialize();
        void RunScript(float delta);
        void Wait(float time);
        ~Script();
    private:
        SunScript::VirtualMachine* _vm;
        unsigned char* _program;
        bool _suspended;
        bool _completed;
        bool _waiting;
        float _waitTime;
        float _elapsedTime;
    };

    class Scripting
    {
    public:
        void AddScript(unsigned char* program);
        void RunScripts(float delta);
        void ClearScripts();

    private:
        std::vector<Script*> _scripts;
    };
}
