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
			private const Int32 HotStreak = 48108;
			private const Int32 FocusMagic = 54648;

			private ProdMbox mbox;
			private static readonly string[] PartyBuffs =
			{
				"Arcane Intellect",
			};
			private static readonly string[] SelfBuffs =
			{
				"Molten Armor",
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

				if (me.IsOnCooldown("Fireball")) /* global cooldown check */
				{
					return;
				}

				if (mbox.buffingAI)
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs);
					CastFocusMagic();
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

				FaceTowards(me, target);
				if (!me.Player.IsCastingOrChanneling())
				{
					if (mbox.complexRotation)
					{
						if (me.HasAura(me.Player, HotStreak, me.Player))
						{
							me.CastSpell("Pyroblast");
						}

						if (me.HasAura(target, "Living Bomb", me.Player))
						{
							me.CastSpell("Fireball");
						}
						else
						{
							me.CastSpell("Living Bomb");
						}
					}
					else
					{
						me.CastSpell("Fireball");
					}
				}
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			private void CastFocusMagic()
			{
				foreach (var client in mbox.clients)
				{
					if (client.GetObjectMgrAndPlayer())
					{
						if (ShouldFM(client) &&
						    !me.HasAura(client.Player, "Focus Magic", me.Player))
						{
							me.CastSpellOnGuid("Focus Magic", client.Player.GUID);
						}
					}
				}
			}

			private bool ShouldFM(Client client)
			{
				return client.Player.UnitClass == UnitClass.Priest;
			}
		}
	}
}