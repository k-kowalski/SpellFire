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
		private class Warlock : Solution
		{
			private ProdMboxV2 mbox;
			private WarlockPet currentPet;

			public Warlock(Client client, ProdMboxV2 mbox) : base(client)
			{
				this.mbox = mbox;

				me.LuaEventListener.Bind("PET_BAR_UPDATE", args =>
				{
					if (me.GetObjectMgrAndPlayer())
					{
						try
						{
							SetCurrentPet();
						}
						catch (Exception e)
						{
							Console.WriteLine($"Exception happened while processing GetCurrentPetType:\n{e.Message}");
						}
						finally
						{
							Console.WriteLine($"Current Warlock pet type: {currentPet.type}");
						}
					}
				});

				SetCurrentPet();
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

				if (me.Player.IsMounted())
				{
					return;
				}

				LootAround(me);

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

				if (currentPet.type == WarlockPet.PetType.Ranged)
				{
					currentPet.CastSpell("Firebolt");
				}
				else if (currentPet.type == WarlockPet.PetType.Melee)
				{
					// navigate pet to target if target is in tank's(master's) range
					if (mbox.me.Player.GetDistance(target) <= MeleeAttackRange)
					{
						me.ExecLua("PetAttack()");
					}
				}

				if (!me.Player.IsCastingOrChanneling())
				{
					if (me.Player.ManaPct < 10)
					{
						if (me.Player.HealthPct > 80)
						{
							me.CastSpell("Life Tap");
						}
						else
						{
							me.CastSpell("Shoot");
						}
					}

					if (target.Health > (me.Player.Health * 1.5) && !me.HasAura(target, "Immolate", me.Player))
					{
						me.CastSpell("Immolate");
					}
					else if (!me.IsOnCooldown("Chaos Bolt"))
					{
						me.CastSpell("Chaos Bolt");
					}
					else
					{
						me.CastSpell("Incinerate");
					}
				}
			}

			private void SetCurrentPet()
			{
				var abilities = me.ExecLuaAndGetResult(
"abilities = ''; for i=1, NUM_PET_ACTION_SLOTS do abilities = abilities..GetPetActionInfo(i)..',' end",
					"abilities");
				
				currentPet = new WarlockPet(me, abilities);
			}


			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			public class WarlockPet
			{
				private readonly Client owner;
				public readonly PetType type = PetType.None;
				public readonly List<string> abilities = new List<string>();

				public WarlockPet(Client owner, string abilities)
				{
					this.owner = owner;
					if (abilities == null)
					{
						return;
					}


					var absSplit = abilities.Split(',');
					foreach (var ability in absSplit)
					{
						if (!String.IsNullOrEmpty(ability))
						{
							this.abilities.Add(ability);

							switch (ability)
							{
								case "Firebolt": // imp
									type = PetType.Ranged;
									break;
								case "Suffering": // voidwalker
								case "Devour Magic": // felhunter
								case "Lash of Pain": // succubus
								case "Intercept": // felguard
									type = PetType.Melee;
									break;
								default:
									break;
							}
						}
					}
				}

				public enum PetType
				{
					None,
					Melee,
					Ranged
				}

				public void CastSpell(string spell)
				{
					owner.ExecLua($"CastPetAction({abilities.IndexOf(spell) + 1})");
				}
			}
		}
	}
}
