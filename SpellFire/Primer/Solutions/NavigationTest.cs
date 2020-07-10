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
		private NavigationEngine navEngine = new NavigationEngine();

		private IList<Vector3> currentPath;
		private int currentNodeIndex;

		public NavigationTest(Client client) : base(client)
		{
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
				if (currentPath == null)
				{
					var path = navEngine.GetPath(me.Player.Coordinates, tar.Coordinates);
					if (path != null)
					{
						currentPath = path;
						currentNodeIndex = 0;
					}
				}
			}

			Advance();
		}

		private void Advance()
		{
			if (currentPath == null)
			{
				return;
			}

			if ((me.Player.Coordinates - currentPath[currentNodeIndex]).Length() < 1f)
			{
				// reached checkpoint
				currentNodeIndex++;
				if (currentNodeIndex == currentPath.Count)
				{
					// reached end
					currentPath = null;
					return;
				}
			}

			Int64 ctmGuid = 0;
			Vector3 target = currentPath[currentNodeIndex];
			me.ControlInterface
				.remoteControl
				.CGPlayer_C__ClickToMove(
					me.Player.GetAddress(), ClickToMoveType.Move, ref ctmGuid, ref target, 1f);
		}

		public override void Dispose()
		{
		}
	}
}
