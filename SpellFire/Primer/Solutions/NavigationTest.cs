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

		public NavigationTest(Client client) : base(client)
		{
			var ne = new NavigationEngine();
			ne.SetCurrentMap(me.Memory.ReadInt32(IntPtr.Zero + Offset.MapId));
			this.Active = false;
		}

		public override void Tick()
		{
		}

		public override void Dispose()
		{
		}
	}
}
