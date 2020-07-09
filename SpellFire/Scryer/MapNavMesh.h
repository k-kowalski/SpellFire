#pragma once
#include <cstdint>
#include <DetourNavMesh.h>
#include <DetourNavMeshQuery.h>

struct MmapTileHeader
{
	int32_t mmapMagic;
	int32_t dtVersion;
	int32_t mmapVersion;
	int32_t size;
	int8_t usesLiquids;
	int8_t padding[3];
};

struct MapNavMesh
{
	const int32_t MaxPathLength = 1024;
	dtNavMesh* navmesh;
	dtNavMeshQuery* navmeshQuery;

	MapNavMesh();
	~MapNavMesh();

	bool InitializeNavmeshQuery();
};

