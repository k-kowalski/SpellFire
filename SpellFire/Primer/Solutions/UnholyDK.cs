using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	public static class DeathKnightConstants
	{
		public const float MeleeRange = 5f;
		public const float PestilenceRange = 10f;
	}

	public class UnholyDK : Solution
	{
		private readonly LuaEventListener eventListener;

		private ControlInterface ci;

		public UnholyDK(Client client) : base(client)
		{
			ci = client.ControlInterface;

			eventListener = new LuaEventListener(ci);
			eventListener.Bind("LOOT_OPENED", LootOpenedHandler);

			this.Active = true;
		}

		private void LootOpenedHandler(LuaEventArgs luaEventArgs)
		{
			Console.WriteLine($"[{DateTime.Now}] looting");
			ci.remoteControl.FrameScript__Execute("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end",
				0, 0);
		}

		public override void Tick()
		{
			Thread.Sleep(1);

			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}

			Looting();

			if (me.IsOnCooldown("Death Coil")) /* global cooldown check */
			{
				return;
			}

			if (!me.Player.IsMounted()
			    && !me.IsOnCooldown("Horn of Winter")
			    && !me.HasAura(me.Player, "Horn of Winter"))
			{
				me.CastSpell("Horn of Winter");
				return;
			}


			Int64 targetGUID = me.GetTargetGUID();
			if (targetGUID == 0)
			{
				return;
			}

			GameObject target = me.ObjectManager.FirstOrDefault(gameObj => gameObj.GUID == targetGUID);

			if (target == null || target.Health == 0 ||
			    ci.remoteControl.CGUnit_C__UnitReaction(me.Player.GetAddress(), target.GetAddress()) >
			    UnitReaction.Neutral)
			{
				return;
			}

			bool isTargetInMelee = me.Player.GetDistance(target) < DeathKnightConstants.MeleeRange;

			Vector3 targetCoords = target.Coordinates;
			if (!isTargetInMelee)
			{
				if (!me.Player.IsMoving() && !me.Player.IsMounted())
				{
					ci.remoteControl.CGPlayer_C__ClickToMove(me.Player.GetAddress(), ClickToMoveType.AutoAttack,
						ref targetGUID, ref targetCoords, 1f);
					return;
				}
			}
			else
			{
				ci.remoteControl.CGPlayer_C__ClickToMoveStop(me.Player.GetAddress());
				float angle = me.Player.Coordinates.AngleBetween(targetCoords);
				ci.remoteControl.CGPlayer_C__ClickToMove(me.Player.GetAddress(), ClickToMoveType.Face, ref targetGUID,
					ref targetCoords, angle);
			}

			bool isTargetBloodPlagueUp = me.HasAura(target, "Blood Plague", me.Player);
			bool isTargetFrostFeverUp = me.HasAura(target, "Frost Fever", me.Player);

			Console.WriteLine($"bp: {isTargetBloodPlagueUp}");
			Console.WriteLine($"ff: {isTargetFrostFeverUp}");

			DeathKnightRunesState runesState = GetAvailableRunes();

			Console.WriteLine($"blood r: {runesState.bloodReady}");
			Console.WriteLine($"frost r: {runesState.frostReady}");
			Console.WriteLine($"unholy r: {runesState.unholyReady}");

			if (isTargetInMelee && !isTargetBloodPlagueUp && runesState.unholyReady > 0)
			{
				me.CastSpell("Plague Strike");
				Console.WriteLine("PS");
				return;
			}

			if (!isTargetFrostFeverUp && runesState.frostReady > 0)
			{
				me.CastSpell("Icy Touch");
				Console.WriteLine("IT");
				return;
			}

			/* check for Pestilence possibility */
			if (isTargetInMelee && (isTargetBloodPlagueUp && isTargetFrostFeverUp) && runesState.bloodReady > 0)
			{

				IEnumerable<GameObject> pestilenceEnemies = me.ObjectManager.Where(gameObj =>
					gameObj.Type == GameObjectType.Unit
					&& gameObj.Health > 0 /* alive units */
					&& gameObj.GUID != targetGUID /* exclude target unit */
					&& me.Player.GetDistance(gameObj) < DeathKnightConstants.PestilenceRange /* unit in range */
					&& ci.remoteControl.CGUnit_C__UnitReaction(me.Player.GetAddress(), gameObj.GetAddress()) <=
					UnitReaction.Neutral /* unit attackable */
					&& !me.HasAura(gameObj, "Blood Plague", me.Player) /* unit doesn't have diseases */
					&& !me.HasAura(gameObj, "Frost Fever", me.Player));

				if (pestilenceEnemies.Any())
				{
					me.CastSpell("Pestilence");
					Console.WriteLine("Pesti");
					return;
				}
			}

			if (isTargetInMelee
			    && isTargetBloodPlagueUp
			    && isTargetFrostFeverUp
			    && runesState.unholyReady > 0
			    && runesState.frostReady > 0)
			{
				if (me.Player.HealthPct >= 80)
				{
					me.CastSpell("Scourge Strike");
					Console.WriteLine("SS");
					return;
				}
				else
				{
					me.CastSpell("Death Strike");
					Console.WriteLine("DS");
					return;
				}
			}

			Int32 runicPower = me.Player.RunicPower;
			if (runicPower >= 40)
			{
				me.CastSpell("Death Coil");
				Console.WriteLine("DC");
				return;
			}

			/* use Blood Strike only when having 2 Blood Runes ready, to have always Pestilence ready to use */
			if (isTargetInMelee && isTargetBloodPlagueUp && isTargetFrostFeverUp
			    && runesState.bloodReady == 2)
			{
				me.CastSpell("Blood Strike");
				return;
			}
		}

		private void Looting()
		{
			IEnumerable<GameObject> lootables =
				me.ObjectManager.Where(gameObj => gameObj.Type == GameObjectType.Unit && gameObj.IsLootable());

			float minDistance = Single.MaxValue;
			GameObject closestLootableUnit = null;

			foreach (GameObject lootable in lootables)
			{
				float distance = me.Player.GetDistance(lootable);
				if (distance < minDistance)
				{
					minDistance = distance;
					closestLootableUnit = lootable;
				}
			}

			if (closestLootableUnit != null)
			{
				Console.WriteLine(
					$"[{DateTime.Now}] closest target away {minDistance}y, checked {lootables.Count()} lootable/s.");

				if (minDistance < 6f && (!me.Player.IsMoving()) && (!me.Player.IsCastingOrChanneling()))
				{
					Console.WriteLine($"[{DateTime.Now}] interacting");

					ci.remoteControl.CGPlayer_C__ClickToMoveStop(me.Player.GetAddress());
					ci.remoteControl.InteractUnit(closestLootableUnit.GetAddress());

					/*
					 * one case for this are corpses that are marked lootable
					 * but in fact loot inside is not ours(ie. other people quest items)
					 * in this event bot would be hammering fruitless looting, which could look unnatural
					 *
					 * other than above is
					 * after successful looting rest a little longer
					 * so it will be more believable
					 */
					Thread.Sleep(100);
				}
			}
		}

		private DeathKnightRunesState GetAvailableRunes()
		{
			uint runeCountTotal = me.Memory.ReadUInt32(IntPtr.Zero + Offset.RuneCount);

			DeathKnightRunesState state = new DeathKnightRunesState();

			state.bloodReady += (runeCountTotal & (1 << (int) DeathKnightRune.Blood1)) != 0 ? 1 : 0;
			state.bloodReady += (runeCountTotal & (1 << (int) DeathKnightRune.Blood2)) != 0 ? 1 : 0;

			state.frostReady += (runeCountTotal & (1 << (int) DeathKnightRune.Frost1)) != 0 ? 1 : 0;
			state.frostReady += (runeCountTotal & (1 << (int) DeathKnightRune.Frost2)) != 0 ? 1 : 0;

			state.unholyReady += (runeCountTotal & (1 << (int) DeathKnightRune.Unholy1)) != 0 ? 1 : 0;
			state.unholyReady += (runeCountTotal & (1 << (int) DeathKnightRune.Unholy2)) != 0 ? 1 : 0;

			return state;
		}

		public override void Dispose()
		{
			eventListener.Dispose();
		}
	}
}