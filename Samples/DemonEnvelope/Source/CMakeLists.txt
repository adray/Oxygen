cmake_minimum_required (VERSION 3.8)

# Enable Hot Reload for MSVC compilers if supported.
if (POLICY CMP0141)
  cmake_policy(SET CMP0141 NEW)
  set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<IF:$<AND:$<C_COMPILER_ID:MSVC>,$<CXX_COMPILER_ID:MSVC>>,$<$<CONFIG:Debug,RelWithDebInfo>:EditAndContinue>,$<$<CONFIG:Debug,RelWithDebInfo>:ProgramDatabase>>")
endif()

project ("DemonEnvelope")

include_directories("../../../libOxygen")

include_directories("${ISLANDER_DIR}/Types/includes")
include_directories("${ISLANDER_DIR}/Entity/includes")
include_directories("${ISLANDER_DIR}/Common/includes")
include_directories("${ISLANDER_DIR}/Renderer/includes")
include_directories("${ISLANDER_DIR}/Engine/includes")
include_directories("${ISLANDER_DIR}/Crimson/includes")
include_directories("${ISLANDER_DIR}/Numerics/includes")
include_directories("${ISLANDER_DIR}/Input/includes")
include_directories("${ISLANDER_DIR}/Model/includes")

include_directories("../../Thirdparty/imgui")

set (IMGUI_SOURCES
    "../../Thirdparty/Imgui/imgui.cpp"
    "../../Thirdparty/Imgui/imgui_widgets.cpp"
)

# Add source to this project's executable.
add_executable (DemonEnvelope 
 ${IMGUI_SOURCES}
 "Program.cpp"
 "Program.h"
 "Network.cpp"
 "Network.h"
 "GameApplication.cpp"
 "GameApplication.h"
 "Editor.h" "Editor.cpp" "Render.h" "Render.cpp" "Level.cpp" "Level.h" "Tilemap.cpp" "Tilemap.h" "Tileset.cpp" "Tileset.h" "ObjectStream.cpp" "ObjectStream.h" "EventStream.cpp" "EventStream.h")
target_link_libraries(DemonEnvelope debug ${OXYGEN_LIBS}\\x64-debug\\libOxygen.lib)
target_link_libraries(DemonEnvelope debug ${LIBCRYPTO}\\libcrypto_static.lib)

target_link_libraries(DemonEnvelope optimized ${OXYGEN_LIBS}\\x64-release\\libOxygen.lib)
target_link_libraries(DemonEnvelope optimized ${LIBCRYPTO}\\libcrypto_static.lib)


target_link_libraries(DemonEnvelope debug ${ISLANDER_LIBS}\\Entity\\Debug\\Islander.Entity.lib)
target_link_libraries(DemonEnvelope debug ${ISLANDER_LIBS}\\Common\\Debug\\islCommon.lib)
target_link_libraries(DemonEnvelope debug ${ISLANDER_LIBS}\\Renderer\\Debug\\islRenderer.lib)
target_link_libraries(DemonEnvelope debug ${ISLANDER_LIBS}\\Numerics\\Debug\\islNumerics.lib)
target_link_libraries(DemonEnvelope debug ${ISLANDER_LIBS}\\Engine\\Debug\\islEngine.lib)
target_link_libraries(DemonEnvelope debug ${ISLANDER_LIBS}\\Crimson\\Debug\\islCrimson.lib)
target_link_libraries(DemonEnvelope debug ${ISLANDER_LIBS}\\Model\\Debug\\islModel.lib)

target_link_libraries(DemonEnvelope optimized ${ISLANDER_LIBS}\\Entity\\Release\\Islander.Entity.lib)
target_link_libraries(DemonEnvelope optimized ${ISLANDER_LIBS}\\Common\\Release\\islCommon.lib)
target_link_libraries(DemonEnvelope optimized ${ISLANDER_LIBS}\\Renderer\\Release\\islRenderer.lib)
target_link_libraries(DemonEnvelope optimized ${ISLANDER_LIBS}\\Numerics\\Release\\islNumerics.lib)
target_link_libraries(DemonEnvelope optimized ${ISLANDER_LIBS}\\Engine\\Release\\islEngine.lib)
target_link_libraries(DemonEnvelope optimized ${ISLANDER_LIBS}\\Crimson\\Release\\islCrimson.lib)
target_link_libraries(DemonEnvelope optimized ${ISLANDER_LIBS}\\Model\\Release\\islModel.lib)

if (CMAKE_VERSION VERSION_GREATER 3.12)
  set_property(TARGET DemonEnvelope PROPERTY CXX_STANDARD 20)
endif()

# TODO: Add tests and install targets if needed.
