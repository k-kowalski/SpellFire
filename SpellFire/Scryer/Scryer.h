#pragma once
#include <string>

#define SPELLFIRE_EXPORT extern "C" __declspec(dllexport)

namespace Navigation
{
	SPELLFIRE_EXPORT void InitializeNavigation(const char* movementMapsDirectoryPath);
	SPELLFIRE_EXPORT bool LoadMap(int32_t mapId);
}
