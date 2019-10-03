using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class BalanceDruidFarm : Solution
	{
		private bool loot;
		private bool lootTargeted;
		private Int64 currentlyOccupiedMobGUID;

		private readonly ControlInterface ci;
		private readonly Memory memory;

		private readonly GameObject player;
		private readonly GameObjectManager objectManager;

		public BalanceDruidFarm(ControlInterface ci, Memory memory)
		{
			this.memory = memory;
			this.ci = ci;

			this.ci.hostControl.LuaEvent += LootOpenedHandler;

			IntPtr clientConnection = memory.ReadPointer86(IntPtr.Zero + Offset.ClientConnection);
			IntPtr objectManagerAddress = memory.ReadPointer86(clientConnection + Offset.GameObjectManager);

			player = new GameObject(memory, ci.remoteControl.ClntObjMgrGetActivePlayerObj());
			objectManager = new GameObjectManager(memory, objectManagerAddress);

			loot = false;
			lootTargeted = false;
		}

		private void LootOpenedHandler(List<string> luaEventArgs)
		{
			if (luaEventArgs[1] == "LOOT_OPENED")
			{
				Task.Run(() =>
				{
					if (loot && lootTargeted)
					{
						ci.remoteControl.FrameScript__Execute("for i = 1, GetNumLootItems() do LootSlot(i) ConfirmLootSlot(i) end", 0, 0);
						loot = false;
						lootTargeted = false;
						ci.remoteControl.SelectUnit(0);
					}
				});
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
					if (gameObject.Type == GameObjectType.Unit && IsAlive(gameObject))
					{
						distance = GetDistance(gameObject, player);
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
					targetObjectCoords = GetCoords(targetObject);
					Vector3 playerObjectCoords = GetCoords(player);

					distance = GetDistance(player, targetObject);
					if (distance < 35f)
					{
						if (IsAlive(targetObject))
						{
							ci.remoteControl.CGPlayer_C__ClickToMoveStop(player.GetAddress());
							float angle = (float)Math.Atan2(targetObjectCoords.y - playerObjectCoords.y, targetObjectCoords.x - playerObjectCoords.x);
							ci.remoteControl.CGPlayer_C__ClickToMove(player.GetAddress(), ClickToMoveType.Face, ref targetGUID, ref targetObjectCoords, angle);
							CastSpell("Fireball");
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
				if (lootTargeted)
				{
					targetObject = objectManager.First(gameObj => gameObj.GUID == targetGUID);

					distance = GetDistance(player, targetObject);
					if (distance < 5f && (!IsMoving(player))) // loot
					{
						ci.remoteControl.InteractUnit(targetObject.GetAddress());
						FinishLooting();
					}
					else
					{
						targetGUID = GetTargetGUID();
						targetObjectCoords = GetCoords(targetObject);
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

			if (loot && lootTargeted)
			{
				loot = false;
				lootTargeted = false;
				ci.remoteControl.SelectUnit(0);
			}
		}

		public override void Finish()
		{
			/* no finish logic */
		}

		public override void Stop()
		{
			ci.hostControl.LuaEvent -= LootOpenedHandler;
			this.Active = false;
		}

		private Vector3 GetCoords(GameObject gameObject)
		{
			return memory.ReadStruct<Vector3>(gameObject.GetAddress() + Offset.PositionX);
		}

		private float GetDistance(GameObject first, GameObject other)
		{
			Vector3 firstCoords = GetCoords(first);
			Vector3 otherCoords = GetCoords(other);
			return (firstCoords - otherCoords).Length();
		}

		private bool IsMoving(GameObject gameObject)
		{
			IntPtr movInfo = memory.ReadPointer86(gameObject.GetAddress() + 216);
			return memory.ReadInt32(movInfo + 96) != 0;
		}

		private bool IsAlive(GameObject gameObject)
		{
			IntPtr unitInfo = memory.ReadPointer86(gameObject.GetAddress() + 0x8);
			Int32 health = memory.ReadInt32(unitInfo + (0x18 * 4));
			//Console.WriteLine($"h:{health}");
			return health != 0;
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