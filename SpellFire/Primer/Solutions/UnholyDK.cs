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
			Thread.Sleep(1);

			Looting();

			if (IsOnCooldown("Death Coil")) /* global cooldown check */
			{
				return;
			}

			if (!player.IsMounted()
			    && !IsOnCooldown("Horn of Winter")
			    && !HasAura(player, "Horn of Winter"))
			{
				CastSpell("Horn of Winter");
				return;
			}


			Int64 targetGUID = GetTargetGUID();
			if (targetGUID == 0)
			{
				return;
			}

			GameObject target = objectManager.FirstOrDefault(gameObj => gameObj.GUID == targetGUID);

			if (target == null || target.Health == 0 ||
			    ci.remoteControl.CGUnit_C__UnitReaction(player.GetAddress(), target.GetAddress()) > UnitReaction.Neutral)
			{
				return;
			}

			bool isTargetInMelee = player.GetDistance(target) < DeathKnightConstants.MeleeRange;

			Vector3 targetCoords = target.Coordinates;
			if (!isTargetInMelee)
			{
				if (!player.IsMoving() && !player.IsMounted())
				{
					ci.remoteControl.CGPlayer_C__ClickToMove(player.GetAddress(), ClickToMoveType.AutoAttack, ref targetGUID, ref targetCoords, 1f);
					return;
				}
			}
			else
			{
				ci.remoteControl.CGPlayer_C__ClickToMoveStop(player.GetAddress());
				float angle = player.Coordinates.AngleBetween(targetCoords);
				ci.remoteControl.CGPlayer_C__ClickToMove(player.GetAddress(), ClickToMoveType.Face, ref targetGUID, ref targetCoords, angle);
			}

			bool isTargetBloodPlagueUp = HasAura(target, "Blood Plague", player);
			bool isTargetFrostFeverUp = HasAura(target, "Frost Fever", player);

			Console.WriteLine($"bp: {isTargetBloodPlagueUp}");
			Console.WriteLine($"ff: {isTargetFrostFeverUp}");

			DeathKnightRunesState runesState = GetAvailableRunes();

			Console.WriteLine($"blood r: {runesState.bloodReady}");
			Console.WriteLine($"frost r: {runesState.frostReady}");
			Console.WriteLine($"unholy r: {runesState.unholyReady}");

			if (isTargetInMelee && !isTargetBloodPlagueUp && runesState.unholyReady > 0)
			{
				CastSpell("Plague Strike");
				Console.WriteLine("PS");
				return;
			}

			if (!isTargetFrostFeverUp && runesState.frostReady > 0)
			{
				CastSpell("Icy Touch");
				Console.WriteLine("IT");
				return;
			}

			/* check for Pestilence possibility */
			if (isTargetInMelee && (isTargetBloodPlagueUp && isTargetFrostFeverUp) && runesState.bloodReady > 0)
			{

				IEnumerable<GameObject> pestilenceEnemies = objectManager.Where(gameObj =>
					gameObj.Type == GameObjectType.Unit
					&& gameObj.Health > 0 /* alive units */
					&& gameObj.GUID != targetGUID /* exclude target unit */
					&& player.GetDistance(gameObj) < DeathKnightConstants.PestilenceRange /* unit in range */
					&& ci.remoteControl.CGUnit_C__UnitReaction(player.GetAddress(), gameObj.GetAddress()) <= UnitReaction.Neutral /* unit attackable */
					&& !HasAura(gameObj, "Blood Plague", player) /* unit doesn't have diseases */
					&& !HasAura(gameObj, "Frost Fever", player));

				if (pestilenceEnemies.Any())
				{
					CastSpell("Pestilence");
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
				if (player.HealthPct >= 80)
				{
					CastSpell("Scourge Strike");
					Console.WriteLine("SS");
					return;
				}
				else
				{
					CastSpell("Death Strike");
					Console.WriteLine("DS");
					return;
				}
			}

			Int32 runicPower = player.RunicPower;
			if (runicPower >= 40)
			{
				CastSpell("Death Coil");
				Console.WriteLine("DC");
				return;
			}

			/* use Blood Strike only when having 2 Blood Runes ready, to have always Pestilence ready to use */
			if (isTargetInMelee && isTargetBloodPlagueUp && isTargetFrostFeverUp
				&& runesState.bloodReady == 2)
			{
				CastSpell("Blood Strike");
				return;
			}
		}

		private bool IsOnCooldown(string spellName)
		{
			string result = ExecLuaAndGetResult($"start = GetSpellCooldown('{spellName}')", "start");
			return result[0] != '0';
		}

		private bool HasAura(GameObject gameObject, string auraName, GameObject ownedBy = null)
		{
			int currentAuraIndex = 0;
			while (true)
			{
				IntPtr auraPtr = ci.remoteControl.CGUnit_C__GetAura(gameObject.GetAddress(), currentAuraIndex++);
				if (auraPtr == IntPtr.Zero)
				{
					return false;
				}

				Aura aura = memory.ReadStruct<Aura>(auraPtr);
				if (auraName == ExecLuaAndGetResult($"name = GetSpellInfo({aura.auraID})", "name"))
				{
					if (ownedBy != null)
					{
						if (aura.creatorGuid == ownedBy.GUID)
						{
							return true;
						}
						else
						{
							continue;
						}
					}
					else
					{
						return true;
					}
				}
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

					ci.remoteControl.CGPlayer_C__ClickToMoveStop(player.GetAddress());
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

		public override void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
		{
			RadarCanvas.BasicRadar(radarCanvas, radarBackBuffer, player, objectManager, GetTargetGUID(), ci);
		}

		public override void Finish()
		{
			eventListener.Dispose();
		}

		private void CastSpell(string spellName)
		{
			ci.remoteControl.FrameScript__Execute($"CastSpellByName('{spellName}')", 0, 0);
		}

		private string ExecLuaAndGetResult(string luaScript, string resultLuaVariable)
		{
			ci.remoteControl.FrameScript__Execute(luaScript, 0, 0);
			return ci.remoteControl.FrameScript__GetLocalizedText(ci.remoteControl.ClntObjMgrGetActivePlayerObj(),
				resultLuaVariable, 0);
		}

		private DeathKnightRunesState GetAvailableRunes()
		{
			uint runeCountTotal = memory.ReadUInt32(  IntPtr.Zero + Offset.RuneCount);

			DeathKnightRunesState state = new DeathKnightRunesState();

			state.bloodReady += (runeCountTotal & (1 << (int)DeathKnightRune.Blood1)) != 0 ? 1 : 0;
			state.bloodReady += (runeCountTotal & (1 << (int)DeathKnightRune.Blood2)) != 0 ? 1 : 0;

			state.frostReady += (runeCountTotal & (1 << (int)DeathKnightRune.Frost1)) != 0 ? 1 : 0;
			state.frostReady += (runeCountTotal & (1 << (int)DeathKnightRune.Frost2)) != 0 ? 1 : 0;

			state.unholyReady += (runeCountTotal & (1 << (int)DeathKnightRune.Unholy1)) != 0 ? 1 : 0;
			state.unholyReady += (runeCountTotal & (1 << (int)DeathKnightRune.Unholy2)) != 0 ? 1 : 0;

			return state;
		}

		private Int64 GetTargetGUID()
		{
			return memory.ReadInt64(IntPtr.Zero + Offset.TargetGUID);
		}
	}
}