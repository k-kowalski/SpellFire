#include "Scryer.h"
#include "DetourNavMesh.h"
#include <unordered_map>
#include <utility>
#include "MapNavMesh.h"
#include "Utils.h"
#include <fstream>
#include <iostream>

static std::string mmapsDirectoryPath;

static std::unordered_map<int32_t, std::shared_ptr<MapNavMesh>> cachedNavmeshes;

void Navigation::InitializeNavigation(const char* movementMapsDirectoryPath)
{
	mmapsDirectoryPath = std::string(movementMapsDirectoryPath);
	std::cout << "Hello path: " << mmapsDirectoryPath << std::endl;
}

bool Navigation::LoadMap(int32_t mapId)
{
	std::ifstream mmapParamsStream;
	mmapParamsStream.open(
		mmapsDirectoryPath + Utils::PadLeadingZeros(mapId, 3) + ".mmap",
		std::ifstream::binary);

	dtNavMeshParams navmeshParams;
	mmapParamsStream.read(reinterpret_cast<char*>(&navmeshParams), sizeof(dtNavMeshParams));
	mmapParamsStream.close();

	std::shared_ptr<MapNavMesh> mapNavmesh;
	cachedNavmeshes[mapId] = mapNavmesh;
	
	if (dtStatusFailed(mapNavmesh->navmesh->init(&navmeshParams)))
	{
		std::cerr << Utils::ScryerLogTag << "Couldn't initialize navmesh" << std::endl;
		return false;
	}

	for (int x = 1; x <= 64; x++)
	{
		for (int y = 1; y <= 64; y++)
		{
			std::ifstream mmapTileStream;
			mmapTileStream.open(
				mmapsDirectoryPath
				+ Utils::PadLeadingZeros(mapId, 3)
				+ Utils::PadLeadingZeros(x, 2)
				+ Utils::PadLeadingZeros(y, 2)
				+ ".mmtile",
				std::ifstream::binary);

			MmapTileHeader mmapTileHeader;
			mmapTileStream.read(reinterpret_cast<char*>(&mmapTileHeader), sizeof(MmapTileHeader));

			void* mmapTileData = dtAlloc(mmapTileHeader.size, DT_ALLOC_PERM);
			mmapTileStream.read(reinterpret_cast<char*>(mmapTileData), mmapTileHeader.size);
			mmapTileStream.close();

			dtTileRef tileRef;
			if (dtStatusFailed(
				mapNavmesh->navmesh->addTile(
				reinterpret_cast<unsigned char*>(mmapTileData),
				mmapTileHeader.size, DT_TILE_FREE_DATA, 0, &tileRef)))
			{
				std::cerr << Utils::ScryerLogTag << "Couldn't add tile to navmesh" << std::endl;
				dtFree(mmapTileData);
				return false;
			}
		}
	}

	return mapNavmesh->InitializeNavmeshQuery();
}
