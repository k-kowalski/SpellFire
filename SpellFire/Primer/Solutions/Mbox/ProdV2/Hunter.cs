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
		private class Hunter : Solution
		{
			private ProdMboxV2 mbox;

			private static readonly string[] PartyBuffs =
			{
			};
			private static readonly string[] SelfBuffs =
			{
				"Trueshot Aura",
				"Aspect of the Dragonhawk",
			};

			public Hunter(Client client, ProdMboxV2 mbox) : base(client)
			{
				this.mbox = mbox;
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

				if (mbox.buffingAI && !me.Player.IsInCombat() && !me.HasAura(me.Player, "Drink"))
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
					CheckPetActive();
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
				else if (me.Player.GetDistance(target) <= MeleeAttackRange)
				{
					me.CastSpell("Disengage");
				}
				else
				{
					FaceTowards(me, target);
				}

				// attack with pet, if target is in tank's range [hunter pet is melee]
				var tank = mbox.me.Player;
				if (tank.GetDistance(target) <= MeleeAttackRange)
				{
					me.ExecLua("PetAttack()");
				}

				if (!me.Player.IsCastingOrChanneling())
				{
					if (!me.Player.IsAutoAttacking())
					{
						me.ExecLua("AttackTarget()");
					}

					if (me.Player.ManaPct < 15)
					{
						if (!me.HasAura(me.Player, "Aspect of the Viper", null) && !me.HasAura(me.Player, "Aspect of the Pack", null))
						{
							me.CastSpell("Aspect of the Viper");
						}
					}

					if (me.Player.ManaPct > 55)
					{
						if (!me.HasAura(me.Player, "Aspect of the Dragonhawk", null) && !me.HasAura(me.Player, "Aspect of the Pack", null))
						{
							me.CastSpell("Aspect of the Dragonhawk");
						}
					}

					var killShot = "Kill Shot";
					if (target.HealthPct < 20 && !me.IsOnCooldown(killShot))
					{
						me.CastSpell(killShot);
					}

					if (target.HealthPct > 80 && !me.HasAura(target, "Hunter's Mark", me.Player))
					{
						me.CastSpell("Hunter's Mark");
					}
					else
					{
						if (target.Health > (me.Player.MaxHealth * 1.5) && !me.HasAura(target, "Serpent Sting", me.Player))
						{
							me.CastSpell("Serpent Sting");
						}
						else
						{
							var chimShot = "Chimera Shot";
							var aimedShot = "Aimed Shot";
							var arcShot = "Arcane Shot";
							if (!me.IsOnCooldown(chimShot))
							{
								me.CastSpell(chimShot);
							}
							else if (!me.IsOnCooldown(aimedShot))
							{
								me.CastSpell(aimedShot);
							}
							else if(!me.IsOnCooldown(arcShot))
							{
								me.CastSpell(arcShot);
							}
							else if (!me.IsOnCooldown("Kill Command"))
							{
								me.CastSpell("Kill Command");
							}
							else
							{
								me.CastSpell("Steady Shot");
							}
						}
					}

					
				}
			}

			private void CheckPetActive()
			{
				var absenceCheck = "texture = GetPetIcon()";
				var res = me.ExecLuaAndGetResult(absenceCheck, "texture");
				if (String.IsNullOrEmpty(res))
				{
					me.CastSpell("Call Pet");
					return;
				}


				var summonedGuid = me.Player.SummonedGuid;
				if (summonedGuid == 0)
				{
					return;
				}
				var pet = me.ObjectManager.FirstOrDefault(obj => obj.GUID == summonedGuid);
				if (pet != null && pet.Health == 0)
				{
					me.CastSpell("Revive Pet");
				}
			}


			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
