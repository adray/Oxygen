#pragma once
#include <string>

namespace Oxygen
{
    class Security
    {
    public:
        Security();
        void SHA256(const std::string& str, unsigned char** digest, unsigned int* digest_len);
        ~Security();
    };
}
