using SpellFire.Well.Controller;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SpellFire.Primer.Gui;
using SpellFire.Well.Lua;
using SpellFire.Well.Model;
using SpellFire.Well.Navigation;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	public class WaypointMapper : Solution
	{
		private ControlInterface ci;

		private int mapId;
		List<Vector3> positions = new List<Vector3>();

		public WaypointMapper(Client client) : base(client)
		{
			ci = client.ControlInterface;
			me.LuaEventListener.Bind("do", ControlEvent);

			this.Active = true;
		}

		private void ControlEvent(LuaEventArgs args)
		{
			if (args.Args[0] == "add")
			{
				if (!positions.Any())
				{
					mapId = me.Memory.ReadInt32(IntPtr.Zero + Offset.MapId);
					Console.WriteLine($"First mark, setting current map id: {mapId}");
				}

				var pos = me.Player.Coordinates;
				Console.WriteLine($"Added coord: {pos}");
				positions.Add(pos);
			}
			else if (args.Args[0] == "clear")
			{
				Console.WriteLine($"Clearing current mapping");
				positions.Clear();
				mapId = 0;
			}
			else if(args.Args[0] == "save")
			{
				Console.WriteLine($"Saving current mapping to file {args.Args[1]}");

				var map = new WaypointMap
				{
					mapId = this.mapId,
					waypoints = positions
				};

				File.WriteAllText(args.Args[1], JsonConvert.SerializeObject(map));
			}
			else if (args.Args[0] == "load")
			{
				Console.WriteLine($"Loading map from file {args.Args[1]}");

				var mapSerialized = File.ReadAllText(args.Args[1]);
				var map = JsonConvert.DeserializeObject<WaypointMap>(mapSerialized);

				positions = map.waypoints;
				mapId = map.mapId;

				Console.WriteLine($"map id: {mapId}");
				Console.WriteLine($"waypoints count: {positions.Count}");
			}
		}

		public override void Tick()
		{
			Thread.Sleep(1000);

			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}
		}

		public override void Dispose()
		{
			me.LuaEventListener.Dispose();
		}
	}
}
