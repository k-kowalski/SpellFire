#pragma once

struct Vector3
{
	float x, y, z;	

	inline Vector3 WowToRecast()
	{
		return Vector3{
			y, z, x
		};
	}

	inline Vector3 RecastToWow()
	{
		return Vector3{
			z, x, y
		};
	}

	inline float* Data()
	{
		return &x;
	}
};