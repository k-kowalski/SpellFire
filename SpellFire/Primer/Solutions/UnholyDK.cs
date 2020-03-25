using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	public enum DeathKnightRune : byte
	{
		Blood1 = 1,
		Blood2 = 2,

		Frost1 = 3,
		Frost2 = 4,

		Unholy1 = 5,
		Unholy2 = 6,
	}

	public class UnholyDK : Solution
	{
		private bool loot;
		private bool lootTargeted;
		private Int64 currentlyOccupiedMobGUID;

		private readonly GameObject player;
		private readonly GameObjectManager objectManager;

		private readonly LuaEventListener eventListener;

		public UnholyDK(ControlInterface ci, Memory memory) : base(ci, memory)
		{
			eventListener = new LuaEventListener(ci);
			eventListener.Bind("LOOT_OPENED", LootOpenedHandler);

			IntPtr clientConnection = memory.ReadPointer86(IntPtr.Zero + Offset.ClientConnection);
			IntPtr objectManagerAddress = memory.ReadPointer86(clientConnection + Offset.GameObjectManager);

			player = new GameObject(memory, ci.remoteControl.ClntObjMgrGetActivePlayerObj());
			objectManager = new GameObjectManager(memory, objectManagerAddress);

			this.Active = true;
		}

		private void LootOpenedHandler(LuaEventArgs luaEventArgs)
		{
			Console.WriteLine($"[{DateTime.Now}] looting");
			ci.remoteControl.FrameScript__Execute("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end", 0, 0);
		}

		public override void Tick()
		{
			Thread.Sleep(800 + SFUtil.RandomGenerator.Next(-200, 200));

			Looting();

			Int64 targetGUID = GetTargetGUID();
			if (targetGUID == 0)
			{
				return;
			}

			GameObject target = objectManager.FirstOrDefault(gameObj => gameObj.GUID == targetGUID);

			if (target == null || target.Health == 0)
			{
				return;
			}

			//Vector3 targetCoords = target.Coordinates;
			//ci.remoteControl.CGPlayer_C__ClickToMove(player.GetAddress(), ClickToMoveType.AutoAttack, ref targetGUID, ref targetCoords, 1f);

			if (GetTargetDebuffRemainingTime("Blood Plague") == 0.0
			    && (IsRuneReady(DeathKnightRune.Unholy1) || IsRuneReady(DeathKnightRune.Unholy2)))
			{
				CastSpell("Plague Strike");
			}

			Int32 runicPower = player.RunicPower;
			if (runicPower >= 40)
			{
				CastSpell("Death Coil");
				return;
			}

			if (GetTargetDebuffRemainingTime("Frost Fever") == 0.0
			    && (IsRuneReady(DeathKnightRune.Frost1) || IsRuneReady(DeathKnightRune.Frost2)))
			{
				CastSpell("Icy Touch");
				return;
			}

			if (GetTargetDebuffRemainingTime("Blood Plague") > 0.0
			    && GetTargetDebuffRemainingTime("Frost Fever") > 0.0
			    && (IsRuneReady(DeathKnightRune.Unholy1) || IsRuneReady(DeathKnightRune.Unholy2))
			    && (IsRuneReady(DeathKnightRune.Frost1) || IsRuneReady(DeathKnightRune.Frost2)))
			{
				if (player.HealthPct >= 60)
				{
					CastSpell("Scourge Strike");
					return;
				}
				else
				{
					CastSpell("Death Strike");
					return;
				}
			}

			if (GetTargetDebuffRemainingTime("Blood Plague") > 0.0
			    && GetTargetDebuffRemainingTime("Frost Fever") > 0.0
			    && (IsRuneReady(DeathKnightRune.Blood1)))
			{
				CastSpell("Blood Strike");
				return;
			}
		}

		private void Looting()
		{
			IEnumerable<GameObject> lootables = objectManager.Where(gameObj => gameObj.Type == GameObjectType.Unit && gameObj.IsLootable());

			float minDistance = Single.MaxValue;
			GameObject closestLootableUnit = null;

			foreach (GameObject lootable in lootables)
			{
				float distance = player.GetDistance(lootable);
				if (distance < minDistance)
				{
					minDistance = distance;
					closestLootableUnit = lootable;
				}
			}

			if (closestLootableUnit != null)
			{
				Console.WriteLine($"[{DateTime.Now}] closest target away {minDistance}y, checked {lootables.Count()} lootable/s.");

				if (minDistance < 6f && (!player.IsMoving()) && (!player.IsCastingOrChanneling()))
				{
					Console.WriteLine($"[{DateTime.Now}] interacting");

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

		public override void Finish()
		{
			eventListener.Dispose();
		}

		private void CastSpell(string spellName)
		{
			ci.remoteControl.FrameScript__Execute($"CastSpellByName('{spellName}')", 0, 0);
		}

		// TODO can be abstracted
		private double GetTargetDebuffRemainingTime(string debuff)
		{
			string luaScript =
				$"local name, rank, icon, count, dispelType, duration, expires, caster, isStealable = UnitDebuff('target', '{debuff}');" +
				$"if expires then debuffRemainingTime = expires - GetTime() else debuffRemainingTime = 0 end";
			ci.remoteControl.FrameScript__Execute(luaScript, 0, 0);

			string result = ci.remoteControl.FrameScript__GetLocalizedText(
				ci.remoteControl.ClntObjMgrGetActivePlayerObj(),
				"debuffRemainingTime", 0);

			try
			{
				return Double.Parse(result);
			}
			catch (FormatException e)
			{
				Console.WriteLine(e);
				return 0.0;
			}
		}

		// TODO can be abstracted
		private bool IsRuneReady(DeathKnightRune rune)
		{
			string luaScript =
				$"start, duration, runeReady = GetRuneCooldown({(byte)rune})";
			ci.remoteControl.FrameScript__Execute(luaScript, 0, 0);

			string result = ci.remoteControl.FrameScript__GetLocalizedText(
				ci.remoteControl.ClntObjMgrGetActivePlayerObj(),
				"start", 0);

			return result[0] == '0';
		}

		private Int64 GetTargetGUID()
		{
			return memory.ReadInt64(IntPtr.Zero + Offset.TargetGUID);
		}

	}
}