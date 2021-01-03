using SpellFire.Well.Model;
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
		private class Warrior : Solution
		{
			private ProdMboxV2 mbox;

			private static readonly string[] PartyBuffs =
			{
				"Battle Shout",
			};

			public Warrior(Client client, ProdMboxV2 mbox) : base(client)
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

				if (!mbox.masterAI)
				{
					return;
				}

				LootAround(me);

				if (me.IsOnCooldown("Heroic Strike")) /* global cooldown check */
				{
					return;
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


				if (me.Player.GetDistance(target) > MeleeAttackRange)
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

					if (rage > 30)
					{
						me.CastSpell("Heroic Strike");
					}
				}
			}


			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}
		}
	}
}
