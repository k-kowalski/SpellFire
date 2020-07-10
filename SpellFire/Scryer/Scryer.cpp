#include "Scryer.h"
#include "DetourNavMesh.h"
#include <unordered_map>
#include <utility>
#include "MapNavMesh.h"
#include "Utils.h"
#include <fstream>
#include <iostream>
#include <filesystem>

static std::string mmapsDirectoryPath;

static MapNavMesh* mapNavmesh;

void Navigation::InitializeNavigation(const char* movementMapsDirectoryPath)
{
	mmapsDirectoryPath = std::string(movementMapsDirectoryPath);
}

bool Navigation::LoadMap(int32_t mapId)
{
	std::cout << Utils::ScryerLogTag << "Loading map for id: " << mapId << std::endl;

	std::ifstream mmapParamsStream;
	mmapParamsStream.open(
		mmapsDirectoryPath + Utils::PadLeadingZeros(mapId, 3) + ".mmap",
		std::ifstream::binary);

	dtNavMeshParams navmeshParams;
	mmapParamsStream.read(reinterpret_cast<char*>(&navmeshParams), sizeof(dtNavMeshParams));
	mmapParamsStream.close();

	mapNavmesh = new MapNavMesh();
	
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

			std::string mmapTileFileName = mmapsDirectoryPath
				+ Utils::PadLeadingZeros(mapId, 3)
				+ Utils::PadLeadingZeros(x, 2)
				+ Utils::PadLeadingZeros(y, 2)
				+ ".mmtile";

			/* missing tiles are ok */
			if (!std::filesystem::exists(mmapTileFileName))
			{
				continue;
			}
			
			mmapTileStream.open(mmapTileFileName, std::ifstream::binary);

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

	if (!mapNavmesh->InitializeNavmeshQuery())
	{
		std::cerr << Utils::ScryerLogTag << "Couldn't initialize navmesh query" << std::endl;
		return false;
	}

	std::cout << Utils::ScryerLogTag << "Map loading successful" << std::endl;

	return true;
}

bool Navigation::CalculatePath(Vector3 start, Vector3 end, Vector3* outPathNodeBuffer, int32_t* outPathNodeCount)
{
	Vector3 startRecast = start.WowToRecast();
	Vector3 endRecast = end.WowToRecast();

	Vector3 pointStart;
	Vector3 pointEnd;

	dtPolyRef startPoly = mapNavmesh->FindNearestNavmeshPoly(startRecast, pointStart.Data());
	dtPolyRef endPoly = mapNavmesh->FindNearestNavmeshPoly(endRecast, pointEnd.Data());

	if (startPoly == endPoly)
	{
		outPathNodeBuffer = &end;
		*outPathNodeCount = 1;
		return true;
	}

	dtPolyRef polyPath[MaxPathLength];
	int32_t polyPathSize;

	if (dtStatusSucceed(mapNavmesh->navmeshQuery->findPath(
		startPoly, endPoly,
		pointStart.Data(),
		pointEnd.Data(),
		&mapNavmesh->queryFilter, polyPath, &polyPathSize, MaxPathLength)))
	{
		if (dtStatusSucceed(mapNavmesh->navmeshQuery->findStraightPath(
			pointStart.Data(),
			pointEnd.Data(),
			polyPath,
			polyPathSize,
			reinterpret_cast<float*>(outPathNodeBuffer),
			nullptr, nullptr, outPathNodeCount, MaxPathLength)))
		{
			for (int i = 0; i < (*outPathNodeCount); i++)
			{
				outPathNodeBuffer[i] = outPathNodeBuffer[i].RecastToWow();
			}

			return true;
		}
		else
		{
			std::cerr << Utils::ScryerLogTag << "Couldn't find path within polygons" << std::endl;
		}
	}
	else
	{
		std::cerr << Utils::ScryerLogTag << "Couldn't find path" << std::endl;
	}

	return false;
}
