using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Primer.Gui;
using SpellFire.Primer.Solutions.Mbox.ProdV2.Behaviours;
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Mbox;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2
{
	public partial class ProdMboxV2 : MultiboxSolution
	{
		private readonly InputMultiplexer inputMultiplexer;
		private IList<Task> slavesTasks = new List<Task>();
		private IList<Solution> slavesSolutions = new List<Solution>();
		private Solution masterSolution;

		private GroupManager gm;
		private BehaviourTree currentBehaviour;

		private static readonly RaidTarget[] AttackPriorities =
		{
			RaidTarget.Skull,
			RaidTarget.Cross,
			RaidTarget.Square,
		};

		private static readonly RaidTarget[] CrowdControlTarget =
		{
			RaidTarget.Diamond,
		};

		public const float HealRange = 40f;
		public const float RangedAttackRange = 35f;
		public const float MeleeAttackRange = 5f;

		private const int MaxSlaveFPS = 20;
		private const int ClientSolutionSleepMs = 200;

		internal bool slavesAI;
		internal bool masterAI;
		internal bool buffingAI;
		internal bool radarOn;
		internal bool complexRotation;
		internal volatile bool followOn;

		private static long fixateTargetGuid;

		private readonly string UtilScript = File.ReadAllText("Scripts/Util.lua");


		private GameObject followUnit;
		private const float followDistance = 5.5f;
		private const float delayDistance = 2.5f;


		private Action<Client, IList<string>> GetCommand(string cmd)
		{
			var command = masterSolution.GetCommand(cmd);
			if (command != null)
				return (self, args) => command.Invoke(args);

			foreach (var slaveSolution in slavesSolutions)
			{
				command = slaveSolution.GetCommand(cmd);
				if (command != null)
					return (self, args) => command.Invoke(args);
			}

			return cmd switch
			{
				/* behaviours commands */
				"bhv" => new Action<Client, IList<string>>((self, args) =>
				{
					if (currentBehaviour == null)
					{
						currentBehaviour = new TrialOfTheChampion(this);
					}
					else
					{
						currentBehaviour.Cmd(args);
					}
				}),
				/* set client's cvar */
				"cvar" => new Action<Client, IList<string>>(((self, args) =>
				{
					IEnumerable<Client> clients = null;
					string charName = args[0];
					string cvarName = args[1];
					string cvarValue = args[2];

					if (charName == "*")
					{
						clients = base.clients;
					}
					else if (charName == "<slaves>")
					{
						clients = Slaves;
					}
					else
					{
						Func<Client, bool> q = null;

						var targetGuid = me.Memory.ReadInt64(IntPtr.Zero + Offset.MouseoverGUID);
						if (targetGuid == 0)
						{
							q = c => c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == charName;
						}
						else
						{
							q = c => c.Player.GUID == targetGuid;
						}

						clients = base.clients.Where(q);
					}

					foreach (var client in clients)
					{
						client.ExecLua($"SetCVar('{cvarName}', {cvarValue})");
					}
				})),
				/* command all to exit vehicle */
				"ev" => new Action<Client, IList<string>>((self, args) =>
				{
					foreach (Client client in clients)
					{
						client.ExecLua("VehicleExit()");
					}
				}),
				/* command all Slaves to follow target */
				"fw" => new Action<Client, IList<string>>((self, args) =>
				{
					if (!followOn)
					{
						// about to change to follow-on, set appropriate follow target
						if (args.Count > 0 && args[0] == "mo")
						{
							var targetGuid = self.Memory.ReadInt64(IntPtr.Zero + Offset.MouseoverGUID);
							if (targetGuid != 0)
							{
								followUnit = self.ObjectManager.First(obj => obj.GUID == targetGuid);
								self.ExecLua($"print('Follow ordered on guid: {targetGuid}')");
							}
							else
							{
								self.ExecLua("print('No target selected')");
							}
						}
						else
						{
							// master player as default follow unit
							followUnit = me.Player;
						}
					}

					followOn = !followOn;
					string state = followOn ? "ON" : "OFF";
					string col = followOn ? "00FF00" : "FF0000";
					me.ExecLua($"print('FOLLOW is now |cff{col}{state}|r')");
				}),
				/* command selected Slave to cast spell on current Master target */
				"cs" => new Action<Client, IList<string>>(((self, args) =>
				{
					string casterName = args[0];
					string spellName = args[1];

					Int64 targetGuid = 0;
					if (args.Count > 2 && args[2] == "mo") // optional mouseover
					{
						targetGuid = me.Memory.ReadInt64(IntPtr.Zero + Offset.MouseoverGUID);
					}
					else
					{
						targetGuid = me.GetTargetGUID();
					}

					Client caster = clients.FirstOrDefault(c =>
						c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == casterName);

					if (caster != null)
					{
						caster.EnqueuePrioritySpellCast(
							new SpellCast
							{
								Coordinates = null,
								SpellName = spellName,
								TargetGUID = targetGuid
							}
						);
					}
					else
					{
						Console.WriteLine($"Couldn't find slave: {casterName}.");
					}
				})),
				/* use item */
				"ui" => new Action<Client, IList<string>>(((self, args) =>
				{
					IEnumerable<Client> users = null;
					string location = args[0];
					string userName = args[1];
					string itemName = args[2];

					if (userName == "*")
					{
						users = clients;
					}
					else if (userName == "<slaves>")
					{
						users = Slaves;
					}
					else
					{
						Func<Client, bool> q = null;

						var targetGuid = me.Memory.ReadInt64(IntPtr.Zero + Offset.MouseoverGUID);
						if (targetGuid == 0)
						{
							q = c => c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == userName;
						}
						else
						{
							q = c => c.Player.GUID == targetGuid;
						}

						users = clients.Where(q);
					}

					foreach (var user in users)
					{
						if (user != null)
						{
							user.ExecLua(UtilScript);
							if (location == "inv")
							{
								user.ExecLua(
									$"filter = function(itemName) return itemName == \"{itemName}\" end;" +
									$"sfUseInventoryItem(filter)");
							}
							else if (location == "bag")
							{
								user.ExecLua(
									$"filter = function(itemName) return itemName == \"{itemName}\" end;" +
									$"sfUseBagItem(filter)");
							}
						}
						else
						{
							Console.WriteLine($"Couldn't find slave: {userName}.");
						}
					}
				})),
				/* cast terrain-targetable spell */
				"ctts" => new Action<Client, IList<string>>(((self, args) =>
				{
					string casterName = args[0];
					string spellName = args[1];

					Int64 targetGuid = 0;
					if (args.Count > 2 && args[2] == "mo") // optional mouseover
					{
						targetGuid = me.Memory.ReadInt64(IntPtr.Zero + Offset.MouseoverGUID);
					}
					else
					{
						targetGuid = me.GetTargetGUID();
					}

					if (targetGuid == 0)
					{
						return;
					}

					Client caster = Slaves.FirstOrDefault(c =>
						c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == casterName);

					if (caster != null)
					{
						GameObject targetObject = caster.ObjectManager.FirstOrDefault(obj => obj.GUID == targetGuid);
						if (targetObject != null)
						{
							caster.EnqueuePrioritySpellCast(
								new SpellCast {
									Coordinates = targetObject.Coordinates - Vector3.Random(), /* randomize location a little */
									SpellName = spellName,
									TargetGUID = targetGuid
								}
							);
						}
						else
						{
							Console.WriteLine($"Target not found.");
						}
					}
					else
					{
						Console.WriteLine($"Couldn't find slave: {casterName}.");
					}
				})),
				/* toggles AI(individual behaviour loops) */
				"ta" => new Action<Client, IList<string>>(((self, args) =>
				{
					string switchArg = args[0];

					if (switchArg.Equals("ma"))
					{
						masterAI = !masterAI;
						string state = masterAI ? "ON" : "OFF";
						Console.WriteLine($"MASTER AI is now {state}.");
						me.ExecLua($"SetMasterStatus('{state}')");
					}
					else if (switchArg.Equals("sl"))
					{
						slavesAI = !slavesAI;
						string state = slavesAI ? "ON" : "OFF";
						Console.WriteLine($"SLAVES AI is now {state}.");
						me.ExecLua($"SetSlavesStatus('{state}')");
					}
					else if (switchArg.Equals("bu"))
					{
						buffingAI = !buffingAI;
						string state = buffingAI ? "ON" : "OFF";
						Console.WriteLine($"BUFFING AI is now {state}.");
						me.ExecLua($"SetBuffingStatus('{state}')");
					}
					else if (switchArg.Equals("ra"))
					{
						radarOn = !radarOn;
						string state = radarOn ? "ON" : "OFF";
						Console.WriteLine($"RADAR is now {state}.");
						me.ExecLua($"SetRadarStatus('{state}')");
					}
					else if (switchArg.Equals("in"))
					{
						inputMultiplexer.ConditionalBroadcastOn = !inputMultiplexer.ConditionalBroadcastOn;
						string state = inputMultiplexer.ConditionalBroadcastOn ? "ON" : "OFF";
						Console.WriteLine($"EXTRA INPUT BROADCAST is now {state}.");
						me.ExecLua($"SetExInputBroadcastStatus('{state}')");
					}
				})),
				/* command slaves to interact with Master mouseover target */
				"it" => new Action<Client, IList<string>>(((self, args) =>
				{
					Int64 masterTargetGuid = me.Memory.ReadInt64(IntPtr.Zero + Offset.MouseoverGUID);
					if (masterTargetGuid == 0)
					{
						return;
					}

					foreach (Client slave in Slaves)
					{
						slave.Memory.Write(IntPtr.Zero + Offset.MouseoverGUID, BitConverter.GetBytes(masterTargetGuid));
						slave.ExecLua("InteractUnit('mouseover')");
					}
				})),
				/* exit all slaves */
				"ex" => new Action<Client, IList<string>>(((self, args) =>
				{
					foreach (Client slave in Slaves)
					{
						slave.ExecLua($"Quit()");
					}
				})),
				/* click static popup */
				"stat" => new Action<Client, IList<string>>(((self, args) =>
				{
					foreach (Client client in Slaves)
					{
						// arg is button number
						client.ExecLua($"StaticPopup1Button{args[0]}:Click()");
					}
				})),
				/* fixate on target */
				"fix" => new Action<Client, IList<string>>(((self, args) =>
				{
					var targetGuid = me.GetTargetGUID();
					if (targetGuid == 0)
					{
						return;
					}

					fixateTargetGuid = targetGuid;

					Console.WriteLine($"Fixated on {fixateTargetGuid}");
				})),
				/* toggles simple rotation */
				"rot" => new Action<Client, IList<string>>(((self, args) =>
				{
					complexRotation = !complexRotation;
					string state = complexRotation ? "ON" : "OFF";
					Console.WriteLine($"Complex Rotation is now {state}.");
					me.ExecLua($"SetRotationStatus('{state}')");
				})),
				"frm" => new Action<Client, IList<string>>(((self, args) =>
				{
					const float magnitude = 5f;
					var formation = new[] {
						new Vector3(-magnitude, -magnitude, 0f),
						new Vector3(magnitude, magnitude, 0f),
						new Vector3(-magnitude, magnitude, 0f),
						new Vector3(magnitude, -magnitude, 0f),
					};

					var formationIndex = 0;
					foreach (var slave in Slaves)
					{
						if (slave.GetObjectMgrAndPlayer())
						{
							Int64 _dummyGuid = 0;
							Vector3 destination = slave.Player.Coordinates - formation[formationIndex];
							slave
								.ControlInterface
								.remoteControl
								.CGPlayer_C__ClickToMove(
									slave.Player.GetAddress(), ClickToMoveType.Move, ref _dummyGuid, ref destination, 1f);

							formationIndex++;
						}
					}
				})),
				/* bring to foreground selected client game window */
				"fg" => new Action<Client, IList<string>>(((self, args) =>
				{
					Client client;
					if (args.Count > 0 && args[0] == "mo") // optional mouseover
					{
						// use tooltip text
						// this enables range-unlimited switching
						var getUnitNameFromMouseoverTooltipScript = "if GameTooltip:NumLines() > 0 then name = _G['GameTooltipTextLeft1']:GetText() else name = nil end";
						var name = self.ExecLuaAndGetResult(getUnitNameFromMouseoverTooltipScript, "name");
						client = clients.FirstOrDefault(c => c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == name);

						if (client == null)
						{
							Console.WriteLine($"Couldn't find client with player name {name}.");
						}
					}
					else
					{
						var targetGuid = self.GetTargetGUID();
						client = clients.FirstOrDefault(c => c.Player.GUID == targetGuid);

						if (client == null)
						{
							Console.WriteLine($"Couldn't find client with player guid {targetGuid}.");
						}
					}

					if (client != null)
					{
						self.ControlInterface.remoteControl.YieldWindowFocus(client.Process.MainWindowHandle);
					}


				})),

				_ => new Action<Client, IList<string>>(((self, args) =>
				{
					Console.WriteLine($"Unrecognized command: {cmd}.");
				})),
			};
		}


		public ProdMboxV2(IEnumerable<Client> clients) : base(clients)
		{
			inputMultiplexer = new InputMultiplexer(
				me.ControlInterface.remoteControl,
				new List<IntPtr>(Slaves.Select(s => s.Process.MainWindowHandle))
				);

			inputMultiplexer.BroadcastKeys.AddRange(new[]
			{
				Keys.Oem5, // fwd slash
				Keys.Oem3, // tilde
				Keys.Oemcomma,
			});

			inputMultiplexer.ConditionalBroadcastKeys.AddRange(new[]
			{
				Keys.Space,
				Keys.W,
				Keys.S,
				Keys.D1,
				Keys.D2,
				Keys.D3,
				Keys.D4,
				Keys.D5,
			});

			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}

			string masterPlayerName = me.ControlInterface.remoteControl.GetUnitName(me.Player.GetAddress());
			SystemWin32.SendMessage(
				me.Process.MainWindowHandle,
				SystemWin32.WM_SETTEXT, IntPtr.Zero,
				$"@@@@[{masterPlayerName}]@@@@ WoW");

			foreach (Client slave in Slaves)
			{
				slave.GetObjectMgrAndPlayer();

				string slavePlayerName = slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress());

				SystemWin32.SendMessage(
					slave.Process.MainWindowHandle,
					SystemWin32.WM_SETTEXT, IntPtr.Zero,
					$"[{slavePlayerName}] WoW");

				/* set event listeners */
				slave.LuaEventListener.Bind("PARTY_INVITE_REQUEST", args =>
				{
					if (args.Args[0] == me.ControlInterface.remoteControl.GetUnitName(me.Player.GetAddress()))
					{
						slave.ExecLua("AcceptGroup()");
					}
					else
					{
						Console.WriteLine($"[{slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress())}] Received foreign invite from {args.Args[1]}.");
					}

				});
				slave.LuaEventListener.Bind("PARTY_MEMBERS_CHANGED", args =>
				{
					slave.ExecLua("StaticPopup_Hide('PARTY_INVITE')");
				});
				slave.LuaEventListener.Bind("CHAT_MSG_WHISPER", args =>
				{
					if (slave.GetObjectMgrAndPlayer())
					{
						SFUtil.PlayNotificationSound();
						Console.WriteLine($"[{slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress())}] Whisper to slave!");
					}
				});
				slave.LuaEventListener.Bind("LOOT_OPENED", args =>
				{
					slave.ExecLua("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end");
				});
				slave.LuaEventListener.Bind("START_LOOT_ROLL", args =>
				{
					var rollId = args.Args[0];

					/* Disenchant if available, fallback to Greed */
					slave.ExecLua($"RollOnLoot({rollId}, {(int)RollType.Disenchant})");
					slave.ExecLua($"RollOnLoot({rollId}, {(int)RollType.Greed})");
				});
				slave.LuaEventListener.Bind("CONFIRM_LOOT_ROLL", args =>
				{
					var rollId = args.Args[0];

					/* Disenchant if available, fallback to Greed */
					slave.ExecLua($"ConfirmLootRoll({rollId}, {(int)RollType.Disenchant})");
					slave.ExecLua($"ConfirmLootRoll({rollId}, {(int)RollType.Greed})");
				});
				slave.LuaEventListener.Bind("PLAYER_REGEN_ENABLED", args =>
				{
//					/* follow master if all out of combat */
//					if ( ! clients.Where(c => c.Player != null).Any(c => c.Player.IsInCombat()))
//					{
//						if (!followOn)
//						{
//							GetCommand("fw").Invoke(slave, new List<string>());
//						}
//					}
				});
				slave.LuaEventListener.Bind("RESURRECT_REQUEST", args =>
				{
					slave.ExecLua("AcceptResurrect()");
				});
				slave.LuaEventListener.Bind("LFG_ROLE_CHECK_SHOW", args =>
				{
					slave.ExecLua("CompleteLFGRoleCheck(true)");
				});
				slave.LuaEventListener.Bind("LFG_PROPOSAL_SHOW", args =>
				{
					slave.ExecLua("LFDDungeonReadyDialogEnterDungeonButton:Click()");
				});

				/* command executor in game */
				slave.LuaEventListener.Bind("do", args =>
				{
					GetCommand(args.Args[0]).Invoke(slave, new List<string>(args.Args.Skip(1)));
				});

				#region SlaveQuestEvents
				slave.LuaEventListener.Bind("QUEST_DETAIL", args =>
				{
					slave.ExecLua("AcceptQuest()");
				});
				slave.LuaEventListener.Bind("QUEST_ACCEPT_CONFIRM", args =>
				{
					slave.ExecLua("StaticPopup1Button1:Click()");
				});
				slave.LuaEventListener.Bind("GOSSIP_SHOW", args =>
				{
					if (slavesAI)
					{
						slave.ExecLua(@"
						activeQuests = GetNumGossipActiveQuests()
						for questIndex = 0, activeQuests do
							SelectGossipActiveQuest(questIndex)
						end");
					}
				});
				slave.LuaEventListener.Bind("QUEST_PROGRESS", args =>
				{
					slave.ExecLua("CompleteQuest()");
				});
				slave.LuaEventListener.Bind("QUEST_COMPLETE", args =>
				{
					slave.ExecLua(@"
						if GetNumQuestChoices() == 0 then
								GetQuestReward(nil)
						end");
				});
				#endregion

				/* invite slaves to party */
				me.ExecLua($"InviteUnit('{slavePlayerName}')");

				/* fps throttle */
				slave.ExecLua($"SetCVar('maxfps', {MaxSlaveFPS})");
			}

			me.LuaEventListener.Bind("PLAYER_REGEN_ENABLED", args =>
			{
				/* follow master if all out of combat */
//				if (!clients.Where(c => c.Player != null).Any(c => c.Player.IsInCombat()))
//				{
//					if (!followOn)
//					{
//						GetCommand("fw").Invoke(me, new List<string>());
//					}
//				}
			});
			me.LuaEventListener.Bind("RESURRECT_REQUEST", args =>
			{
				me.ExecLua("AcceptResurrect()");
			});
			me.LuaEventListener.Bind("LFG_PROPOSAL_SHOW", args =>
			{
				me.ExecLua("LFDDungeonReadyDialogEnterDungeonButton:Click()");
			});

			me.LuaEventListener.Bind("LOOT_OPENED", args =>
			{
				me.ExecLua("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end");
			});

			/* command executor in game */
			me.LuaEventListener.Bind("do", args =>
			{
				GetCommand(args.Args[0]).Invoke(me, new List<string>(args.Args.Skip(1)));
			});

			#region MasterQuestEvents
			/* automatically share acquired quests with slaves */
			me.LuaEventListener.Bind("QUEST_ACCEPTED", args =>
			{
				me.ExecLua($"QuestLogPushQuest({args.Args[0]})");
			});
			#endregion


			this.Active = true;
			AssignRoutines();


			gm = new GroupManager(this);

			// focus master
			foreach (var slave in Slaves)
			{
				slave.ControlInterface.remoteControl.YieldWindowFocus(me.Process.MainWindowHandle);
			}
		}

		public override void Tick()
		{
			Thread.Sleep(10);

			masterSolution?.Tick();

			if (masterAI)
			{
				gm.ManageGroup();
			}

			FollowTarget();
			lastFrameFollowOn = followOn;


			if (currentBehaviour != null)
			{
				var res = currentBehaviour.Eval();
				if (res == BTStatus.Success)
				{
					Console.WriteLine("Behaviour successfully concluded.");
					currentBehaviour = null;
				}
			}
		}

		private bool lastFrameFollowOn;

		private void FollowTarget()
		{
			IEnumerable<Client> eligibleSlaves;
			int masterMapId;
			if (!followOn)
			{
				if (lastFrameFollowOn)
				{
					// just turned off follow, so stop movement
					masterMapId = me.Memory.ReadInt32(IntPtr.Zero + Offset.MapId);
					eligibleSlaves = Slaves
						.Where(c => c.Memory.ReadInt32(IntPtr.Zero + Offset.MapId) == masterMapId && c.Player != null);
					foreach (var slave in eligibleSlaves)
					{
						if (slave.Player.IsMoving())
						{
							slave.ExecLua("MoveForwardStart();MoveForwardStop()");
						}
					}
				}

				return;
			}
			else
			{
				if (me.Player == null)
				{
					followOn = false;
				}
			}


			if (followUnit == null)
			{
				return;
			}

			var targetCoordinates = followUnit.Coordinates;
			masterMapId = me.Memory.ReadInt32(IntPtr.Zero + Offset.MapId);
			eligibleSlaves = Slaves
				.Where(c => c.Memory.ReadInt32(IntPtr.Zero + Offset.MapId) == masterMapId && c.Player != null);
			long _guid = 0;
			foreach (var slave in eligibleSlaves)
			{
				var slaveCoords = slave.Player.Coordinates;
				var diff = (targetCoordinates - slaveCoords);
				var distance = diff.Length();

				if (distance > (followDistance + delayDistance))
				{
					var adjusted = ((diff * followDistance) / distance);
					var target = targetCoordinates - adjusted;

					slave
						.ControlInterface
						.remoteControl
						.CGPlayer_C__ClickToMove(slave.Player.GetAddress(), ClickToMoveType.Move, ref _guid, ref target, 1f);
				}
				else
				{
					float angle = slaveCoords.AngleBetween(targetCoordinates);
					if (!SFUtil.IsFacing(slave.Player.Rotation, angle) && !slave.Player.IsMoving())
					{
						slave.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
							slave.Player.GetAddress(), ClickToMoveType.Face, ref _guid, ref slaveCoords, angle);
					}
				}
			}
		}

		public override void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
		{
			masterSolution?.RenderRadar(radarCanvas, radarBackBuffer);
		}

		public override void Dispose()
		{
			foreach (var solution in slavesSolutions)
			{
				solution.Stop();
			}

			foreach (var task in slavesTasks)
			{
				task.Wait();
			}
		}

		private void AssignRoutines()
		{
			/* master */
			try
			{
				if (me.LaunchSettings.Solution == null)
				{
					goto AssignSlaves;
				}

				Type solutionType = Type.GetType(me.LaunchSettings.Solution);

				masterSolution = Activator.CreateInstance(solutionType, me, this) as Solution;
				if (masterSolution != null)
				{
					Console.WriteLine($"Bound Master solution: {me.LaunchSettings.Solution}.");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Could not launch Master solution {me.LaunchSettings.Solution}.");
				Console.WriteLine(e);
			}

			AssignSlaves:
			foreach (Client slave in Slaves)
			{
				try
				{
					if (slave.LaunchSettings.Solution == null)
					{
						continue;
					}

					Type solutionType = Type.GetType(slave.LaunchSettings.Solution);

					if (Activator.CreateInstance(solutionType, slave, this) is Solution solution)
					{
						Console.WriteLine($"Bound Slave solution: {slave.LaunchSettings.Solution}.");

						slavesTasks.Add(
							Task.Run(() =>
							{
								try
								{
									while (this.Active)
									{
										solution.Tick();
									}
								}
								catch (Exception e)
								{
									Console.WriteLine(e);
								}
								solution.Dispose();
								Console.WriteLine($"Solution ended: {slave.LaunchSettings.Solution}.");
							})
						);

						slavesSolutions.Add(solution);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"Could not launch Slave solution {slave.LaunchSettings.Solution}.");
					Console.WriteLine(e);
				}
			}
		}

		private static void BuffUp(Client self, ProdMboxV2 mbox, string[] partyBuffs, string[] selfBuffs,
			Func<UnitClass, string> classBuffFilter = null)
		{
			foreach (Client client in mbox.clients)
			{
				if (classBuffFilter == null)
				{
					foreach (var partyBuff in partyBuffs)
					{
						if (client.GetObjectMgrAndPlayer() &&
							client.Player.Health > 0 &&
						    !self.HasAura(client.Player, partyBuff, null) &&
							!client.IsOnCooldown(partyBuff))
						{
							if (client.Player.GetDistance(self.Player) < RangedAttackRange/* use RAR for buff range */)
							{
								self.CastSpellOnGuid(partyBuff, client.Player.GUID);
								return;
							}
						}
					}
				}
				else
				{
					var partyBuff = classBuffFilter(client.Player.UnitClass);
					if (client.GetObjectMgrAndPlayer() &&
						client.Player.Health > 0 &&
					    !self.HasAura(client.Player, partyBuff, null) &&
						!client.IsOnCooldown(partyBuff))
					{
						if (client.Player.GetDistance(self.Player) < RangedAttackRange/* use RAR for buff range */)
						{
							self.CastSpellOnGuid(partyBuff, client.Player.GUID);
							return;
						}
					}
				}
			}

			foreach (var selfBuff in selfBuffs)
			{
				if (self.Player.Health > 0 && !self.HasAura(self.Player, selfBuff, null))
				{
					self.CastSpell(selfBuff);
					return;
				}
			}
		}

		private static void LootAround(Client c)
		{
			const float lootingRange = 8f;

			if (c.Player.IsInCombat())
			{
				return;
			}

			IEnumerable<GameObject> lootables =
				c.ObjectManager.Where(gameObj => gameObj.Type == GameObjectType.Unit && gameObj.IsLootable());

			float minDistance = Single.MaxValue;
			GameObject closestLootableUnit = null;

			foreach (GameObject lootable in lootables)
			{
				float distance = c.Player.GetDistance(lootable);
				if (distance < minDistance)
				{
					minDistance = distance;
					closestLootableUnit = lootable;
				}
			}

			if (closestLootableUnit != null)
			{
				//Console.WriteLine($"[{DateTime.Now}] closest target away {minDistance}y, checked {lootables.Count()} lootable/s.");

				if (minDistance < lootingRange && (!c.Player.IsMoving()) && (!c.Player.IsCastingOrChanneling()))
				{
					//Console.WriteLine($"[{DateTime.Now}] interacting");

					c.ControlInterface.remoteControl.InteractUnit(closestLootableUnit.GetAddress());

					/*
					 * one case for this are corpses that are marked lootable
					 * but in fact loot inside is not ours(ie. other people quest items)
					 * in this event bot would be hammering fruitless looting, which could look unnatural
					 *
					 * other than above is
					 * after successful looting rest a little longer
					 * so it will be more believable
					 */
					Thread.Sleep(100);
				}
			}
		}

		private static Int64[] GetRaidTargetGuids(Client c)
		{
			const int raidTargetsMax = 8;

			byte[] raidTargetsBytes = c.Memory.Read(IntPtr.Zero + Offset.RaidTargets, raidTargetsMax * sizeof(Int64));
			Int64[] targetGuids = new Int64[raidTargetsMax];

			for (int i = 0; i < raidTargetsMax; i++)
			{
				targetGuids[i] = BitConverter.ToInt64(raidTargetsBytes, i * sizeof(Int64));
			}

			return targetGuids;
		}

		private GameObject SelectRaidTargetByPriority(Int64[] raidTargetGuids, RaidTarget[] targetPriorities, Client c)
		{
			foreach (var marker in targetPriorities)
			{
				Int64 targetGuid = raidTargetGuids[(int)marker];
				if (targetGuid == 0)
				{
					continue;
				}

				GameObject gameObj = c.ObjectManager.FirstOrDefault(obj => obj.GUID == targetGuid);
				if (gameObj != null
					&& gameObj.Health > 0)
				{
					if (fixateTargetGuid != 0)
					{
						Console.WriteLine("Clearing fixate target. Valid mark detected");
						fixateTargetGuid = 0;
					}
					return gameObj;
				}
			}

			if (fixateTargetGuid != 0)
			{
				GameObject gameObj = c.ObjectManager.FirstOrDefault(obj => obj.GUID == fixateTargetGuid);
				if (gameObj != null
				    && gameObj.Health > 0
				    && c.ControlInterface.remoteControl
					    .CGUnit_C__UnitReaction(c.Player.GetAddress(), gameObj.GetAddress()) <= UnitReaction.Neutral)
				{
					return gameObj;
				}
			}

			foreach (var targetGuid in gm.GroupTargetGuids)
			{
				GameObject gameObj = c.ObjectManager.FirstOrDefault(obj => obj.GUID == targetGuid);
				if (gameObj != null
				    && gameObj.Health > 0)
				{
					return gameObj;
				}
			}

			return null;
		}

		private List<GameObject> SelectAllRaidTargetsByPriority(Int64[] raidTargetGuids, RaidTarget[] targetPriorities, Client c)
		{
			var targets = new List<GameObject>();
			foreach (var marker in targetPriorities)
			{
				Int64 targetGuid = raidTargetGuids[(int)marker];
				if (targetGuid == 0)
				{
					continue;
				}

				GameObject gameObj = c.ObjectManager.FirstOrDefault(obj => obj.GUID == targetGuid);
				if (gameObj != null
				    && gameObj.Health > 0)
				{
					if (fixateTargetGuid != 0)
					{
						Console.WriteLine("Clearing fixate target. Valid mark detected");
						fixateTargetGuid = 0;
					}
					targets.Add(gameObj);
				}
			}

			if (targets.Any())
			{
				return targets;
			}

			if (fixateTargetGuid != 0)
			{
				GameObject gameObj = c.ObjectManager.FirstOrDefault(obj => obj.GUID == fixateTargetGuid);
				if (gameObj != null
				    && gameObj.Health > 0
				    && c.ControlInterface.remoteControl
					    .CGUnit_C__UnitReaction(c.Player.GetAddress(), gameObj.GetAddress()) <= UnitReaction.Neutral)
				{
					targets.Add(gameObj);
					return targets;
				}
			}

			foreach (var targetGuid in gm.GroupTargetGuids)
			{
				GameObject gameObj = c.ObjectManager.FirstOrDefault(obj => obj.GUID == targetGuid);
				if (gameObj != null
				    && gameObj.Health > 0)
				{
					targets.Add(gameObj);
				}
			}

			return targets;
		}

		private static void FaceTowards(Client c, GameObject targetObj)
		{
			if (c.Player.IsMoving())
			{
				return;
			}


			long _Guid = 0;
			Vector3 playerCoords = c.Player.Coordinates;


			float angle = playerCoords.AngleBetween(targetObj.Coordinates);
			if (!SFUtil.IsFacing(c.Player.Rotation, angle))
			{
				c.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
					c.Player.GetAddress(), ClickToMoveType.Face, ref _Guid, ref playerCoords, angle);
			}
		}
	}
}
