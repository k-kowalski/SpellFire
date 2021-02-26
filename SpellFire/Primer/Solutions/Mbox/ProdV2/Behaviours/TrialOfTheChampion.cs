using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SpellFire.Well.Controller;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2.Behaviours
{
	public struct PlayerData
	{
		public long warhorseGuid;
		public int originalWeaponId;
	}

	public class TrialOfTheChampion : BehaviourTree
	{
		private const string vehicleName = "Argent Battleworg";
		private readonly string[] eventStarterNames = {
			"Arelas Brightstar", "Jaeren Sunsworn"
		};
		private const int argentLanceId = 46106;
		private const int mainHandSlotId = 16;

		private Dictionary<Client, PlayerData> squadData = new Dictionary<Client, PlayerData>();
		private LinkedList<GameObject> allWarhorses = new LinkedList<GameObject>();

		private bool eventStarted;
		private bool mountedCombatDone;

		private Vector3 gatherPointAfterMounted = new Vector3(744.708f, 600.2399f, 411.5745f);

		private readonly ProdMboxV2 mbox;

		public TrialOfTheChampion(ProdMboxV2 mbox)
		{
			this.mbox = mbox;

			// initialize
			long _guidForCtm = 0;

			allWarhorses = new LinkedList<GameObject>(mbox.me.ObjectManager.Where(obj =>
				obj.Type == GameObjectType.Unit && GetName(mbox.me.ControlInterface, obj) == vehicleName));

			for (int i = 0; i < mbox.clients.Count(); i++)
			{
				var client = mbox.clients.ElementAt(i);
				int originalClientWepId = GetMainHandId(client);
				var associatedVeh = allWarhorses.First.Value;
				allWarhorses.RemoveFirst();
				squadData.Add(client, new PlayerData
				{
					warhorseGuid = associatedVeh.GUID,
					originalWeaponId = originalClientWepId
				});
			}

			var nodes = new List<BTNode>();

			var equipLances = new LeafAction((() =>
			{
				bool allEquipped = true;
				foreach (var entry in squadData)
				{
					entry.Key.ExecLua("EquipItemByName('Argent Lance')");

					allEquipped &= GetMainHandId(entry.Key) == argentLanceId;
				}

				return allEquipped ? BTStatus.Success : BTStatus.Running;
			}));

			var goToVehicles = new LeafAction((() =>
			{
				bool allCloseToVehicles = true;
				foreach (var entry in squadData)
				{
					var target = entry.Key.ObjectManager.First(obj => obj.GUID == entry.Value.warhorseGuid).Coordinates;

					entry.Key
						.ControlInterface
						.remoteControl
						.CGPlayer_C__ClickToMove(
							entry.Key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref target, 1f);

					allCloseToVehicles &= entry.Key.Player.Coordinates.Distance(target) < 5f;
				}

				return allCloseToVehicles ? BTStatus.Success : BTStatus.Running;
			}));

			Func<Client, bool> IsInVehicle = (player) =>
			{
				var vehicleCheck = "canExit = CanExitVehicle()";
				var res = player.ExecLuaAndGetResult(vehicleCheck, "canExit");
				return !String.IsNullOrEmpty(res);
			};

			var enterVehicles = new LeafAction((() =>
			{
				var allEnteredVehicle = true;
				foreach (var entry in squadData)
				{
					var target = entry.Key.ObjectManager.First(obj => obj.GUID == entry.Value.warhorseGuid);

					entry.Key.ControlInterface.remoteControl.InteractUnit(target.GetAddress());

					allEnteredVehicle &= IsInVehicle(entry.Key);
				}

				return allEnteredVehicle ? BTStatus.Success : BTStatus.Running;
			}));

			var goToEventStarter = new LeafAction((() =>
			{
				var allCloseToStarterNPC = true;


				GameObject targetObj = null;
				foreach (var eventStarterName in eventStarterNames)
				{
					targetObj = mbox.me.ObjectManager
						.FirstOrDefault(obj => obj.Type == GameObjectType.Unit && GetName(mbox.me.ControlInterface, obj) == eventStarterName);
					if (targetObj != null)
					{
						break;
					}
				}

				var target = targetObj.Coordinates;

				foreach (var entry in squadData)
				{
					entry.Key
						.ControlInterface
						.remoteControl
						.CGPlayer_C__ClickToMove(
							entry.Key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref target, 1f);


					var veh = entry.Key.ObjectManager.First(obj => obj.GUID == entry.Value.warhorseGuid);
					allCloseToStarterNPC &= veh.Coordinates.Distance(target) < 5f;
				}

				return allCloseToStarterNPC ? BTStatus.Success : BTStatus.Running;
			}));

			mbox.me.LuaEventListener.Bind("GOSSIP_CLOSED", args =>
			{
				eventStarted = true;
				mbox.me.LuaEventListener.Unbind("GOSSIP_CLOSED");
			});

			var startEvent = new LeafAction((() =>
			{
				GameObject targetObj = null;
				foreach (var eventStarterName in eventStarterNames)
				{
					targetObj = mbox.me.ObjectManager
						.FirstOrDefault(obj => obj.Type == GameObjectType.Unit && GetName(mbox.me.ControlInterface, obj) == eventStarterName);
					if (targetObj != null)
					{
						break;
					}
				}

				mbox.me.ControlInterface.remoteControl.InteractUnit(targetObj.GetAddress());

				Thread.Sleep(100);

				// XXX: depends on a server which is available. Prefer 2, cause it can be skip
				mbox.me.ExecLua("SelectGossipOption(2)");
				mbox.me.ExecLua("SelectGossipOption(1)");

				return eventStarted ? BTStatus.Success : BTStatus.Running;
			}));

			var mountedCombat = new LeafAction((() =>
			{
				var allVehiclesDisappeared = true;
				var threateners = SelectThreateningUnits(mbox.me.ControlInterface, mbox.me.ObjectManager);
				foreach (var key in squadData.Keys.ToList())
				{
					key.ExecLua("CastSpellByName('Defend')");
					var myVehicle = key.ObjectManager.FirstOrDefault(obj => obj.GUID == squadData[key].warhorseGuid);

					if (myVehicle == null)
					{
						allVehiclesDisappeared &= true;
						continue;
					}
					else
					{
						allVehiclesDisappeared = false;
					}

					// get to new mount, when low health or warhorse dead
					if (myVehicle.HealthPct < 20)
					{
						if (allWarhorses.Any())
						{
							var myPlayerCoords = Vector3.Zero;
							if (myVehicle.Health == 0)
							{
								myPlayerCoords = key.Player.Coordinates;
							}
							else
							{
								myPlayerCoords = myVehicle.Coordinates;
							}

							var freeClosestWarhorse = allWarhorses.Aggregate(
								(unit1, unit2) => unit1.Coordinates.Distance(myPlayerCoords) < unit2.Coordinates.Distance(myPlayerCoords) ? unit1 : unit2);
							var freeWarhorseCoords = freeClosestWarhorse.Coordinates;

							if (myPlayerCoords.Distance(freeWarhorseCoords) < 3f && !myVehicle.IsMoving())
							{
								var localizedWarhorse =
									key.ObjectManager.First(obj => obj.GUID == freeClosestWarhorse.GUID);
								key.ControlInterface.remoteControl.InteractUnit(localizedWarhorse.GetAddress());
								allWarhorses.Remove(freeClosestWarhorse);

								var entryToModify = squadData[key];
								entryToModify.warhorseGuid = localizedWarhorse.GUID;
								squadData[key] = entryToModify;
							}
							else
							{
								key
									.ControlInterface
									.remoteControl
									.CGPlayer_C__ClickToMove(
										key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref freeWarhorseCoords, 1f);
							}
						}
						else
						{
							Console.WriteLine("No warhorses left!");
						}
						continue;
					}

					if (!threateners.Any())
					{
						continue;
					}
					// get threatener object local to given client obj manager
					var threatener = key.ObjectManager.First(obj => obj.GUID == threateners.First().GUID);

					var myVehicleCoords = myVehicle.Coordinates;
					var target = threatener.Coordinates;

					// XXX: trickery, to get to work targeting mounts and their owners when fighting final mounted Champions
					// this way, players are circling around, trying to use abilities, having Champions targeted
					var shouldBounce = false;
					if (target.x == 0f && target.y == 0f && target.z == 0f)
					{
						target = myVehicleCoords - (Vector3.Random() * 5);
					}
					else if (mbox.me == key)
					{
						// XXX: only bounce, if master, but not on final mounted fight
						shouldBounce = true;
					}

					float dist = target.Distance(myVehicle.Coordinates);
					if (dist > 20)
					{
						// get close, if far
						key
							.ControlInterface
							.remoteControl
							.CGPlayer_C__ClickToMove(
								key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref target, 1f);
					}
					else if (dist < 7)
					{
						key.ExecLua("CastSpellByName('Thrust')");
						if (shouldBounce)
						{
							var bouncePoint = target - (Vector3.Random() * 5);

							key
								.ControlInterface
								.remoteControl
								.CGPlayer_C__ClickToMove(
									key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref bouncePoint, 1f);
						}
					}

					// face
					float angle = myVehicleCoords.AngleBetween(target);
					key.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
						key.Player.GetAddress(), ClickToMoveType.Face, ref _guidForCtm, ref target, angle);

					key.ControlInterface.remoteControl.SelectUnit(threatener.GUID);

					key.ExecLua("CastSpellByName('Charge')");
				}

				return allVehiclesDisappeared ? BTStatus.Success : BTStatus.Running;
			}));

			Func<Client, int, bool> FinishUpCheck = (player, origWepId) =>
			{
				var vehicleCheck = "canExit = CanExitVehicle()";
				var res = player.ExecLuaAndGetResult(vehicleCheck, "canExit");

				var vehicleExited = String.IsNullOrEmpty(res);
				var equippedOriginalItem = GetMainHandId(player) == origWepId;
				var atCenter = player.Player.Coordinates.Distance(gatherPointAfterMounted) < 3f;

				return vehicleExited && equippedOriginalItem && atCenter;
			};

			var finishUp = new LeafAction((() =>
			{
				bool allFinished = true;
				foreach (var entry in squadData)
				{
					entry.Key.ExecLua($"VehicleExit()");
					entry.Key.ExecLua($"EquipItemByName({entry.Value.originalWeaponId})");
					entry.Key
						.ControlInterface
						.remoteControl
						.CGPlayer_C__ClickToMove(
							entry.Key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref gatherPointAfterMounted, 1f);

					allFinished &= FinishUpCheck(entry.Key, entry.Value.originalWeaponId);
				}

				return allFinished ? BTStatus.Success : BTStatus.Running;
			}));

			root = new Sequence(equipLances, goToVehicles, enterVehicles, goToEventStarter, startEvent, mountedCombat, finishUp);
		}

		private int GetMainHandId(Client c)
		{
			return Int32.Parse(c.ExecLuaAndGetResult($"id = GetInventoryItemID('player', {mainHandSlotId})", "id"));
		}

		public string GetName(ControlInterface ci, GameObject unit)
		{
			return ci.remoteControl.GetUnitName(unit.GetAddress());
		}

		private List<GameObject> SelectThreateningUnits(ControlInterface ci, GameObjectManager gameObjMgr)
		{
			var threateners = new List<GameObject>();
			foreach (var gameObj in gameObjMgr)
			{
				if (gameObj.Type == GameObjectType.Unit && gameObj.IsInCombat() && IsAttackableByTank(gameObj) && gameObj.Health > 0)
				{
					var targetedPlayerGuid = squadData.Select(entry => entry.Value.warhorseGuid).FirstOrDefault(g => g == gameObj.TargetGUID);
					if (targetedPlayerGuid != 0 && !GetName(ci, gameObj).Contains("Mount"))
					{
						threateners.Add(gameObj);
					}
				}
			}

			return threateners;
		}

		private bool IsAttackableByTank(GameObject gameObj)
		{
			return mbox.me.ControlInterface.remoteControl.CGUnit_C__UnitReaction(mbox.me.Player.GetAddress(),
				       gameObj.GetAddress()) <= UnitReaction.Neutral;
		}

		public override void Cmd(IList<string> args)
		{
			var rootSequence = root as Sequence;
			rootSequence.MoveCurrentNode(1);
			Console.WriteLine($"Moved sequence by 1 node forward");
		}
	}
}
