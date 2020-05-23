using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Mbox;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox
{
	public class TestMboxSln : MultiboxSolution
	{
		private readonly InputMultiplexer inputMultiplexer;
		private bool slavesAI;
		private bool masterAI;
		private IList<Task> slavesTasks;

		private Action<IList<string>> GetCommand(string cmd)
		{
			return cmd switch
			{
				/* command all Slaves to follow Master */
				"follow" => new Action<IList<string>>(((args) =>
				{
					string masterName = me.ControlInterface.remoteControl.GetUnitName(me.Player.GetAddress());

					foreach (Client slave in Slaves)
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

					Client caster = Slaves.FirstOrDefault(c =>
						c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == casterName);

					caster.CastSpellOnGuid(spellName, targetGuid);
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
			slavesTasks = new List<Task>();

			inputMultiplexer = new InputMultiplexer(
				me.ControlInterface.hostControl,
				new List<IntPtr>(Slaves.Select(s => s.Process.MainWindowHandle))
				);

			inputMultiplexer.BroadcastKeys.AddRange(new[]
			{
				Keys.Space
			});

			me.GetObjectMgrAndPlayer();

			foreach (Client slave in Slaves)
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
					Console.Write($"[{slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress())}] Whisper to slave!");
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

			AssignRoutines();
		}

		private void AssignRoutines()
		{
			foreach (Client slave in Slaves)
			{
				if (slave.RoutineName != null)
				{
					MethodInfo routineInfo = GetType().GetMethod(slave.RoutineName,
						BindingFlags.NonPublic | BindingFlags.Instance);
					if (routineInfo != null)
					{
						Console.WriteLine($"Bound routine {slave.RoutineName}.");
						slavesTasks.Add(
							Task.Run((() =>
							{
								while (Active)
								{
									routineInfo.Invoke(this, new object[] {slave});
								}
							}))
						);
					}
					else
					{
						Console.WriteLine($"Could not find routine {slave.RoutineName}.");
					}
				}
			}
		}

		private void Priest(Client c)
		{
			Thread.Sleep(200);
			if (!slavesAI)
			{
				return;
			}

			if (c.IsOnCooldown("Smite")) /* global cooldown check */
			{
				return;
			}

			foreach (Client client in clients)
			{
				if (client.Player.HealthPct < 70
				    && (!client.HasAura(client.Player, "Renew", null)))
				{
					c.CastSpellOnGuid("Renew", client.Player.GUID);
					return;
				}
			}

			GameObject target = GetMasterAttackTarget(c);
			if (target == null)
			{
				return;
			}

			c.ControlInterface.remoteControl.SelectUnit(target.GUID);
			c.CastSpell("Smite");
		}

		private void Warlock(Client c)
		{

			if (c.IsOnCooldown("Shadow Bolt")) /* global cooldown check */
			{
				return;
			}

			Thread.Sleep(200);
			if (!slavesAI)
			{
				return;
			}

			GameObject target = GetMasterAttackTarget(c);
			if (target == null)
			{
				return;
			}

			c.ControlInterface.remoteControl.SelectUnit(target.GUID);
			c.CastSpell("Shadow Bolt");
		}

		private GameObject GetMasterAttackTarget(Client c)
		{
			Int64 targetGUID = me.GetTargetGUID();
			if (targetGUID == 0)
			{
				return null;
			}

			GameObject target = c.ObjectManager.FirstOrDefault(gameObj => gameObj.GUID == targetGUID);

			if (target == null || target.Health == 0 ||
			    c.ControlInterface.remoteControl
				.CGUnit_C__UnitReaction(c.Player.GetAddress(), target.GetAddress()) > UnitReaction.Neutral)
			{
				return null;
			}

			return target;
		}

		public override void Tick()
		{
			Thread.Sleep(1000);
		}

		public override void Dispose()
		{
		}
	}
}
