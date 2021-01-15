using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2
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
				var targets = mbox.SelectAllRaidTargetsByPriority(targetGuids, AttackPriorities, me);
				if (targets == null)
				{
					return;
				}

				/*
				 * targeting scheme:
				 * - if high prio target is near tank and near me -> attack high prio target
				 * - if high prio target is near tank and away from me -> navigate to tank
				 *
				 * - if high prio target is away from tank and near me -> attack high prio target
				 * - if high prio target is away from tank and away from me -> make lower prio target into high prio and start over
				 *
				 * do for all targets marked
				 */
				GameObject currentTarget = null;
				foreach (var target in targets)
				{
					var tank = mbox.me.Player;
					var tankInRange = tank.GetDistance(target) <= MeleeAttackRange;
					var meInRange = me.Player.GetDistance(target) <= MeleeAttackRange;
					if (tankInRange)
					{
						currentTarget = target;
						if (!meInRange)
						{
							long _Guid = 0;
							Vector3 targetCoords = tank.Coordinates;
							me.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
								me.Player.GetAddress(), ClickToMoveType.Move, ref _Guid, ref targetCoords, 0f);
							return;
						}
						else
						{
							break;
						}
					}
					else
					{
						if (meInRange)
						{
							currentTarget = target;
							break;
						}
					}
					
				}

				if (currentTarget != null)
				{
					if (me.GetTargetGUID() != currentTarget.GUID)
					{
						me.ControlInterface.remoteControl.SelectUnit(currentTarget.GUID);
					}

					FaceTowards(me, currentTarget);
				}
				else
				{
					return;
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
							if (!me.HasAura(me.Player, "Slice and Dice", me.Player))
							{
								me.CastSpell("Slice and Dice");
							}
							else
							{
								me.CastSpell("Eviscerate");
							}
						}
						else if (energy > 45)
						{
							me.CastSpell("Sinister Strike");
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
