using SpellFire.Well.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Primer.Gui;

namespace SpellFire.Primer.Solutions.Mbox.Prod
{
	public partial class ProdMbox : MultiboxSolution
	{
		private class Paladin : Solution
		{
			private ProdMbox mbox;

			private static string PaladinBuffsForClass(UnitClass unitClass)
			{
				return unitClass switch
				{
					UnitClass.Paladin => "Blessing of Sanctuary",
					_ => "Blessing of Kings"
				};
			}

			private static readonly string[] PartyBuffs = { };
			private static readonly string[] SelfBuffs =
			{
				"Seal of Light", "Righteous Fury"
			};
			public Paladin(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
			}

			public override void Tick()
			{
				Thread.Sleep(200);
				me.RefreshLastHardwareEvent();

				if (!mbox.masterAI)
				{
					return;
				}

				if (!me.GetObjectMgrAndPlayer())
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Seal of Light")) /* global cooldown check */
				{
					return;
				}

				if (!me.Player.IsInCombat())
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs, PaladinBuffsForClass);
				}

				Int64 targetGUID = me.GetTargetGUID();
				if (targetGUID == 0)
				{
					return;
				}

				GameObject target = me.ObjectManager.FirstOrDefault(gameObj => gameObj.GUID == targetGUID);

				if (target == null || target.Health == 0 ||
					me.ControlInterface.remoteControl.CGUnit_C__UnitReaction(me.Player.GetAddress(), target.GetAddress()) >
					UnitReaction.Neutral)
				{
					return;
				}

				if (me.Player.GetDistance(target) > MeleeAttackRange)
				{
					return;
				}

				FaceTowards(me, target);
				if (!me.Player.IsCastingOrChanneling())
				{
					if (!me.Player.IsAutoAttacking())
					{
						me.ExecLua("AttackTarget()");
					}

					if (!me.IsOnCooldown("Hammer of Wrath") && target.HealthPct < 20)
					{
						me.CastSpell("Hammer of Wrath");
					}

					if (!me.IsOnCooldown("Judgement of Light"))
					{
						me.CastSpell("Judgement of Light");
					}

					if (!me.IsOnCooldown("Hammer of the Righteous"))
					{
						me.CastSpell("Hammer of the Righteous");
					}

					else
					{
						bool isHSUp = me.HasAura(me.Player, "Holy Shield", me.Player);
						if (isHSUp)
						{
							if (!me.IsOnCooldown("Hammer of the Righteous"))
							{
								me.CastSpell("Hammer of the Righteous");
							}
						}
						else
						{
							me.CastSpell("Holy Shield");
						}
					}
				}
			}

			public override void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
			{
				/* nothing */ Thread.Sleep(5000);
			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
