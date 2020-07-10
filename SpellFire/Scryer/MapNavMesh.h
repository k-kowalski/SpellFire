#pragma once
#include <cstdint>
#include <DetourNavMesh.h>
#include <DetourNavMeshQuery.h>
#include "Vector3.h"

constexpr int32_t MaxPathLength = 1024;

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
	dtNavMesh* navmesh;
	dtNavMeshQuery* navmeshQuery;
	dtQueryFilter queryFilter;

	MapNavMesh();
	~MapNavMesh();

	bool InitializeNavmeshQuery();
	dtPolyRef FindNearestNavmeshPoly(Vector3& pos, float* nearestPoint);
};

