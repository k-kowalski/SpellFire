using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.Prod
{
	public partial class ProdMboxV2 : MultiboxSolution
	{
		private class Rogue : Solution
		{
			private ProdMboxV2 mbox;

			public Rogue(Client client, ProdMboxV2 mbox) : base(client)
			{
				this.mbox = mbox;
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

				if (!mbox.slavesAI)
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Sinister Strike")) /* global cooldown check */
				{
					return;
				}

				Int64[] targetGuids = GetRaidTargetGuids(me);
				var targets = SelectAllRaidTargetsByPriority(targetGuids, AttackPriorities, me);
				if (targets == null)
				{
					return;
				}

				// if target of higher priority is away, target closer one of lower priority
				GameObject target = targets[0];
				if (targets.Count > 1 && targets[0].GetDistance(me.Player) > MeleeAttackRange)
				{
					if (targets.Count > 2 && targets[1].GetDistance(me.Player) > MeleeAttackRange)
					{
						target = targets[2];
					}
					else
					{
						target = targets[1];
					}
				}


				if (me.GetTargetGUID() != target.GUID)
				{
					me.ControlInterface.remoteControl.SelectUnit(target.GUID);
				}


				if (me.Player.GetDistance(target) > MeleeAttackRange)
				{
					// navigate to exactly tank's position if target is in tank's range
					var tank = mbox.me.Player;
					if (tank.GetDistance(target) > MeleeAttackRange)
					{
						return;
					}
					else
					{
						long _Guid = 0;
						Vector3 targetCoords = tank.Coordinates;
						me.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
							me.Player.GetAddress(), ClickToMoveType.Move, ref _Guid, ref targetCoords, 0f);

						return;
					}
				}
				else
				{
					FaceTowards(me, target);
				}

				var energy = me.Player.Energy;
				var comboPoints = GetComboPoints();
				if (!me.Player.IsCastingOrChanneling())
				{
					if (!me.Player.IsAutoAttacking())
					{
						me.ExecLua("AttackTarget()");
					}

					if (energy > 40)
					{
						if (comboPoints > 3)
						{
							me.CastSpell("Eviscerate");
						}
						else
						{
							if (energy > 50)
							{
								me.CastSpell("Sinister Strike");
							}
						}
					}
				}
			}

			public byte GetComboPoints()
			{
				return me.Memory.Read(IntPtr.Zero + Offset.ComboPoints, 1)[0];
			}


			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
