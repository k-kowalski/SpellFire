using SpellFire.Well.Model;
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
		private class Shaman : Solution
		{
			private ProdMboxV2 mbox;
			private static readonly string[] PartyBuffs =
			{
			};
			private static readonly string[] SelfBuffs =
			{
				"Water Shield",
			};

			public Shaman(Client client, ProdMboxV2 mbox) : base(client)
			{
				this.mbox = mbox;

				me.LuaEventListener.Bind("SPELL_AURA_REMOVED", args =>
				{
					if (mbox.slavesAI)
					{
						long destGuid = Convert.ToInt64(args.Args[5], 16);
						if (destGuid == me.Player.GUID)
						{
							var auraName = args.Args[9];
							if (auraName == "Bloodlust" || auraName == "Power Infusion")
							{
								if (me.GetObjectMgrAndPlayer() && me.Player.IsInCombat())
								{
									/* continue burst after boost expired */
									me.CastSpell("Elemental Mastery");
								}
							}
						}
					}
				});
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
					CheckShamanEnchant();
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

				if (!me.Player.IsCastingOrChanneling())
				{
					/* no simple/complex rotation separation */
					bool isFSUp = me.HasAura(target, "Flame Shock", me.Player);
					if (isFSUp)
					{
						if (!me.IsOnCooldown("Lava Burst"))
						{
							me.CastSpell("Lava Burst");
						}
						else
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
					}
					else
					{
						if (!me.IsOnCooldown("Flame Shock"))
						{
							me.CastSpell("Flame Shock");
						}
						else
						{
							me.CastSpell("Lightning Bolt");
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