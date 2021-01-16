using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2
{
	public partial class ProdMboxV2 : MultiboxSolution
	{
		private class PriestV2 : Solution
		{
			private const Int32 WeakenedSoul = 6788;

			private ProdMboxV2 mbox;

			private readonly List<GameObject> LowHealthPlayers;
			private readonly List<GameObject> MidHealthPlayers;
			private readonly List<GameObject> SteadyHealthPlayers;

			private int LowHealthPctThreshold = 55;
			private int MidHealthPctThreshold = 80;


			private static readonly string[] PartyBuffs =
			{
				"Power Word: Fortitude",
			};

			private static readonly string[] SelfBuffs =
			{
				"Inner Fire",
			};

			public PriestV2(Client client, ProdMboxV2 mbox) : base(client)
			{
				this.mbox = mbox;

				var clientsCount = mbox.clients.Count();
				LowHealthPlayers = new List<GameObject>(clientsCount);
				MidHealthPlayers = new List<GameObject>(clientsCount);
				SteadyHealthPlayers = new List<GameObject>(clientsCount);

				me.LuaEventListener.Bind("SPELLS_CHANGED", args => RefreshSpellIds());

			}

			Dictionary<string, int> cachedSpellIds = new Dictionary<string, int>();

			private void RefreshSpellIds()
			{
				lock (cachedSpellIds)
				{
					Console.WriteLine($"[{me.Player.UnitClass}] Refreshing spell ids");
					foreach (var spellName in cachedSpellIds.Keys)
					{
						cachedSpellIds[spellName] = me.GetSpellId(spellName);
					}
				}
			}

			private int GetCachedSpellId(string spellName)
			{
				lock (cachedSpellIds)
				{
					int spellId;
					if (cachedSpellIds.TryGetValue(spellName, out spellId))
					{
						return spellId;
					}
					else
					{
						cachedSpellIds[spellName] = spellId = me.GetSpellId(spellName);
						return spellId;
					}
				}
			}

			public override void Tick()
			{
				Thread.Sleep(ProdMboxV2.ClientSolutionSleepMs);
				me.RefreshLastHardwareEvent();

				if (me.CastPrioritySpell())
				{
					return;
				}

				if (!me.GetObjectMgrAndPlayer())
				{
					return;
				}

				if (!mbox.slavesAI)
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Smite")) /* global cooldown check */
				{
					return;
				}

				if (mbox.buffingAI)
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
				}



				LowHealthPlayers.Clear();
				MidHealthPlayers.Clear();
				SteadyHealthPlayers.Clear();
				foreach (Client client in mbox.clients)
				{
					if (!client.GetObjectMgrAndPlayer())
					{
						continue;
					}

					GameObject player = client.Player;

					if (player.Health > 0)
					{
						if (client.Player.GetDistance(me.Player) > HealRange)
						{
							continue;
						}

						/* assess team health */
						if (player.HealthPct < LowHealthPctThreshold)
						{
							LowHealthPlayers.Add(player);
						}
						else if (player.HealthPct < MidHealthPctThreshold)
						{
							MidHealthPlayers.Add(player);
						}
						else
						{
							SteadyHealthPlayers.Add(player);
						}
					}
				}




				if (!me.Player.IsCastingOrChanneling())
				{
					if (LowHealthPlayers.Count > 0)
					{
						if (!ShieldPlayers(LowHealthPlayers))
						{
							HealLowTarget(GetLowestHealthPlayer(LowHealthPlayers));
						}
					}
					else if (MidHealthPlayers.Count > 0)
					{
						if (!ShieldPlayers(MidHealthPlayers))
						{
							HealLowTarget(GetLowestHealthPlayer(MidHealthPlayers));
						}
					}
				}

				if (LowHealthPlayers.Any() || MidHealthPlayers.Any())
				{
					return;
				}


				if (me.Player.ManaPct < 90)
				{
					return;
				}

				Int64[] targetGuids = GetRaidTargetGuids(me);
				GameObject target = mbox.SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
				if (target == null)
				{
					return;
				}

				if (me.GetTargetGUID() != target.GUID)
				{
					me.ControlInterface.remoteControl.SelectUnit(target.GUID);
				}

				if (me.Player.GetDistance(target) > RangedAttackRange)
				{
					return;
				}
				else
				{
					FaceTowards(me, target);
				}


				me.ExecLua("PetAttack()");
				if (!me.Player.IsCastingOrChanneling())
				{
					me.CastSpell("Smite");
				}
			}

			private GameObject GetLowestHealthPlayer(List<GameObject> players)
			{
				return players.Aggregate((lowest, player) => player.Health < lowest.Health ? player : lowest);
			}

			private void HealMidTarget(GameObject target)
			{
				if (!me.HasAura(target, GetCachedSpellId("Renew"), null))
				{
					me.CastSpellOnGuid(GetCachedSpellId("Renew"), target.GUID);
				}
			}

			private void HealLowTarget(GameObject target)
			{
				if (!me.HasAura(target, GetCachedSpellId("Renew"), null))
				{
					me.CastSpellOnGuid(GetCachedSpellId("Renew"), target.GUID);
				}
				else
				{
					me.CastSpellOnGuid(GetCachedSpellId("Flash Heal"), target.GUID);
				}
			}

			private bool ShieldPlayers(List<GameObject> players)
			{
				foreach (var player in players)
				{
					var pws = GetCachedSpellId("Power Word: Shield");
					if (!me.HasAura(player, pws, null) && player.IsInCombat())
					{
						if (!me.HasAura(player, WeakenedSoul, null))
						{
							me.CastSpellOnGuid(pws, player.GUID);
							return true;
						}
					}
				}

				return false;
			}


			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
