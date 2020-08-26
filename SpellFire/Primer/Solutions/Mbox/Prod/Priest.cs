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
				"Prayer of Fortitude",
				"Prayer of Spirit",
				"Prayer of Shadow Protection"
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

				if (me.IsOnCooldown("Smite")) /* global cooldown check */
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

				if (me.Player.GetDistance(target) > RangedAttackRange)
				{
					return;
				}

				if (me.GetTargetGUID() != target.GUID)
				{
					me.ControlInterface.remoteControl.SelectUnit(target.GUID);
				}
				else
				{
					FaceTowards(me, target);
				}

				if (!me.Player.IsCastingOrChanneling())
				{
					if (mbox.complexRotation)
					{
						bool isDPUp = me.HasAura(target, "Vampiric Touch", me.Player);
						if (isDPUp)
						{
							bool isSWPUp = me.HasAura(target, "Shadow Word: Pain", me.Player);
							if (isSWPUp)
							{
								me.CastSpell(!me.IsOnCooldown("Mind Blast") ? "Mind Blast" : "Mind Flay");
							}
							else
							{
								me.CastSpell("Shadow Word: Pain");
							}
						}
						else
						{
							me.CastSpell("Vampiric Touch");
						}
					}
					else
					{
						
						me.CastSpell(!me.IsOnCooldown("Mind Blast") ? "Mind Blast" : "Mind Flay");
					}

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
