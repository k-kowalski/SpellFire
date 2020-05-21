using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Well.Lua;
using SpellFire.Well.Mbox;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox
{
	public class TestMboxSln : MultiboxSolution
	{
		private readonly InputMultiplexer inputMultiplexer;
		private bool slavesAI;
		private bool masterAI;

		private Action<IList<string>> GetCommand(string cmd)
		{
			return cmd switch
			{
				/* command all Slaves to follow Master */
				"follow" => new Action<IList<string>>(((args) =>
				{
					string masterName = me.ControlInterface.remoteControl.GetUnitName(me.Player.GetAddress());

					foreach (Client slave in slaves)
					{
						slave
							.ControlInterface
							.remoteControl
							.FrameScript__Execute($"FollowUnit('{masterName}')", 0, 0);
					}
				})),
				/* command selected Slave to cast spell on current Master target */
				"castspell" => new Action<IList<string>>(((args) =>
				{
					string casterName = args[0];
					string spellName = args[1];
					Int64 targetGuid = me.GetTargetGUID();

					Client caster = slaves.FirstOrDefault(c =>
						c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == casterName);

					string spellLink = caster.ExecLuaAndGetResult(
						$"link = GetSpellLink('{spellName}')",
						"link");
					string spellID = spellLink.Split('|')[2].Split(':')[1];
					caster.ControlInterface.remoteControl.Spell_C__CastSpell(Int32.Parse(spellID), IntPtr.Zero,
						targetGuid, false);
				})),
				/* toggles AI(individual behaviour loops) */
				"toggleai" => new Action<IList<string>>(((args) =>
				{
					string switchArg = args[0];

					if (switchArg.Equals("m"))
					{
						masterAI = !masterAI;
						string state = masterAI ? "ON" : "OFF";
						Console.WriteLine($"MASTER AI is now {state}.");
					}
					else if (switchArg.Equals("s"))
					{
						slavesAI = !slavesAI;
						string state = slavesAI ? "ON" : "OFF";
						Console.WriteLine($"SLAVES AI is now {state}.");
					}
				})),
				_ => null,
			};
		}


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
					Console.Write($"[{slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress())}] Whisper on slave!");
				});

				/* invite slaves to party */
				me.ControlInterface.remoteControl.FrameScript__Execute($"InviteUnit('{slavePlayerName}')", 0, 0 );
			}

			me.LuaEventListener.Bind("MY_CMD", args =>
			{
				GetCommand(args.Args[0]).Invoke(new List<string>(args.Args.Skip(1)));
			});

			/* turn on follow initially */
			GetCommand("follow").Invoke(null);

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
