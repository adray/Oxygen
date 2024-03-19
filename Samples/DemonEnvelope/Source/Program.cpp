#include "Program.h"
#include "GameApplication.h"
#include <memory>

int main()
{
    std::unique_ptr<DE::GameApplication> app = std::unique_ptr<DE::GameApplication>(new DE::GameApplication());
    app->Start();

    return 0;
}
