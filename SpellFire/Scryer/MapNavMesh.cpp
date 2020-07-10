#include "MapNavMesh.h"


MapNavMesh::MapNavMesh()
{
	navmesh = dtAllocNavMesh();
	navmeshQuery = dtAllocNavMeshQuery();
}

MapNavMesh::~MapNavMesh()
{
	if (navmesh != nullptr)
	{
		dtFreeNavMesh(navmesh);
	}

	if (navmeshQuery != nullptr)
	{
		dtFreeNavMeshQuery(navmeshQuery);
	}
}

bool MapNavMesh::InitializeNavmeshQuery()
{
	return dtStatusSucceed(navmeshQuery->init(navmesh, MaxPathLength));
}

dtPolyRef MapNavMesh::FindNearestNavmeshPoly(Vector3& pos, float* nearestPoint)
{
	float extents[3] = { 5.0f, 5.0f, 5.0f };

	dtPolyRef polyRef;
	navmeshQuery->findNearestPoly(pos.Data(), extents, &queryFilter, &polyRef, nearestPoint);
	return polyRef;
}
