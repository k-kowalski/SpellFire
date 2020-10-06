using SpellFire.Well.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
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
				"Arcane Brilliance",
			};
			private static readonly string[] SelfBuffs =
			{
				"Molten Armor",
			};

			public Mage(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;

				me.LuaEventListener.Bind("TRADE_SHOW", args =>
				{
					if (mbox.slavesAI)
					{
						/* trade conjured water */
						me.ExecLua(mbox.UtilScript);
						me.ExecLua(
							$"filter = function(itemName) return itemName:find('Conjured') and itemName:find('Water') end;" +
							$"sfUseBagItem(filter)");
					}
				});

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
									me.CastSpell("Icy Veins");
								}
							}
						}
					}
				});
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
						if (me.HasAura(me.Player, HotStreak, me.Player))
						{
							me.CastSpell("Pyroblast");
						}

						if (me.HasAura(target, "Living Bomb", me.Player))
						{
							me.CastSpell("Frostfire Bolt");
						}
						else
						{
							me.CastSpell("Living Bomb");
						}
					}
					else
					{
						me.CastSpell("Frostfire Bolt");
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
				return client.Player.UnitClass == UnitClass.Shaman;
			}
		}
	}
}