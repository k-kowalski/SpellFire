using SpellFire.Well.Controller;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Primer.Gui;
using SpellFire.Well.Lua;
using SpellFire.Well.Model;
using SpellFire.Well.Navigation;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	public class NavigationTest : Solution
	{
		private NavigationEngine navEngine;

		private Vector3 previousPosition = Vector3.Zero;
		private int stuckMeter = 0;
		private const int stuckToleranceLimit = 5;

		public NavigationTest(Client client) : base(client)
		{
			navEngine = new NavigationEngine();

			if (!navEngine.SetCurrentMap(me.Memory.ReadInt32(IntPtr.Zero + Offset.MapId)))
			{
				throw new Exception();
			}

			this.Active = true;
		}

		public override void Tick()
		{
			Thread.Sleep(1);
			me.GetObjectMgrAndPlayer();

			var guid = me.GetTargetGUID();
			if (guid == 0)
			{
				return;
			}
			var tar = me.ObjectManager.FirstOrDefault(obj => obj.GUID == guid);
			if (tar != null)
			{
				Navigate(tar.Coordinates);
			}
		}

		private void Navigate(Vector3 destination)
		{
			Vector3 playerPosition = me.Player.Coordinates;
			if (playerPosition.Distance(destination) < 1f)
			{
				previousPosition = Vector3.Zero;
				stuckMeter = 0;
				return;
			}

			var waypoint = navEngine.GetNextPathNode(me.Player.Coordinates, destination);
			if (waypoint != null)
			{
				Vector3 end = waypoint.Value;

				Int64 ctmGuid = 0;
				me.ControlInterface
					.remoteControl
					.CGPlayer_C__ClickToMove(
						me.Player.GetAddress(), ClickToMoveType.Move, ref ctmGuid, ref end, 1f);

				if (playerPosition.Distance(previousPosition) < 0.01f)
				{
					stuckMeter++;
					if (stuckMeter == stuckToleranceLimit)
					{
						me.ControlInterface
							.remoteControl
							.FrameScript__Execute("JumpOrAscendStart()", 0, 0);
						stuckMeter = 0;
					}
				}
				else
				{
					stuckMeter = 0;
				}

				previousPosition = playerPosition;
			}
		}

		public override void Dispose()
		{
		}
	}
}