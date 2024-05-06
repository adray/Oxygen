#pragma once
#include <SunScript.h>
#include <vector>
#include <list>
#include <memory>
#include <sstream>

namespace Oxygen
{
    class Message;
}

namespace DE
{
    class Level;

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

    enum class ScriptSuspendReason
    {
        None = 0x0,
        Dialogue = 0x1,
        WaitTime = 0x2
    };

    class Script
    {
    public:
        Script(unsigned char* program);
        void Initialize(DE::Level* level);
        void RunScript(float delta);
        void Wait(float time);
        void SetSuspendReason(ScriptSuspendReason reason);
        inline std::stringstream& GetLog() { return _log; }
        inline DE::Level* Level() const { return _level; }
        ~Script();
    private:
        SunScript::VirtualMachine* _vm;
        unsigned char* _program;
        bool _suspended;
        bool _completed;
        bool _resume;
        float _waitTime;
        float _elapsedTime;
        std::stringstream _log;
        DE::Level* _level;
        ScriptSuspendReason _reason;
    };

    enum class ScriptTrigger
    {
        None = 0x0,
        OnCreate = 0x1,
        OnTouch = 0x2
    };

    class ScriptObject
    {
    public:
        ScriptObject(int id);
        void Deserialize(Oxygen::Message& msg);
        void Serialize(Oxygen::Message& msg);

        inline int Version() const { return _version; }
        inline int X() const { return _px; }
        inline int Y() const { return _py; }
        inline int ID() const { return _id; }
        inline int ParentID() const { return _parentId; }
        inline const std::string& ScriptName() const { return _scriptName; }
        inline ScriptTrigger Trigger() const { return _trigger; }

        inline void SetVersion(int version) { _version = version; }
        inline void SetParentID(int id) { _parentId = id; }
        inline void SetX(int x) { _px = x; }
        inline void SetY(int y) { _py = y; }
        inline void SetScriptName(const std::string& name) {
            _scriptName = name;
        }
        inline void SetTrigger(ScriptTrigger trigger) { _trigger = trigger; }

        void CompileScript(const std::string& directory);
        inline unsigned char* Program() { return _programData; }

        inline void SetTriggered(bool triggered) { _isTriggered = triggered; }
        inline bool IsTriggered() { return _isTriggered; }

    private:
        bool _isTriggered;
        int _version;
        int _id;
        int _px;
        int _py;
        int _parentId;
        std::string _scriptName;
        ScriptTrigger _trigger;
        unsigned char* _programData;
    };

    class Scripting
    {
    public:
        void AddScript(unsigned char* program, DE::Level* level);
        void RunScripts(float delta);
        void ClearScripts();

        inline int NumScripts() const { return int(_scripts.size()); }
        const std::string Log(int script) const;

    private:
        std::vector<Script*> _scripts;
    };
}
