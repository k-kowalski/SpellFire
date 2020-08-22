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

				if (me.CastPrioritySpell())
				{
					return;
				}

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
				if (mbox.buffingAI)
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
					CheckShamanEnchant();
				}

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
					bool isFSUp = me.HasAura(target, "Flame Shock", me.Player);
					if (isFSUp)
					{
						if (!me.IsOnCooldown("Chain Lightning"))
						{
							me.CastSpell("Chain Lightning");
						}
						else
						{
							me.CastSpell("Lightning Bolt");
						}
					}
					else
					{
						if (!me.IsOnCooldown("Flame Shock"))
						{
							me.CastSpell("Flame Shock");
						}
					}
				}
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
	}
}