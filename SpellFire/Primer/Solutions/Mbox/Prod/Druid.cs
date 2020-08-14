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
			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
				"Mark of the Wild",
				//"Thorns",
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

				if (!me.Player.IsInCombat())
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
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
					if (me.HasAura(target, "Moonfire", me.Player))
					{
						if (me.HasAura(target, "Insect Swarm", me.Player))
						{
							me.CastSpell("Wrath");
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
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
