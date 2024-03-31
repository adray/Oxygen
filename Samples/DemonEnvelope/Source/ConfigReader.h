#pragma once
#include <string>
#include <vector>
#include <fstream>

namespace DE
{
    class ConfigReader
    {
    public:
        ConfigReader(const std::string& filepath);

        bool ReadNextRow();

        const std::string Get(const std::string& name) const;

    private:
        std::ifstream _stream;
        std::vector<std::string> _header;
        std::vector<std::string> _row;
    };
}
