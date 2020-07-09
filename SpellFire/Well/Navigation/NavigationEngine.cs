using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.Util;

namespace SpellFire.Well.Navigation
{
	public class NavigationEngine
	{
		private const string NavigationModule = "Scryer.dll";
		#region Imports

		[DllImport(NavigationModule, CallingConvention = CallingConvention.Cdecl)]
		private static extern void InitializeNavigation(string movementMapsDirectoryPath);

		[DllImport(NavigationModule, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool LoadMap(Int32 mapId);

		#endregion

		public NavigationEngine()
		{
			InitializeNavigation(SFConfig.Global.MovementMapsDirectoryPath);
		}
	}
}
