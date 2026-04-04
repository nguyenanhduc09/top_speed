cmake_minimum_required(VERSION 3.10)

if (POLICY CMP0091)
  cmake_policy(SET CMP0091 NEW)
endif()

project(Tolk)

option (TOLK_MULTITHREADED "Build Multi-Threaded (/MT) instead of Multi-Threaded DLL (/MD)" OFF)
option (BUILD_EXAMPLES "Build example applications" OFF)

if (MSVC)
  if (TOLK_MULTITHREADED)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
  else()
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreadedDLL$<$<CONFIG:Debug>:Debug>")
  endif()

  foreach (flag_var
      CMAKE_C_FLAGS CMAKE_C_FLAGS_DEBUG CMAKE_C_FLAGS_RELEASE
      CMAKE_C_FLAGS_MINSIZEREL CMAKE_C_FLAGS_RELWITHDEBINFO
      CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE
      CMAKE_CXX_FLAGS_MINSIZEREL CMAKE_CXX_FLAGS_RELWITHDEBINFO)
    if (DEFINED ${flag_var} AND ${flag_var} MATCHES "/MD")
      string(REGEX REPLACE "/MDd?" "" ${flag_var} "${${flag_var}}")
      string(STRIP "${${flag_var}}" ${flag_var})
    endif()
  endforeach()
endif()

set (TOLK_COMPILE_FLAGS -D_EXPORTING -DUNICODE -D_UNICODE)
set (TOLK_LINK_FLAGS user32 ole32 oleaut32 sapi)
set (TOLK_WINDOWS_LIB_DIR "${PROJECT_SOURCE_DIR}/../CrossSpeak/lib/windows")

add_library(Tolk SHARED src/Tolk.cpp src/Tolk.h src/TolkVersion.h
  src/ScreenReaderDriver.h src/Tolk.rc
  src/ScreenReaderDriverBOY.cpp src/ScreenReaderDriverBOY.h
  src/ScreenReaderDriverJAWS.cpp src/ScreenReaderDriverJAWS.h
  src/ScreenReaderDriverNVDA.cpp src/ScreenReaderDriverNVDA.h
  src/ScreenReaderDriverSA.cpp src/ScreenReaderDriverSA.h
  src/ScreenReaderDriverSNova.cpp src/ScreenReaderDriverSNova.h
  src/ScreenReaderDriverWE.cpp src/ScreenReaderDriverWE.h
  src/ScreenReaderDriverZDSR.cpp src/ScreenReaderDriverZDSR.h
  src/ScreenReaderDriverZT.cpp src/ScreenReaderDriverZT.h
  src/ScreenReaderDriverSAPI.cpp src/ScreenReaderDriverSAPI.h
  src/fsapi.c src/fsapi.h src/wineyes.c src/wineyes.h src/zt.c src/zt.h)

target_include_directories (Tolk INTERFACE src)
target_link_libraries (Tolk PRIVATE ${TOLK_LINK_FLAGS})
target_compile_options (Tolk PRIVATE ${TOLK_COMPILE_FLAGS})

if(CMAKE_CXX_COMPILER_ID MATCHES "GNU")
  target_link_libraries(Tolk PUBLIC -static-libgcc;-static-libstdc++)
endif()

## ---- install
# add README
install (FILES docs/README.md DESTINATION docs)
# add LICENSE
install (FILES LICENSE.txt LICENSE-NVDA.txt DESTINATION license)
# add Binay DLL
install (TARGETS Tolk DESTINATION lib)
# Install PDB files for Debug builds
install (FILES Tolk.pdb DESTINATION lib OPTIONAL)
# add header file
install (FILES src/Tolk.h DESTINATION include)
# add Client libraries
if (WIN32)
  if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    install (FILES
      "${TOLK_WINDOWS_LIB_DIR}/nvdaControllerClient64.dll"
      "${TOLK_WINDOWS_LIB_DIR}/SAAPI64.dll"
      "${TOLK_WINDOWS_LIB_DIR}/BoyCtrl-x64.dll"
      "${TOLK_WINDOWS_LIB_DIR}/ZDSRAPI_x64.dll"
      "${TOLK_WINDOWS_LIB_DIR}/ZDSRAPI.ini"
      DESTINATION lib)
  else()
    message(FATAL_ERROR "This repository only stages the x64 Windows Tolk companion DLLs in CrossSpeak/lib/windows.")
  endif()
endif()
