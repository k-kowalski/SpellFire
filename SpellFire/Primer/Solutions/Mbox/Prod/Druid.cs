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
		private class Druid : Solution
		{
			private const Int32 EclipseLunar = 48518;

			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
				"Gift of the Wild",
				"Thorns",
			};
			private static readonly string[] SelfBuffs =
			{
				"Moonkin Form"
			};

			public Druid(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
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

				if (me.IsOnCooldown("Wrath")) /* global cooldown check */
				{
					return;
				}

				if (mbox.buffingAI)
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
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
						if (me.HasAura(target, "Moonfire", me.Player))
						{
							if (me.HasAura(target, "Insect Swarm", me.Player))
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
							else
							{
								me.CastSpell("Insect Swarm");
							}
						}
						else
						{
							me.CastSpell("Moonfire");
						}
					}
					else
					{
						me.CastSpell("Wrath");
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
