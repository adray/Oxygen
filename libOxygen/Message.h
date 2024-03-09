#pragma once
#include <string>
#include <vector>

namespace Oxygen
{
    class Message
    {
    public:
        Message(unsigned char* data, int size);
        Message(const std::string& nodeName, const std::string& messageName);
        Message(const Message& msg);
        void WriteString(const std::string& str);
        void WriteBytes(int numBytes, unsigned char* bytes);
        void WriteInt32(int value);
        void WriteDouble(double value);
        const std::string ReadString();
        int ReadInt32();
        double ReadDouble();
        void ReadBytes(int numBytes, unsigned char* bytes);
        void Prepare();
        const unsigned char* const data() const { return _data.data(); }
        const size_t size() const { return _data.size(); }
        inline const std::string& NodeName() const { return _nodeName; }
        inline const std::string& MessageName() const { return _messageName; }

    private:
        std::string _nodeName;
        std::string _messageName;
        std::vector<unsigned char> _data;
        std::vector<unsigned char>::iterator _it;
    };
}
