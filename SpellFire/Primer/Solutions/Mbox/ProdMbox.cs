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
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Mbox;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox
{
	public class ProdMbox : MultiboxSolution
	{
		private readonly InputMultiplexer inputMultiplexer;
		private bool slavesAI;
		private bool masterAI;
		private IList<Task> slavesTasks = new List<Task>();

		private Action<IList<string>> GetCommand(string cmd)
		{
			return cmd switch
			{
				/* command all Slaves to follow Master */
				"fw" => new Action<IList<string>>(((args) =>
				{
					string masterName = me.ControlInterface.remoteControl.GetUnitName(me.Player.GetAddress());

					string switchArg = args[0];

					foreach (Client slave in Slaves)
					{
						if (switchArg.Equals("st"))
						{
							// break follow
							slave
								.ControlInterface
								.remoteControl
								.FrameScript__Execute($"MoveBackwardStart();MoveBackwardStop()", 0, 0);
						}
						else if (switchArg.Equals("fw"))
						{
							slave
								.ControlInterface
								.remoteControl
								.FrameScript__Execute($"FollowUnit('{masterName}')", 0, 0);
						}
					}
				})),
				/* command selected Slave to cast spell on current Master target */
				"cs" => new Action<IList<string>>(((args) =>
				{
					string casterName = args[0];
					string spellName = args[1];
					Int64 targetGuid = me.GetTargetGUID();

					Client caster = Slaves.FirstOrDefault(c =>
						c.ControlInterface.remoteControl.GetUnitName(c.Player.GetAddress()) == casterName);

					if (caster != null)
					{
						caster.CastSpellOnGuid(spellName, targetGuid);
					}
					else
					{
						Console.WriteLine($"Couldn't find slave: {casterName}.");
					}
				})),
				/* toggles AI(individual behaviour loops) */
				"ta" => new Action<IList<string>>(((args) =>
				{
					string switchArg = args[0];

					if (switchArg.Equals("ma"))
					{
						masterAI = !masterAI;
						string state = masterAI ? "ON" : "OFF";
						Console.WriteLine($"MASTER AI is now {state}.");
					}
					else if (switchArg.Equals("sl"))
					{
						slavesAI = !slavesAI;
						string state = slavesAI ? "ON" : "OFF";
						Console.WriteLine($"SLAVES AI is now {state}.");
					}
				})),
				/* command slaves to interact with Master target */
				"it" => new Action<IList<string>>(((args) =>
				{
					Int64 masterTargetGuid = me.GetTargetGUID();
					if (masterTargetGuid == 0)
					{
						return;
					}

					foreach (Client slave in Slaves)
					{
						GameObject targetObj = slave.ObjectManager.FirstOrDefault(gameObj => gameObj.GUID == masterTargetGuid);

						slave
							.ControlInterface
							.remoteControl
							.InteractUnit(targetObj.GetAddress());
					}
				})),

				_ => new Action<IList<string>>(((args) =>
				{
					Console.WriteLine($"Unrecognized command: {cmd}.");
				})),
			};
		}


		public ProdMbox(IEnumerable<Client> clients) : base(clients)
		{
			inputMultiplexer = new InputMultiplexer(
				me.ControlInterface.hostControl,
				new List<IntPtr>(Slaves.Select(s => s.Process.MainWindowHandle))
				);

			inputMultiplexer.BroadcastKeys.AddRange(new[]
			{
				Keys.Space, // jump
				Keys.L, // quest log
				Keys.B, // backpack
				Keys.Oemplus, // bind '+'
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
						Console.Write($"[{slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress())}] Received foreign invite from {args.Args[1]}.");
					}

				});
				slave.LuaEventListener.Bind("PARTY_MEMBERS_CHANGED", args =>
				{
					slave.ExecLua("StaticPopup_Hide('PARTY_INVITE')");
				});
				slave.LuaEventListener.Bind("CHAT_MSG_WHISPER", args =>
				{
					Console.Write($"[{slave.ControlInterface.remoteControl.GetUnitName(slave.Player.GetAddress())}] Whisper to slave!");
				});
				slave.LuaEventListener.Bind("LOOT_OPENED", args =>
				{
					slave.ExecLua("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end");
				});
				slave.LuaEventListener.Bind("PLAYER_REGEN_ENABLED", args =>
				{
					slave.ExecLua("FollowUnit('party1')");
				});

				/* command executor in game */
				slave.LuaEventListener.Bind("do", args =>
				{
					GetCommand(args.Args[0]).Invoke(new List<string>(args.Args.Skip(1)));
				});

				#region SlaveQuestEvents
				slave.LuaEventListener.Bind("QUEST_DETAIL", args =>
				{
					slave.ExecLua("AcceptQuest()");
				});
				slave.LuaEventListener.Bind("QUEST_ACCEPT_CONFIRM", args =>
				{
					slave.ExecLua("ConfirmAcceptQuest()");
				});
				slave.LuaEventListener.Bind("GOSSIP_SHOW", args =>
				{
					slave.ExecLua(@"
						activeQuests = GetNumGossipActiveQuests()
						for questIndex = 0, activeQuests do
							SelectGossipActiveQuest(questIndex)
						end");
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
			}

			me.LuaEventListener.Bind("LOOT_OPENED", args =>
			{
				me.ExecLua("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end");
			});

			me.LuaEventListener.Bind("PARTY_MEMBERS_CHANGED", args =>
			{
				me.ExecLua(@"
					method, partyMaster, raidMaster = GetLootMethod()
					if method ~= 'master' then
						SetLootMethod('master', 'player')
					end");
			});

			/* command executor in game */
			me.LuaEventListener.Bind("do", args =>
			{
				GetCommand(args.Args[0]).Invoke(new List<string>(args.Args.Skip(1)));
			});

			#region MasterQuestEvents
			/* automatically share acquired quests with slaves */
			me.LuaEventListener.Bind("QUEST_ACCEPTED", args =>
			{
				me.ExecLua($"QuestLogPushQuest({args.Args[0]})");
			});
			me.LuaEventListener.Bind("GOSSIP_SHOW", args =>
			{
				me.ExecLua(@"
						activeQuests = GetNumGossipActiveQuests()
						for questIndex = 0, activeQuests do
							SelectGossipActiveQuest(questIndex)
						end");
			});
			me.LuaEventListener.Bind("QUEST_PROGRESS", args =>
			{
				me.ExecLua("CompleteQuest()");
			});
			me.LuaEventListener.Bind("QUEST_COMPLETE", args =>
			{
				me.ExecLua(@"
						if GetNumQuestChoices() == 0 then
								GetQuestReward(nil)
						end");
			});
			#endregion

			/* turn on follow initially */
			GetCommand("fw").Invoke(new List<string>(new[] {"fw"}));

			this.Active = true;

			AssignRoutines();
		}

		private void AssignRoutines()
		{
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
						Console.WriteLine($"Bound solution: {slave.LaunchSettings.Solution}.");
						slavesTasks.Add(
							Task.Run(() =>
							{
								while (this.Active)
								{
									solution.Tick();
								}
								solution.Dispose();
							})
						);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"Could not launch solution {slave.LaunchSettings.Solution}.");
					Console.WriteLine(e);
				}
			}
		}

		private static readonly string[] PartyBuffs =
		{
			"Blessing of Wisdom",
		};
		private static readonly string[] SelfBuffs =
		{
			"Seal of Righteousness", "Righteous Fury"
		};

		public override void Tick()
		{
			Thread.Sleep(200);
			me.RefreshLastHardwareEvent();

			if (!masterAI)
			{
				return;
			}

			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}

			LootAround(me);
			BuffUp(me, this, PartyBuffs, SelfBuffs, PaladinBuffsForClass);
		}

		private static string PaladinBuffsForClass(UnitClass unitClass)
		{
			return unitClass switch
			{
				UnitClass.Paladin => "Blessing of Kings",
				_ => "Blessing of Wisdom"
			};
		}
		private static void BuffUp(Client self, ProdMbox mbox, string[] partyBuffs, string[] selfBuffs,
			Func<UnitClass, string> classBuffFilter = null)
		{
			foreach (Client client in mbox.clients)
			{
				if (classBuffFilter == null)
				{
					foreach (var partyBuff in partyBuffs)
					{
						if (client.Player.Health > 0 && !self.HasAura(client.Player, partyBuff, null))
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
					if (client.Player.Health > 0 && !self.HasAura(client.Player, partyBuff, null))
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

		public override void Dispose()
		{
			me.LuaEventListener.Dispose();

			foreach (var task in slavesTasks)
			{
				task.Wait();
			}
		}

		private static void LootAround(Client c)
		{
			const float lootingRange = 6f;

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

					c.ControlInterface.remoteControl.CGPlayer_C__ClickToMoveStop(c.Player.GetAddress());
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

		private static GameObject SelectRaidTargetByPriority(Int64[] raidTargetGuids, RaidTarget[] targetPriorities, Client c)
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
				    && gameObj.Health > 0
				    && c.ControlInterface.remoteControl
					    .CGUnit_C__UnitReaction(c.Player.GetAddress(), gameObj.GetAddress()) <= UnitReaction.Neutral)
				{
					return gameObj;
				}
			}

			return null;
		}

		private static void FaceTowards(Client c, GameObject targetObj)
		{
			if (c.Player.IsMoving())
			{
				return;
			}

			Int64 targetGuid = targetObj.GUID;
			Vector3 targetCoords = targetObj.Coordinates;

			float angle = c.Player.Coordinates.AngleBetween(targetCoords);

			c.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
				c.Player.GetAddress(), ClickToMoveType.Face, ref targetGuid, ref targetCoords, angle);
		}

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

		public override void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
		{
			// nothing
		}

		private const float RangedAttackRange = 35f;

		#region Slave

		private class Priest : Solution
		{
			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
				"Power Word: Fortitude",
			};
			private static readonly string[] SelfBuffs =
			{
				"Inner Fire",
			};

			public Priest(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
			}

			public override void Tick()
			{
				Thread.Sleep(200);
				me.RefreshLastHardwareEvent();

				if (!mbox.slavesAI)
				{
					return;
				}

				if (!me.GetObjectMgrAndPlayer())
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Smite")) /* global cooldown check */
				{
					return;
				}

				BuffUp(me, mbox, PartyBuffs, SelfBuffs);
				HealUp();

				Int64[] targetGuids = GetRaidTargetGuids(me);
				GameObject target = SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
				if (target == null)
				{
					return;
				}

				if (me.Player.GetDistance(target) > RangedAttackRange)
				{
					return;
				}

				if (me.GetTargetGUID() != target.GUID)
				{
					me.ControlInterface.remoteControl.SelectUnit(target.GUID);
				}
				FaceTowards(me, target);
				if (!me.Player.IsCastingOrChanneling())
				{
					me.CastSpell(!me.IsOnCooldown("Mind Blast") ? "Mind Blast" : "Mind Flay");
				}
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			private void HealUp()
			{
				foreach (Client client in mbox.clients)
				{
					if (client.Player.HealthPct < 50
					    && (!me.HasAura(client.Player, "Renew", null)))
					{
						me.CastSpellOnGuid("Renew", client.Player.GUID);
						return;
					}
				}
			}
		}

		private class Druid : Solution
		{
			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
				"Mark of the Wild",
				"Thorns",
			};
			private static readonly string[] SelfBuffs =
			{
			};

			public Druid(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
			}

			public override void Tick()
			{
				Thread.Sleep(200);
				me.RefreshLastHardwareEvent();

				if (!mbox.slavesAI)
				{
					return;
				}

				if (!me.GetObjectMgrAndPlayer())
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Wrath")) /* global cooldown check */
				{
					return;
				}

				BuffUp(me, mbox, PartyBuffs, SelfBuffs);

				Int64[] targetGuids = GetRaidTargetGuids(me);
				GameObject target = SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
				if (target == null)
				{
					return;
				}

				if (me.Player.GetDistance(target) > RangedAttackRange)
				{
					return;
				}


				if (me.GetTargetGUID() != target.GUID)
				{
					me.ControlInterface.remoteControl.SelectUnit(target.GUID);
				}
				FaceTowards(me, target);
				me.CastSpell("Wrath");
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}

		private class Shaman : Solution
		{
			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
			};
			private static readonly string[] SelfBuffs =
			{
				"Water Shield",
			};

			public Shaman(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
			}

			public override void Tick()
			{
				Thread.Sleep(200);
				me.RefreshLastHardwareEvent();

				if (!mbox.slavesAI)
				{
					return;
				}

				if (!me.GetObjectMgrAndPlayer())
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Lightning Bolt")) /* global cooldown check */
				{
					return;
				}
				BuffUp(me, mbox, PartyBuffs, SelfBuffs);
				CheckShamanEnchant();

				Int64[] targetGuids = GetRaidTargetGuids(me);
				GameObject target = SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
				if (target == null)
				{
					return;
				}

				if (me.Player.GetDistance(target) > RangedAttackRange)
				{
					return;
				}

				if (me.GetTargetGUID() != target.GUID)
				{
					me.ControlInterface.remoteControl.SelectUnit(target.GUID);
				}

				FaceTowards(me, target);
				me.CastSpell(!me.IsOnCooldown("Earth Shock") ? "Earth Shock" : "Lightning Bolt");
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			private void CheckShamanEnchant()
			{
				var enchCheck = "hasMainHandEnchant, mainHandExpiration, mainHandCharges, hasOffHandEnchant, offHandExpiration, offHandCharges = GetWeaponEnchantInfo()";
				var res = me.ExecLuaAndGetResult(enchCheck, "hasMainHandEnchant");
				if (String.IsNullOrEmpty(res))
				{
					me.CastSpell("Flametongue Weapon");
				}
			}
		}

		private class Mage : Solution
		{
			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
				"Arcane Intellect",
			};
			private static readonly string[] SelfBuffs =
			{
				"Frost Armor",
			};

			private string stockScript;

			public Mage(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
				stockScript = File.ReadAllText("Scripts/Stock.lua");

				me.LuaEventListener.Bind("TRADE_SHOW", args =>
				{
					me.ExecLua(stockScript);
				});

				me.LuaEventListener.Bind("TRADE_PLAYER_ITEM_CHANGED", args =>
				{
					Thread.Sleep(300); /*  it looks that some servers need delay */
					me.ExecLua("AcceptTrade()");
				});
			}

			public override void Tick()
			{
				Thread.Sleep(200);
				me.RefreshLastHardwareEvent();

				if (!mbox.slavesAI)
				{
					return;
				}

				if (!me.GetObjectMgrAndPlayer())
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Fireball")) /* global cooldown check */
				{
					return;
				}
				BuffUp(me, mbox, PartyBuffs, SelfBuffs);

				Int64[] targetGuids = GetRaidTargetGuids(me);
				GameObject ccTarget = SelectRaidTargetByPriority(targetGuids,
					CrowdControlTarget,
					me);
				if (ccTarget != null)
				{
					if (!me.HasAura(ccTarget, "Polymorph", me.Player))
					{
						me.CastSpellOnGuid("Polymorph", ccTarget.GUID);
						return;
					}
				}

				GameObject target = SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
				if (target == null)
				{
					return;
				}

				if (me.Player.GetDistance(target) > RangedAttackRange)
				{
					return;
				}

				if (me.GetTargetGUID() != target.GUID)
				{
					me.ControlInterface.remoteControl.SelectUnit(target.GUID);
				}

				FaceTowards(me, target);
				me.CastSpell(!me.IsOnCooldown("Fire Blast") ? "Fire Blast" : "Fireball");
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}

		#endregion
	}
}
