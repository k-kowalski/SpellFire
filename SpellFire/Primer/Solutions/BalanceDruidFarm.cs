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
	/// <summary>
	/// Find closest enemy, kill, loot, repeat
	/// as Balance Druid
	/// </summary>
	public class BalanceDruidFarm : Solution
	{
		private bool loot;
		private bool lootTargeted;
		private Int64 currentlyOccupiedMobGUID;

		private readonly LuaEventListener eventListener;

		private ControlInterface ci;

		public BalanceDruidFarm(Client client) : base(client)
		{
			ci = client.ControlInterface;

			eventListener = new LuaEventListener(ci);
			eventListener.Bind("LOOT_OPENED", LootOpenedHandler);

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

			if (!client.GetObjectMgrAndPlayer())
			{
				return;
			}

			Int64 targetGUID = client.GetTargetGUID();
			Vector3 targetObjectCoords = new Vector3();
			GameObject targetObject = null;
			float distance;
			if (targetGUID == 0 && (!loot))
			{
				float minDistance = Single.MaxValue;
				Int64 GUID = 0;


				foreach (GameObject gameObject in client.ObjectManager)
				{
					//Console.WriteLine($"checking: {currentGameObject.ToString("X")} .. has guid: {currentGameObjectGUID} ");
					if (gameObject.Type == GameObjectType.Unit
						&& gameObject.Health > 0
						&& gameObject.UnitType != CreatureType.Critter)
					{
						distance = client.Player.GetDistance(gameObject);
						if (distance < minDistance)
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
					targetObject = client.ObjectManager.First(gameObj => gameObj.GUID == targetGUID);
					targetObjectCoords = targetObject.Coordinates;
					Vector3 playerObjectCoords = client.Player.Coordinates;

					distance = client.Player.GetDistance(targetObject);
					if (distance < 35f)
					{
						if (targetObject.Health > 0)
						{
							ci.remoteControl.CGPlayer_C__ClickToMoveStop(client.Player.GetAddress());
							float angle = playerObjectCoords.AngleBetween(targetObjectCoords);
							ci.remoteControl.CGPlayer_C__ClickToMove(client.Player.GetAddress(), ClickToMoveType.Face, ref targetGUID, ref targetObjectCoords, angle);
							if ( ! client.Player.IsCastingOrChanneling())
							{
								client.CastSpell("Wrath");
							}
							loot = true;
							currentlyOccupiedMobGUID = targetGUID;
						}
					}
					else
					{
						ci.remoteControl.CGPlayer_C__ClickToMove(client.Player.GetAddress(), ClickToMoveType.Move, ref targetGUID, ref targetObjectCoords, 1f);
					}
				}
				else if (targetGUID == 0 && loot && (!lootTargeted))
				{
					ci.remoteControl.SelectUnit(currentlyOccupiedMobGUID);
					targetGUID = client.GetTargetGUID();
					lootTargeted = true;
				}
				if (targetGUID != 0 && lootTargeted)
				{
					targetObject = client.ObjectManager.First(gameObj => gameObj.GUID == targetGUID);

					distance = client.Player.GetDistance(targetObject);
					if (distance < 6f && ( ! client.Player.IsMoving())) // loot
					{
						ci.remoteControl.InteractUnit(targetObject.GetAddress());
						FinishLooting();
					}
					else
					{
						targetGUID = client.GetTargetGUID();
						targetObjectCoords = targetObject.Coordinates;
						ci.remoteControl.CGPlayer_C__ClickToMove(client.Player.GetAddress(), ClickToMoveType.Move, ref targetGUID, ref targetObjectCoords, 1f);
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

		public override void Dispose()
		{
			eventListener.Dispose();
		}
	}
}