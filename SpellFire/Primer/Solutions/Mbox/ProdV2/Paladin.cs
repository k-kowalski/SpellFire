using SpellFire.Well.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Primer.Gui;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2
{
	public partial class ProdMboxV2 : MultiboxSolution
	{
		private class Paladin : Solution
		{
			private ProdMboxV2 mbox;

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
				//"Seal of Light",
				"Righteous Fury"
			};
			public Paladin(Client client, ProdMboxV2 mbox) : base(client)
			{
				this.mbox = mbox;

				const int viewDistanceMax = 1250;
				me.ExecLua($"SetCVar('farclip', {viewDistanceMax})");
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

				if (!mbox.masterAI)
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
					BuffUp(me, mbox, PartyBuffs, SelfBuffs, PaladinBuffsForClass);
				}

				long targetGuid = me.GetTargetGUID();
				if (targetGuid == 0)
				{
					return;
				}
				GameObject target = me.ObjectManager.FirstOrDefault(obj => obj.GUID == targetGuid);
				bool validTarget = target != null
				             && target.Health > 0
				             && me.ControlInterface.remoteControl
					             .CGUnit_C__UnitReaction(me.Player.GetAddress(), target.GetAddress()) <= UnitReaction.Neutral;
				if (!validTarget)
				{
					return;
				}

				if (me.Player.GetDistance(target) > MeleeAttackRange || me.Player.IsMounted())
				{
					return;
				}
				else
				{
					FaceTowards(me, target);
				}

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

					if (!me.IsOnCooldown("Shield of Righteousness"))
					{
						me.CastSpell("Shield of Righteousness");
					}

					bool isHSUp = me.HasAura(me.Player, "Holy Shield", null);
					if (!isHSUp)
					{
						me.CastSpell("Holy Shield");
					}
				}
			}

			public override void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
			{
				if (mbox.masterAI && mbox.radarOn)
				{
					base.RenderRadar(radarCanvas, radarBackBuffer);
				}
				else
				{
					Thread.Sleep(ProdMboxV2.ClientSolutionSleepMs);
				}

			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
