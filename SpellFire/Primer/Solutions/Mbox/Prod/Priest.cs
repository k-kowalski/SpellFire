

//#define SHADOW
#define DISCIPLINE

using SpellFire.Well.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.Prod
{
	public partial class ProdMbox : MultiboxSolution
	{
		private class Priest : Solution
		{
			private ProdMbox mbox;

			private const Int32 PowerWordShield_r14 = 48066;
			private const Int32 WeakenedSoul = 6788;
			private const Int32 Renew_r14 = 48068;

			private readonly List<GameObject> LowHealthPlayers;
			private readonly List<GameObject> MidHealthPlayers;
			private readonly List<GameObject> SteadyHealthPlayers;

			private Specialisation currentSpecialisation = Specialisation.Discipline;

			private static readonly string[] PartyBuffs =
			{
				"Prayer of Fortitude",
				"Prayer of Spirit",
				"Prayer of Shadow Protection"
			};

			private static readonly string[] DisciplineSelfBuffs =
			{
				"Inner Fire",
			};

			private static readonly string[] ShadowSelfBuffs =
			{
				"Inner Fire",
				"Vampiric Embrace",
				"Shadowform"
			};

			public Priest(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;

				var clientsCount = mbox.clients.Count();
				LowHealthPlayers = new List<GameObject>(clientsCount);
				MidHealthPlayers = new List<GameObject>(clientsCount);
				SteadyHealthPlayers = new List<GameObject>(clientsCount);
			}

			public override void Tick()
			{
				Thread.Sleep(ProdMbox.ClientSolutionSleepMs);
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

				if (currentSpecialisation == Specialisation.Discipline)
				{
					#region DISCIPLINE

					if (mbox.buffingAI)
					{
						BuffUp(me, mbox, PartyBuffs, DisciplineSelfBuffs);
					}

					if (me.Player.IsMounted())
					{
						return;
					}


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
							if (player.HealthPct < 55)
							{
								LowHealthPlayers.Add(player);
							}
							else if (player.HealthPct < 85)
							{
								MidHealthPlayers.Add(player);
							}
							else
							{
								SteadyHealthPlayers.Add(player);
							}
						}
					}

					/* process team health data */
					if (!me.Player.IsCastingOrChanneling())
					{
						if (LowHealthPlayers.Count > 2)
						{
							if (!me.IsOnCooldown("Inner Focus"))
							{
								me.CastSpell("Inner Focus");
							}
							me.CastSpell("Prayer of Healing");
						}
						else if (LowHealthPlayers.Count > 0)
						{
							if (ShieldPlayers(LowHealthPlayers))
							{
								return;
							}

							HealLowMidTarget(GetLowestHealthPlayer(LowHealthPlayers));
						}
						else if (MidHealthPlayers.Count > 0)
						{
							if (ShieldPlayers(MidHealthPlayers))
							{
								return;
							}

							HealLowMidTarget(GetLowestHealthPlayer(MidHealthPlayers));
						}
						else if (SteadyHealthPlayers.Count > 0)
						{
							if (ShieldPlayers(SteadyHealthPlayers))
							{
								return;
							}
						}
					}

					LowHealthPlayers.Clear();
					MidHealthPlayers.Clear();
					SteadyHealthPlayers.Clear();

					/* mana mgmt */
					if (me.Player.ManaPct < 45 && me.Player.IsInCombat())
					{
						if (!me.IsOnCooldown("Shadowfiend"))
						{
							me.CastSpell("Shadowfiend");
						}
						else if (me.Player.ManaPct < 25)
						{
							/* command Druid to innervate me, if available */
							var druidClient = mbox.clients.FirstOrDefault(client =>
								client.Player.UnitClass == UnitClass.Druid);
							if (druidClient != null)
							{
								if (druidClient.Player.GetDistance(me.Player) <= HealRange &&
								    druidClient.Player.Health > 0 &&
								    !druidClient.IsOnCooldown("Innervate"))
								{
									druidClient.EnqueuePrioritySpellCast(
										new SpellCast
										{
											Coordinates = null,
											SpellName = "Innervate",
											TargetGUID = me.Player.GUID
										}
									);
									Console.WriteLine("Commanding Druid to Innervate!");
									return;
								}
							}
						}
					}

					/* disci dps rotation */
					Int64[] targetGuids = GetRaidTargetGuids(me);
					GameObject target = SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
					if (target == null)
					{
						return;
					}

					if (me.GetTargetGUID() != target.GUID)
					{
						me.ControlInterface.remoteControl.SelectUnit(target.GUID);
					}

					me.ExecLua("PetAttack()");

					/* disabled for now */
					return;
					if (me.Player.ManaPct < 72)
					{
						return;
					}

					if (me.Player.GetDistance(target) > RangedAttackRange)
					{
						return;
					}
					else
					{
						FaceTowards(me, target);
					}

					me.CastSpellOnGuid("Smite", target.GUID);
					#endregion
				}
				else
				{
					#region SHADOW
					if (mbox.buffingAI)
					{
						BuffUp(me, mbox, PartyBuffs, ShadowSelfBuffs);
					}

					Int64[] targetGuids = GetRaidTargetGuids(me);
					GameObject target = SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
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

					if (!me.Player.IsCastingOrChanneling())
					{
						if (target.Health > ProdMbox.BigHealthThreshold)
						{
							bool isDPUp = me.HasAura(target, "Devouring Plague", me.Player);
							if (isDPUp)
							{
								bool isSWPUp = me.HasAura(target, "Shadow Word: Pain", me.Player);
								if (isSWPUp)
								{
									bool isVTUp = me.HasAura(target, "Vampiric Touch", me.Player);
									if (isVTUp)
									{
										me.CastSpell(!me.IsOnCooldown("Mind Blast") ? "Mind Blast" : "Mind Flay");
									}
									else
									{
										me.CastSpell("Vampiric Touch");
									}
								}
								else
								{
									me.CastSpell("Shadow Word: Pain");
								}
							}
							else
							{
								me.CastSpell("Devouring Plague");
							}
						}
						else
						{

							me.CastSpell(!me.IsOnCooldown("Mind Blast") ? "Mind Blast" : "Mind Flay");
						}
					}
					#endregion
				}
			}

			private GameObject GetLowestHealthPlayer(List<GameObject> players)
			{
				return players.Aggregate((lowest, player) => player.Health < lowest.Health ? player : lowest);
			}

			public override Action<IList<string>> GetCommand(string cmd)
			{
				return cmd switch
				{
					"respec" => new Action<IList<string>>(((args) =>
					{
						if (currentSpecialisation == Specialisation.Discipline)
						{
							currentSpecialisation = Specialisation.Shadow;
						}
						else if (currentSpecialisation == Specialisation.Shadow)
						{
							currentSpecialisation = Specialisation.Discipline;
						}

						Console.WriteLine($"Respecced Priest AI to {currentSpecialisation}");
					})),
					_ => null
				};
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			private bool ShieldPlayers(List<GameObject> players)
			{
				foreach (var player in players)
				{
					if (!me.HasAura(player, PowerWordShield_r14, null) && player.IsInCombat())
					{
						if (!me.HasAura(player, WeakenedSoul, null))
						{
							me.CastSpellOnGuid("Power Word: Shield", player.GUID);
							return true;
						}
					}
				}

				return false;
			}

			private void HealLowMidTarget(GameObject target)
			{
				if (!me.IsOnCooldown("Penance"))
				{
					me.CastSpellOnGuid("Penance", target.GUID);
				}
				else if (!me.IsOnCooldown("Prayer of Mending") && target.IsInCombat())
				{
					me.CastSpellOnGuid("Prayer of Mending", target.GUID);
				}
				else
				{
					me.CastSpellOnGuid("Flash Heal", target.GUID);
				}
			}

			private void HealSteadyTarget(GameObject target)
			{
				if (!me.HasAura(target, PowerWordShield_r14, null) && target.IsInCombat())
				{
					if (!me.HasAura(target, WeakenedSoul, null))
					{
						me.CastSpellOnGuid("Power Word: Shield", target.GUID);
					}
				}
			}

			enum Specialisation
			{
				Discipline,
				Shadow
			}
		}
	}
}
