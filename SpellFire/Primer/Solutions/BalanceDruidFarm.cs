﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.LuaEvents;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	/// <summary>
	/// Find closest enemy, kill, loot, repeat
	/// as Balance Druid
	/// </summary>
	public class BalanceDruidFarm : Solution
	{
		private bool loot;
		private bool lootTargeted;
		private Int64 currentlyOccupiedMobGUID;

		private readonly GameObject player;
		private readonly GameObjectManager objectManager;

		private readonly LuaEventListener eventListener;

		public BalanceDruidFarm(ControlInterface ci, Memory memory) : base(ci, memory)
		{
			eventListener = new LuaEventListener(ci);
			eventListener.Bind("LOOT_OPENED", LootOpenedHandler);

			IntPtr clientConnection = memory.ReadPointer86(IntPtr.Zero + Offset.ClientConnection);
			IntPtr objectManagerAddress = memory.ReadPointer86(clientConnection + Offset.GameObjectManager);

			player = new GameObject(memory, ci.remoteControl.ClntObjMgrGetActivePlayerObj());
			objectManager = new GameObjectManager(memory, objectManagerAddress);

			loot = false;
			lootTargeted = false;

			this.Active = true;
		}

		private void LootOpenedHandler(LuaEventArgs luaEventArgs)
		{
			if (loot && lootTargeted)
			{
				ci.remoteControl.FrameScript__Execute("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end", 0, 0);
				loot = false;
				lootTargeted = false;
				ci.remoteControl.SelectUnit(0);
			}
		}

		public override void Tick()
		{
			Thread.Sleep(500);
			Int64 targetGUID = GetTargetGUID();
			Vector3 targetObjectCoords = new Vector3();
			GameObject targetObject = null;
			float distance;
			if (targetGUID == 0 && (!loot))
			{
				float minDistance = Single.MaxValue;
				Int64 GUID = 0;


				foreach (GameObject gameObject in objectManager)
				{
					//Console.WriteLine($"checking: {currentGameObject.ToString("X")} .. has guid: {currentGameObjectGUID} ");
					if (gameObject.Type == GameObjectType.Unit && gameObject.IsAlive())
					{
						distance = player.GetDistance(gameObject);
						if (distance < minDistance && (!gameObject.UnitName.Contains("Gryph")))
						{
							minDistance = distance;
							GUID = gameObject.GUID;
						}
					}
				}

				//Console.WriteLine($"Selecting GUID {GUID}");
				ci.remoteControl.SelectUnit(GUID);
			}
			else
			{
				if (targetGUID != 0 && (!lootTargeted))
				{
					targetObject = objectManager.First(gameObj => gameObj.GUID == targetGUID);
					targetObjectCoords = targetObject.Coordinates;
					Vector3 playerObjectCoords = player.Coordinates;

					distance = player.GetDistance(targetObject);
					if (distance < 35f)
					{
						if (targetObject.IsAlive())
						{
							ci.remoteControl.CGPlayer_C__ClickToMoveStop(player.GetAddress());
							float angle = playerObjectCoords.AngleBetween(targetObjectCoords);
							ci.remoteControl.CGPlayer_C__ClickToMove(player.GetAddress(), ClickToMoveType.Face, ref targetGUID, ref targetObjectCoords, angle);
							if ( ! player.IsCastingOrChanneling())
							{
								CastSpell("Wrath");
							}
							loot = true;
							currentlyOccupiedMobGUID = targetGUID;
						}
					}
					else
					{
						ci.remoteControl.CGPlayer_C__ClickToMove(player.GetAddress(), ClickToMoveType.Move, ref targetGUID, ref targetObjectCoords, 1f);
					}
				}
				else if (targetGUID == 0 && loot && (!lootTargeted))
				{
					ci.remoteControl.SelectUnit(currentlyOccupiedMobGUID);
					targetGUID = GetTargetGUID();
					lootTargeted = true;
				}
				if (targetGUID != 0 && lootTargeted)
				{
					targetObject = objectManager.First(gameObj => gameObj.GUID == targetGUID);

					distance = player.GetDistance(targetObject);
					if (distance < 6f && ( ! player.IsMoving())) // loot
					{
						ci.remoteControl.InteractUnit(targetObject.GetAddress());
						FinishLooting();
					}
					else
					{
						targetGUID = GetTargetGUID();
						targetObjectCoords = targetObject.Coordinates;
						ci.remoteControl.CGPlayer_C__ClickToMove(player.GetAddress(), ClickToMoveType.Move, ref targetGUID, ref targetObjectCoords, 1f);
					}
				}
			}
		}

		private void FinishLooting()
		{
			/*
             * looting is done in answer for Lua event
             * here we timeout when it takes too much time
             */
			DateTime interactTime = DateTime.Now;
			Func<Int32, bool> lootingTimeout = (timeoutSeconds) => (DateTime.Now - interactTime).Seconds < timeoutSeconds;
			while (loot && lootTargeted && lootingTimeout(2))
			{
				Thread.Sleep(100);
			}

			/* timeout reached, reset state */
			if (loot && lootTargeted)
			{
				loot = false;
				lootTargeted = false;
				ci.remoteControl.SelectUnit(0);
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

		private Int64 GetTargetGUID()
		{
			return memory.ReadInt64(IntPtr.Zero + Offset.TargetGUID);
		}

	}
}