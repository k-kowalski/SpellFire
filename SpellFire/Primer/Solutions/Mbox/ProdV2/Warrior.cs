using SpellFire.Well.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Primer.Gui;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2
{
	public partial class ProdMboxV2 : MultiboxSolution
	{
		private class Warrior : Solution
		{
			private ProdMboxV2 mbox;

			public Warrior(Client client, ProdMboxV2 mbox) : base(client)
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

				LootAround(me);

				if (me.IsOnCooldown("Heroic Strike")) /* global cooldown check */
				{
					return;
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


				if (me.Player.GetDistance(target) > MeleeAttackRange)
				{
					// navigate to target if it is near any player
					var nearPlayer = GetNearPlayer(target);
					if (nearPlayer != null)
					{
						long _Guid = 0;
						Vector3 targetCoords = nearPlayer.Player.Coordinates;
						me.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
							me.Player.GetAddress(), ClickToMoveType.Move, ref _Guid, ref targetCoords, 0f);
					}
					return;
				}

				if (me.Player.IsMounted())
				{
					return;
				}
				else
				{
					FaceTowards(me, target);
				}

				var rage = me.Player.Rage;
				if (!me.Player.IsCastingOrChanneling())
				{
					if (!me.Player.IsAutoAttacking())
					{
						me.ExecLua("AttackTarget()");
					}

					if (rage > 5)
					{
						var revenge = "Revenge";
						if (me.CanBeCasted(revenge) && !me.IsOnCooldown(revenge))
						{
							me.CastSpell(revenge);
						}
						else if (rage > 10 && !me.HasAura(me.Player, "Battle Shout", me.Player))
						{
							me.CastSpell("Battle Shout");
						}
						else if (rage > 20)
						{
							if (!me.IsOnCooldown("Thunder Clap"))
							{
								me.CastSpell("Thunder Clap");
							}
							else if (!me.IsOnCooldown("Shield Slam"))
							{
								me.CastSpell("Shield Slam");
							}
							else
							{
								me.CastSpell("Devastate");
							}
						}
					}
				}
			}

			private Client GetNearPlayer(GameObject target)
			{
				var targetCoords = target.Coordinates;
				return mbox.clients.FirstOrDefault(c => c.Player.Coordinates.Distance(targetCoords) <= MeleeAttackRange);
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
