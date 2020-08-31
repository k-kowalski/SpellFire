using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Primer.Gui;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox.Prod
{
	public partial class ProdMbox : MultiboxSolution
	{
		private class DeathKnight : Solution
		{
			private ProdMbox mbox;

			private static readonly string[] PartyBuffs = { };
			private static readonly string[] SelfBuffs =
			{
				"Horn of Winter",
				"Bone Shield",
			};

			public DeathKnight(Client client, ProdMbox mbox) : base(client)
			{
				this.mbox = mbox;
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

				if (me.IsOnCooldown("Death Coil")) /* global cooldown check */
				{
					return;
				}

				if (mbox.buffingAI)
				{
					BuffUp(me, mbox, PartyBuffs, SelfBuffs, null);
				}

				Int64[] targetGuids = GetRaidTargetGuids(me);
				GameObject target = SelectRaidTargetByPriority(targetGuids, AttackPriorities, me);
				if (target == null)
				{
					return;
				}

				if (me.Player.GetDistance(target) > MeleeAttackRange)
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
						me.ExecLua("AttackTarget();PetAttack()");
					}

					DeathKnightRunesState runesState = GetAvailableRunes();
					bool isTargetBloodPlagueUp = me.HasAura(target, "Blood Plague", me.Player);
					bool isTargetFrostFeverUp = me.HasAura(target, "Frost Fever", me.Player);

					if (mbox.complexRotation)
					{
						if (!isTargetBloodPlagueUp && runesState.unholyReady > 0)
						{
							me.CastSpell("Plague Strike");
							return;
						}

						if (!isTargetFrostFeverUp && runesState.frostReady > 0)
						{
							me.CastSpell("Icy Touch");
							return;
						}

						/* check for Pestilence possibility */
						if (isTargetBloodPlagueUp && isTargetFrostFeverUp && runesState.bloodReady > 0)
						{

							IEnumerable<GameObject> pestilenceEnemies = me.ObjectManager.Where(gameObj =>
								gameObj.Type == GameObjectType.Unit
								&& gameObj.Health > 0 /* alive units */
								&& gameObj.GUID != target.GUID /* exclude target unit */
								&& me.Player.GetDistance(gameObj) < DeathKnightConstants.PestilenceRange /* unit in range */
								&& me.ControlInterface.remoteControl.CGUnit_C__UnitReaction(me.Player.GetAddress(), gameObj.GetAddress()) <=
								UnitReaction.Neutral /* unit attackable */
								&& !me.HasAura(gameObj, "Blood Plague", me.Player) /* unit doesn't have diseases */
								&& !me.HasAura(gameObj, "Frost Fever", me.Player));

							if (pestilenceEnemies.Any())
							{
								me.CastSpell("Pestilence");
								return;
							}
						}

						if (isTargetBloodPlagueUp
							&& isTargetFrostFeverUp
							&& runesState.unholyReady > 0
							&& runesState.frostReady > 0)
						{
							if (me.Player.HealthPct >= 80)
							{
								me.CastSpell("Scourge Strike");
								return;
							}
							else
							{
								me.CastSpell("Death Strike");
								return;
							}
						}

						Int32 runicPower = me.Player.RunicPower;
						if (runicPower >= 40)
						{
							me.CastSpell("Death Coil");
							return;
						}

						/* use Blood Strike only when having 2 Blood Runes ready, to have always Pestilence ready to use */
						if (isTargetBloodPlagueUp && isTargetFrostFeverUp
							&& runesState.bloodReady == 2)
						{
							me.CastSpell("Blood Strike");
							return;
						}
					}
					else
					{
						if (!isTargetBloodPlagueUp && runesState.unholyReady > 0)
						{
							me.CastSpell("Plague Strike");
							return;
						}

						if (isTargetBloodPlagueUp
						    && isTargetFrostFeverUp
						    && runesState.unholyReady > 0
						    && runesState.frostReady > 0)
						{
							if (me.Player.HealthPct >= 80)
							{
								me.CastSpell("Scourge Strike");
								return;
							}
							else
							{
								me.CastSpell("Death Strike");
								return;
							}
						}

						Int32 runicPower = me.Player.RunicPower;
						if (runicPower >= 40)
						{
							me.CastSpell("Death Coil");
							return;
						}

						if (isTargetBloodPlagueUp && isTargetFrostFeverUp
						                          && runesState.bloodReady > 0)
						{
							me.CastSpell("Blood Strike");
							return;
						}
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
					Thread.Sleep(ProdMbox.ClientSolutionSleepMs);
				}

			}

			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			private DeathKnightRunesState GetAvailableRunes()
			{
				uint runeCountTotal = (uint)me.Memory.ReadInt32(IntPtr.Zero + Offset.RuneCount);

				DeathKnightRunesState state = new DeathKnightRunesState();

				state.bloodReady += (runeCountTotal & (1 << (int)DeathKnightRune.Blood1)) != 0 ? 1 : 0;
				state.bloodReady += (runeCountTotal & (1 << (int)DeathKnightRune.Blood2)) != 0 ? 1 : 0;

				state.frostReady += (runeCountTotal & (1 << (int)DeathKnightRune.Frost1)) != 0 ? 1 : 0;
				state.frostReady += (runeCountTotal & (1 << (int)DeathKnightRune.Frost2)) != 0 ? 1 : 0;

				state.unholyReady += (runeCountTotal & (1 << (int)DeathKnightRune.Unholy1)) != 0 ? 1 : 0;
				state.unholyReady += (runeCountTotal & (1 << (int)DeathKnightRune.Unholy2)) != 0 ? 1 : 0;

				return state;
			}
		}
	}
}
