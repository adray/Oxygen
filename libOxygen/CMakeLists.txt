﻿cmake_minimum_required (VERSION 3.8)

# Enable Hot Reload for MSVC compilers if supported.
if (POLICY CMP0141)
  cmake_policy(SET CMP0141 NEW)
  set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<IF:$<AND:$<C_COMPILER_ID:MSVC>,$<CXX_COMPILER_ID:MSVC>>,$<$<CONFIG:Debug,RelWithDebInfo>:EditAndContinue>,$<$<CONFIG:Debug,RelWithDebInfo>:ProgramDatabase>>")
endif()

project ("libOxygen")

include_directories(${LIBCRYPTO_HEADERS})

# Add source to this project's executable.
add_library (libOxygen "ClientConnection.cpp" "ClientConnection.h" "Message.h" "Message.cpp" "Subscriber.cpp" "Subscriber.h" "DeltaCompress.cpp" "DeltaCompress.h" "Security.cpp" "Security.h" "ObjectStream.cpp" "ObjectStream.h" "EventStream.cpp" "EventStream.h" "Metrics.cpp" "Metrics.h"   "AssetService.h" "AssetService.cpp" "PluginService.cpp" "PluginService.h" "BuildService.cpp" "BuildService.h" "DownloadStream.cpp" "DownloadStream.h" "UploadStream.cpp" "UploadStream.h")

if (CMAKE_VERSION VERSION_GREATER 3.12)
  set_property(TARGET libOxygen PROPERTY CXX_STANDARD 20)
endif()

# TODO: Add tests and install targets if needed.
