using System;
using System.Runtime.InteropServices;

namespace SpellFire.Well.Util
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3
	{
		public static readonly Vector3 Zero = new Vector3(0, 0, 0);

		public readonly float x;
		public readonly float y;
		public readonly float z;

		public Vector3(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static Vector3 Random()
		{
			return new Vector3((float)SFUtil.RandomGenerator.NextDouble(),
				(float)SFUtil.RandomGenerator.NextDouble(), (float)SFUtil.RandomGenerator.NextDouble());
		}

		public static Vector3 operator -(Vector3 left, Vector3 right)
		{
			return new Vector3(left.x - right.x, left.y - right.y, left.z - right.z);
		}

		public float Length()
		{
			return (float) Math.Sqrt(x*x + y*y + z*z);
		}

		public float AngleBetween(Vector3 other)
		{
			return (float) Math.Atan2(other.y - this.y, other.x - this.x);
		}

		public float Distance(Vector3 other)
		{
			return (this - other).Length();
		}

		public override string ToString()
		{
			return $"x:{x}  y:{y}  z:{z}";
		}
	}
}
