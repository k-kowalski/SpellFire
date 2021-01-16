using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.Util;

namespace SpellFire.Well.Navigation
{
	public class NavigationEngine
	{
		private const string NavigationModule = "Scryer.dll";
		private readonly Vector3[] PathNodesBuffer = new Vector3[1024];

		#region Imports
		[DllImport(NavigationModule, CallingConvention = CallingConvention.Cdecl)]
		private static extern void InitializeNavigation(string movementMapsDirectoryPath);

		[DllImport(NavigationModule, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool LoadMap(Int32 mapId);

		[DllImport(NavigationModule, CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe bool CalculatePath(
			Vector3 start,
			Vector3 end,
			Vector3* pathNodesBuffer,
			out Int32 outPathNodeCount);
		#endregion

		public NavigationEngine()
		{
			InitializeNavigation(SFConfig.Global.MovementMapsDirectoryPath);
		}

		public bool SetCurrentMap(Int32 mapId)
		{
			return LoadMap(mapId);
		}

		public IList<Vector3> GetPath(Vector3 start, Vector3 end)
		{
			Int32 count;
			unsafe
			{
				fixed (Vector3* pathNodesBufferPtr = PathNodesBuffer)
				{
					if (CalculatePath(start, end, pathNodesBufferPtr, out count) && count > 1)
					{
						return new ArraySegment<Vector3>(PathNodesBuffer, 1, count);
					}
				}
			}

			return null;
		}

		public Vector3? GetNextPathNode(Vector3 start, Vector3 end)
		{
			var path = GetPath(start, end);
			if (path != null)
			{
				if (start.Distance(path[0]) < 1f && path.Count > 1)
				{
					return path[1];
				}
				else
				{
					return path[0];
				}
			}

			return null;
		}
	}
}
