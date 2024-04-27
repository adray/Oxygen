#include "ConfigReader.h"

using namespace DE;

ConfigReader::ConfigReader(const std::string& filepath)
{
    _stream.open(filepath);

    if (!_stream.bad())
    {
        char headerData[512];
        _stream.getline(headerData, sizeof(headerData));
        std::string header(headerData);

        size_t offset = 0;
        size_t next = header.find(',', offset);
        while (next != std::string::npos)
        {
            _header.push_back(header.substr(offset, next - offset));

            offset = next + 1;
            next = header.find(',', offset);
        }
        _header.push_back(header.substr(offset));
    }
}

bool ConfigReader::ReadNextRow()
{
    if (!_stream.bad() && !_stream.eof())
    {
        _row.clear();

        char rowData[512];
        _stream.getline(rowData, sizeof(rowData));
        std::string row(rowData);

        size_t offset = 0;
        size_t next = row.find(',', offset);
        while (next != std::string::npos)
        {
            _row.push_back(row.substr(offset, next - offset));

            offset = next + 1;
            next = row.find(',', offset);
        }
        _row.push_back(row.substr(offset));

        return _row.size() == _header.size();
    }

    return false;
}

const std::string ConfigReader::Get(const std::string& name) const
{
    for (int i = 0; i < _header.size(); i++)
    {
        if (_header[i] == name)
        {
            return _row[i];
        }
    }

    return "";
}
