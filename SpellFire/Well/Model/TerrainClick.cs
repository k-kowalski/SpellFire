using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Util;

namespace SpellFire.Well.Model
{
	[Serializable]
	public struct TerrainClick
	{
		public Int64 GUID;
		public Vector3 Coordinates;
	}
}
