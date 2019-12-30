using SpellFire.Well.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.LuaEvents;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	/// <summary>
	/// Autoloots lootable corpses as you stand in loot range
	/// Does respect when you cast or channel spell and it will not interrupt
	/// </summary>
	class AutoLooter : Solution
	{

		private readonly GameObject player;
		private readonly GameObjectManager objectManager;

		private readonly LuaEventListener eventListener;

		public AutoLooter(ControlInterface ci, Memory memory) : base(ci, memory)
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
			Thread.Sleep(200);

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
					Thread.Sleep(300);
				}
			}
		}

		public override void Finish()
		{
			eventListener.Dispose();
		}
	}
}
