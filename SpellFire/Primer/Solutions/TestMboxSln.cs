using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Well.Lua;
using SpellFire.Well.Mbox;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	public class TestMboxSln : MultiboxSolution
	{
		private readonly InputMultiplexer inputMultiplexer;

		public TestMboxSln(IEnumerable<Client> clients) : base(clients)
		{
			inputMultiplexer = new InputMultiplexer(
				me.ControlInterface.hostControl,
				new List<IntPtr>(slaves.Select(s => s.Process.MainWindowHandle))
				);

			inputMultiplexer.BroadcastKeys.AddRange(new[]
			{
				Keys.W, Keys.A, Keys.S, Keys.D,
				Keys.Left, Keys.Right, Keys.Up, Keys.Down,
				Keys.Space
			});

			this.Active = true;
		}

		public override void Tick()
		{
		}

		public override void Dispose()
		{
		}
	}
}
