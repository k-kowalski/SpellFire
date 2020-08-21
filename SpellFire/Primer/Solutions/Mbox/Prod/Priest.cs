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
			private static readonly string[] PartyBuffs =
			{
				//"Power Word: Fortitude",
				"Divine Spirit",
				//"Shadow Protection"
			};
			private static readonly string[] SelfBuffs =
			{
				"Inner Fire", "Vampiric Embrace", "Shadowform"
			};

			public Priest(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
			}

			public override void Tick()
			{
				Thread.Sleep(ProdMbox.ClientSolutionSleep);
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

				if (me.IsOnCooldown("Smite")) /* global cooldown check */
				{
					return;
				}

				if (mbox.buffingAI && !me.Player.IsInCombat())
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
//					bool isDPUp = me.HasAura(target, "Vampiric Touch", me.Player);
//					if (isDPUp)
//					{
//						bool isSWPUp = me.HasAura(target, "Shadow Word: Pain", me.Player);
//						if (isSWPUp)
//						{
							me.CastSpell(!me.IsOnCooldown("Mind Blast") ? "Mind Blast" : "Mind Flay");
//						}
//						else
//						{
//							me.CastSpell("Shadow Word: Pain");
//						}
//					}
//					else
//					{
//						me.CastSpell("Vampiric Touch");
//					}

				}
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			private void HealUp()
			{
				foreach (Client client in mbox.clients)
				{
					if (client.Player.HealthPct < 50
						&& (!me.HasAura(client.Player, "Renew", null)))
					{
						me.CastSpellOnGuid("Renew", client.Player.GUID);
						return;
					}
				}
			}
		}
	}
}
