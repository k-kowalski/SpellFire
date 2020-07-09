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
