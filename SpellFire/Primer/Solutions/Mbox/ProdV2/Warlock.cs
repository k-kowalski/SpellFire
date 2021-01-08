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
		private class Warlock : Solution
		{
			private ProdMboxV2 mbox;
			private PetType currentPetType;

			public Warlock(Client client, ProdMboxV2 mbox) : base(client)
			{
				this.mbox = mbox;

				me.LuaEventListener.Bind("PET_BAR_UPDATE", args =>
				{
					if (me.GetObjectMgrAndPlayer())
					{
						currentPetType = GetCurrentPetType();
						Console.WriteLine($"Current Warlock pet type: {currentPetType}");
					}
				});
				currentPetType = GetCurrentPetType();
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

				if (me.IsOnCooldown("Shadow Bolt")) /* global cooldown check */
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


				if (me.Player.GetDistance(target) > RangedAttackRange)
				{
					return;
				}
				else
				{
					FaceTowards(me, target);
				}

				if (currentPetType == PetType.Ranged)
				{
					me.ExecLua("PetAttack()");
				}
				else if (currentPetType == PetType.Melee)
				{
					// navigate pet to target if target is in tank's(master's) range
					if (mbox.me.Player.GetDistance(target) <= MeleeAttackRange)
					{
						me.ExecLua("PetAttack()");
					}
				}

				if (!me.Player.IsCastingOrChanneling())
				{
					bool isImmoUp = me.HasAuraEx(target, "Immolate", me.Player);
					if(!isImmoUp)
					{
						me.CastSpell("Immolate");
					}
					else
					{
						me.CastSpell("Shadow Bolt");
					}
				}
			}

			private PetType GetCurrentPetType()
			{
				var currentPetFirstAbility = me.ExecLuaAndGetResult(
					"name, subtext, texture, isToken, isActive, autoCastAllowed, autoCastEnabled = GetPetActionInfo(4)",
					"name");
				
				switch (currentPetFirstAbility)
				{
					case "Firebolt": // imp
						return PetType.Ranged;
					case "Suffering": // voidwalker
					case "Devour Magic": // felhunter
					case "Lash of Pain": // succubus
					case "Intercept": // felguard
						return PetType.Melee;
					default:
						return PetType.None;
				}
			}


			public override void Dispose()
			{
				me.LuaEventListener.Dispose();
			}

			enum PetType
			{
				None,
				Melee,
				Ranged
			}
		}
	}
}
