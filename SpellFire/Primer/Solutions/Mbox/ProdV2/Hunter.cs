using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.Prod
{
	public partial class ProdMboxV2 : MultiboxSolution
	{
		private class Hunter : Solution
		{
			private ProdMboxV2 mbox;

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

				LootAround(me);

				if (me.IsOnCooldown("Serpent Sting")) /* global cooldown check */
				{
					return;
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

					var aimedShot = "Aimed Shot";
					if (!me.IsOnCooldown(aimedShot))
					{
						me.CastSpell(aimedShot);
					}

					var arcShot = "Arcane Shot";
					if (!me.IsOnCooldown(arcShot))
					{
						me.CastSpell(arcShot);
					}

					//					bool isSerpentStingUp = me.HasAuraEx(target, "Serpent Sting", me.Player);
					//					if(!isSerpentStingUp)
					//					{
					//						me.CastSpell("Serpent Sting");
					//					}
				}
			}


			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
