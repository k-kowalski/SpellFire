#pragma once
#include <string>
#include "Vector3.h"

#define SPELLFIRE_EXPORT extern "C" __declspec(dllexport)

namespace Navigation
{
	SPELLFIRE_EXPORT void InitializeNavigation(const char* movementMapsDirectoryPath);
	SPELLFIRE_EXPORT bool LoadMap(int32_t mapId);
	SPELLFIRE_EXPORT bool CalculatePath(
		Vector3 from,
		Vector3 to,
		Vector3* outPathNodeBuffer,
		int32_t*  outPathNodeCount
	);
}
