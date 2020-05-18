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
				Keys.Space
			});

			me.GetObjectMgrAndPlayer();

			foreach (Client slave in slaves)
			{
				slave.GetObjectMgrAndPlayer();

				string slavePlayerName = slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress());

				/* set event listeners */
				slave.LuaEventListener.Bind("PARTY_INVITE_REQUEST", args => slave
					.ControlInterface.remoteControl.FrameScript__Execute("AcceptGroup()", 0, 0));
				slave.LuaEventListener.Bind("PARTY_MEMBERS_CHANGED", args => slave
					.ControlInterface.remoteControl.FrameScript__Execute("StaticPopup_Hide('PARTY_INVITE')", 0, 0));
				slave.LuaEventListener.Bind("CHAT_MSG_WHISPER",args => /* TODO: display desktop toast notfication? */
					{
						Console.Write($"[{slavePlayerName}] Whisper on slave!");
					});

				/* invite slaves to party */
				me.ControlInterface.remoteControl.FrameScript__Execute($"InviteUnit('{slavePlayerName}')", 0, 0 );
			}

			me.LuaEventListener.Bind("MY_SF_EVENT", args => Console.WriteLine(args.Args[0]));

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
