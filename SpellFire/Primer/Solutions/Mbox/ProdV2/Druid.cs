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
		private class Druid : Solution
		{
			private const Int32 EclipseLunar = 48518;

			private ProdMboxV2 mbox;
			private static readonly string[] PartyBuffs =
			{
				"Gift of the Wild",
				"Thorns",
			};
			private static readonly string[] SelfBuffs =
			{
				"Moonkin Form"
			};

			public Druid(Client client, ProdMboxV2 mbox) : base(client)
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

				if (me.IsOnCooldown("Wrath")) /* global cooldown check */
				{
					return;
				}

				if (mbox.buffingAI)
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
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
					var shouldMF = target.Health > (me.Player.MaxHealth * 2.5) &&
					               !me.HasAura(target, "Moonfire", me.Player);
					if (shouldMF)
					{
						me.CastSpell("Moonfire");
					}
					else
					{
						var shouldIS = target.Health > (me.Player.MaxHealth * 2.5) &&
						               !me.HasAura(target, "Insect Swarm", me.Player);
						if (shouldIS)
						{
							me.CastSpell("Insect Swarm");
						}
						else
						{
							if (me.HasAura(me.Player, EclipseLunar, me.Player))
							{
								me.CastSpell("Starfire");
							}
							else
							{
								me.CastSpell("Wrath");
							}
						}
					}
				}
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
