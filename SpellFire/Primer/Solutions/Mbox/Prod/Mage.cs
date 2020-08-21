using SpellFire.Well.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.Prod
{
	public partial class ProdMbox : MultiboxSolution
	{
		private class Mage : Solution
		{
			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
				"Arcane Intellect",
			};
			private static readonly string[] SelfBuffs =
			{
				"Mage Armor",
			};

			private string stockScript;

			public Mage(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
				stockScript = File.ReadAllText("Scripts/Stock.lua");

				me.LuaEventListener.Bind("TRADE_SHOW", args =>
				{
					me.ExecLua(stockScript);
				});

				me.LuaEventListener.Bind("TRADE_PLAYER_ITEM_CHANGED", args =>
				{
					Thread.Sleep(300); /*  it looks that some servers need delay */
					me.ExecLua("AcceptTrade()");
				});
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

				if (me.IsOnCooldown("Fireball")) /* global cooldown check */
				{
					return;
				}

				if (mbox.buffingAI && !me.Player.IsInCombat())
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
				}

				Int64[] targetGuids = GetRaidTargetGuids(me);
				GameObject ccTarget = SelectRaidTargetByPriority(targetGuids,
					CrowdControlTarget,
					me);
				if (ccTarget != null)
				{
					if (!me.HasAura(ccTarget, "Polymorph", me.Player))
					{
						me.CastSpellOnGuid("Polymorph", ccTarget.GUID);
						return;
					}
				}

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
//					if (me.HasAura(target, "Living Bomb", me.Player))
//					{
						me.CastSpell("Fireball");
//					}
//					else
//					{
//						me.CastSpell("Living Bomb");
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