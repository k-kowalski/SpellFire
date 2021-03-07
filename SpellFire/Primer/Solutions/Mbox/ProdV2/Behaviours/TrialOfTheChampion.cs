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

	public class TrialOfTheChampion : BTNode
	{
		private Vector3 gatherPoint = new Vector3(744.708f, 600.2399f, 411.5745f);

		private readonly ProdMboxV2 mbox;

		private BTNode root;

		private GenericGroupManager grpMgr;

		private GrandChampions grandChampionsBhv;
		private EadricThePure eadricBhv;
		private ArgentConfessorPaletress paletressBhv;
		private TheBlackKnight tbkBhv;

		private Client tank;

		public TrialOfTheChampion(ProdMboxV2 mbox)
		{
			this.mbox = mbox;

			tank = mbox.me;

			if (!mbox.masterAI)
			{
				mbox.GetCommand("ta").Invoke(mbox.me, new List<string>(new [] {"ma"}));
			}

			if (!mbox.slavesAI)
			{
				mbox.GetCommand("ta").Invoke(mbox.me, new List<string>(new[] { "sl" }));
			}

			if (!mbox.buffingAI)
			{
				mbox.GetCommand("ta").Invoke(mbox.me, new List<string>(new[] { "bu" }));
			}

			grpMgr = new GenericGroupManager(mbox);
			grpMgr.AutoTargetThreateners = false;

			eadricBhv = new EadricThePure(this);
			paletressBhv = new ArgentConfessorPaletress(this);
			tbkBhv = new TheBlackKnight(this);

			var paletress = mbox.me.ObjectManager.FirstOrDefault(obj =>
				obj.Type == GameObjectType.Unit && GetName(mbox.me.ControlInterface, obj) == ArgentConfessorPaletress.PaletressName);

			if (paletress != null)
			{
				Console.WriteLine("Running Paletress");
				root = paletressBhv;
			}
			else
			{
				grandChampionsBhv = new GrandChampions(this);
				Console.WriteLine("Running Champions");
				root = grandChampionsBhv;
			}
		}

		public override BTStatus Execute() => root.Execute();

		public string GetName(ControlInterface ci, GameObject unit)
		{
			return ci.remoteControl.GetUnitName(unit.GetAddress());
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

		public class GrandChampions : BTNode
		{

			private readonly TrialOfTheChampion totch;
			private readonly ProdMboxV2 mbox;
			private BTNode root;

			private const string vehicleName = "Argent Battleworg";
			private readonly string[] eventStarterNames = {"Arelas Brightstar", "Jaeren Sunsworn"};
			private const int argentLanceId = 46106;
			private const int mainHandSlotId = 16;

			private Dictionary<Client, PlayerData> squadData = new Dictionary<Client, PlayerData>();
			private LinkedList<GameObject> allWarhorses = new LinkedList<GameObject>();

			private bool eventStarted;


			private const int BladestormId = 67541;
			private readonly int[] PoisonBottleIds = { 68316, 67594};



			private readonly string[,] GrandChampionsNames = {
				{"Marshal Jacob Alerius", "Mokra the Skullcrusher"},
				{"Ambrose Boltspark", "Eressea Dawnsinger"},
				{"Jaelyne Evensong", "Zul'tore"},
				{"Lana Stouthammer", "Deathstalker Visceri"},
				{"Colosos", "Runok Wildmane"},
			};

			private List<GameObject> GrandChampionsUnits = new List<GameObject>(3);

			private const string drink = "Honeymint Tea";

			public GrandChampions(TrialOfTheChampion totch)
			{
				this.totch = totch;
				this.mbox = totch.mbox;

				root = new Sequence(false, MountedCombatBehaviour(), NormalCombatBehaviour());
			}

			private BTNode NormalCombatBehaviour()
			{
				var nodes = new List<BTNode>();

				var getTargets = new LeafAction((() =>
				{
					mbox.me.ExecLua("print('Running normal combat now')");
					var units = mbox.me.ObjectManager.Where(obj => obj.Type == GameObjectType.Unit)
						.Select(unit => new Tuple<string, GameObject>(totch.GetName(mbox.me.ControlInterface, unit), unit));
					foreach (var championName in GrandChampionsNames)
					{
						foreach (var unit in units)
						{
							if (unit.Item1 == championName)
							{
								GrandChampionsUnits.Add(unit.Item2);
							}
						}
					}
					

					return GrandChampionsUnits.Count == 3 ? BTStatus.Success : BTStatus.Running;
				}));
				nodes.Add(getTargets);

				var prepare = new LeafAction((() =>
				{
					var needsRegen = false;
					foreach (var client in mbox.clients)
					{
						// if have mana, drink until regenerated
						if (client.Player.MaxMana != 0)
						{
							if (client.Player.ManaPct < 70)
							{
								needsRegen = true;
								if (!mbox.me.HasAura(client.Player, "Drink"))
								{
									client.ExecLua(mbox.UtilScript);
									client.ExecLua(
										$"filter = function(itemName) return itemName == \"{drink}\" end;" +
										$"sfUseInventoryItem(filter)");
								}
							}
						}
					}

					if (needsRegen)
					{
						return BTStatus.Running;
					}

					return BTStatus.Success;
				}));
				nodes.Add(prepare);

				long _guidForCtm = 0;
				var combat = new LeafAction((() =>
				{
					// target champion
					GameObject currentlyTargetedChampion = null;
					foreach (var champion in GrandChampionsUnits)
					{
						if (champion.HealthPct > 0)
						{
							currentlyTargetedChampion = champion;
							mbox.me.ControlInterface.remoteControl.SelectUnit(champion.GUID);
							mbox.GroupTargetGuids[0] = champion.GUID;
							break;
						}
					}

					if (currentlyTargetedChampion == null)
					{
						return BTStatus.Success;
					}


					var targetCoords = currentlyTargetedChampion.Coordinates;

					// check for Bladestorm
					GameObject bladestormingChampion = null;
					var bladestormCoords = Vector3.Zero;
					foreach (var champion in GrandChampionsUnits)
					{
						if (champion.HealthPct > 0)
						{
							foreach (var aura in champion.Auras)
							{
								if (aura.auraID == BladestormId)
								{
									bladestormingChampion = champion;
									bladestormCoords = bladestormingChampion.Coordinates;
									break;
								}
							}
						}
					}

					// aggregate threateners
					_ = totch.grpMgr.Execute();

					float getCloseDist = 30;
					float runAwayFromBsDist = 14;
					foreach (var client in mbox.clients)
					{
						var playerCoords = client.Player.Coordinates;
						var diff = (targetCoords - playerCoords);
						var distance = diff.Length();

						// if not tank
						if (client != totch.tank)
						{
							// run away from Bladestorm
							if (bladestormingChampion != null)
							{
								diff = (bladestormCoords - playerCoords);
								distance = diff.Length();

								if (distance <= 6)
								{
									var adjusted = ((diff * runAwayFromBsDist) / distance);
									var finalTargetCoords = targetCoords - adjusted;
									client
										.ControlInterface
										.remoteControl
										.CGPlayer_C__ClickToMove(
											client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
									break;
								}
							}

							// step out of Poison
							if (client.Player.Auras.Select(aura => aura.auraID).Any(auraId => PoisonBottleIds.Contains(auraId)))
							{
								var finalTargetCoords = playerCoords + new Vector3(5f, 0f, 0f);
								client
									.ControlInterface
									.remoteControl
									.CGPlayer_C__ClickToMove(
										client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
								continue;
							}

							// get close, if far
							if (distance > getCloseDist)
							{
								var adjusted = ((diff * getCloseDist) / distance);
								var finalTargetCoords = targetCoords - adjusted;

								client
									.ControlInterface
									.remoteControl
									.CGPlayer_C__ClickToMove(
										client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
							}
						}
						else
						{
							var tankDist = 4;
							// as tank, always sit on target
							var adjusted = ((diff * tankDist) / distance);
							var finalTargetCoords = targetCoords - adjusted;

							client
								.ControlInterface
								.remoteControl
								.CGPlayer_C__ClickToMove(
									client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
						}
					}

					return BTStatus.Running;
				}));
				nodes.Add(combat);

				return new Sequence(false, nodes.ToArray());
			}

			private BTNode MountedCombatBehaviour()
			{
				var nodes = new List<BTNode>();
				long _guidForCtm = 0;

				allWarhorses = new LinkedList<GameObject>(mbox.me.ObjectManager.Where(obj =>
					obj.Type == GameObjectType.Unit && totch.GetName(mbox.me.ControlInterface, obj) == vehicleName));
				int[] defendSpellIds = new[] { 62552 };

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

				var equipLances = new LeafAction((() =>
				{
					mbox.me.ExecLua("print('Running mounted combat now')");
					bool allEquipped = true;
					foreach (var entry in squadData)
					{
						entry.Key.ExecLua("EquipItemByName('Argent Lance')");

						allEquipped &= GetMainHandId(entry.Key) == argentLanceId;
					}

					return allEquipped ? BTStatus.Success : BTStatus.Running;
				}));
				nodes.Add(equipLances);

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
				nodes.Add(goToVehicles);

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
				nodes.Add(enterVehicles);

				var goToEventStarter = new LeafAction((() =>
				{
					var allCloseToStarterNPC = true;


					GameObject targetObj = null;
					foreach (var eventStarterName in eventStarterNames)
					{
						targetObj = mbox.me.ObjectManager
							.FirstOrDefault(obj => obj.Type == GameObjectType.Unit && totch.GetName(mbox.me.ControlInterface, obj) == eventStarterName);
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
				nodes.Add(goToEventStarter);

				mbox.me.LuaEventListener.Bind("GOSSIP_CLOSED", args =>
				{
					eventStarted = true;
					mbox.me.LuaEventListener.Unbind("GOSSIP_CLOSED");
				});

				GameObject targetObj = null;
				var startEvent = new LeafAction((() =>
				{
					foreach (var eventStarterName in eventStarterNames)
					{
						if (targetObj != null)
						{
							break;
						}

						targetObj = mbox.me.ObjectManager
							.FirstOrDefault(obj => obj.Type == GameObjectType.Unit && totch.GetName(mbox.me.ControlInterface, obj) == eventStarterName);
					}

					mbox.me.ControlInterface.remoteControl.InteractUnit(targetObj.GetAddress());

					Thread.Sleep(100);

					mbox.me.ExecLua("SelectGossipOption(2)");

					return eventStarted ? BTStatus.Success : BTStatus.Running;
				}));
				nodes.Add(startEvent);

				var mountedCombat = new LeafAction((() =>
				{
					var allVehiclesDisappeared = true;
					var threateners = SelectThreateningUnitsForMountedCombat(mbox.me.ControlInterface, mbox.me.ObjectManager);
					foreach (var key in squadData.Keys.ToList())
					{
						var myVehicle = key.ObjectManager.FirstOrDefault(obj => obj.GUID == squadData[key].warhorseGuid);
						if (myVehicle == null)
						{
							continue;
						}
						else
						{
							allVehiclesDisappeared = false;
						}





						// ensure 3 stacks of Defend
						var myVehicleAuras = myVehicle.Auras;
						Aura? defendAura = null;
						foreach (var aura in myVehicleAuras)
						{
							if (defendSpellIds.Contains(aura.auraID))
							{
								defendAura = aura;
								break;
							}
						}

						if (defendAura == null || defendAura.Value.stackCount != 3)
						{
							key.ExecLua("CastSpellByName('Defend')");
						}


						// get to new mount, when low health or warhorse dead
						if (myVehicle.HealthPct < 35)
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

						// always try to go to gather point
						target = totch.gatherPoint;

						float dist = target.Distance(myVehicle.Coordinates);
						if (dist > 24)
						{
							// get close, if far
							key
								.ControlInterface
								.remoteControl
								.CGPlayer_C__ClickToMove(
									key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref target, 1f);
						}

						var bouncePoint = target - (Vector3.Random() * 5);

						key
							.ControlInterface
							.remoteControl
							.CGPlayer_C__ClickToMove(
								key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref bouncePoint, 1f);

						// face
						float angle = myVehicleCoords.AngleBetween(target);
						key.ControlInterface.remoteControl.CGPlayer_C__ClickToMove(
							key.Player.GetAddress(), ClickToMoveType.Face, ref _guidForCtm, ref target, angle);

						key.ControlInterface.remoteControl.SelectUnit(threatener.GUID);

						key.ExecLua("CastSpellByName('Charge')");
						key.ExecLua("CastSpellByName('Thrust')");
					}

					return allVehiclesDisappeared ? BTStatus.Success : BTStatus.Running;
				}));
				nodes.Add(mountedCombat);

				Func<Client, int, bool> FinishUpCheck = (player, origWepId) =>
				{
					var vehicleCheck = "canExit = CanExitVehicle()";
					var res = player.ExecLuaAndGetResult(vehicleCheck, "canExit");

					var vehicleExited = String.IsNullOrEmpty(res);
					var equippedOriginalItem = GetMainHandId(player) == origWepId;
					var atCenter = player.Player.Coordinates.Distance(totch.gatherPoint) < 3f;

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
								entry.Key.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref totch.gatherPoint, 1f);

						allFinished &= FinishUpCheck(entry.Key, entry.Value.originalWeaponId);
					}

					return allFinished ? BTStatus.Success : BTStatus.Running;
				}));
				nodes.Add(finishUp);

				return new Sequence(false, nodes.ToArray());
			}
			private int GetMainHandId(Client c)
			{
				return Int32.Parse(c.ExecLuaAndGetResult($"id = GetInventoryItemID('player', {mainHandSlotId})", "id"));
			}

			private List<GameObject> SelectThreateningUnitsForMountedCombat(ControlInterface ci, GameObjectManager gameObjMgr)
			{
				var threateners = new List<GameObject>();
				foreach (var gameObj in gameObjMgr)
				{
					if (gameObj.Type == GameObjectType.Unit && gameObj.IsInCombat() && totch.IsAttackableByTank(gameObj) && gameObj.Health > 0)
					{
						var targetedPlayerGuid = squadData.Select(entry => entry.Value.warhorseGuid).FirstOrDefault(g => g == gameObj.TargetGUID);
						if (targetedPlayerGuid != 0 && !totch.GetName(ci, gameObj).Contains("Mount"))
						{
							threateners.Add(gameObj);
						}
					}
				}

				return threateners;
			}

			public override BTStatus Execute() => root.Execute();
		}

		public class EadricThePure : BTNode
		{
			private readonly TrialOfTheChampion totch;

			public EadricThePure(TrialOfTheChampion totch)
			{
				this.totch = totch;
			}


			public override BTStatus Execute()
			{
				throw new NotImplementedException();
			}
		}

		public class ArgentConfessorPaletress : BTNode
		{
			private readonly TrialOfTheChampion totch;
			private readonly ProdMboxV2 mbox;
			private BTNode root;

			public const string PaletressName = "Argent Confessor Paletress";
			GameObject paletress = null;

			long paletressGUID;
			private const int TremorTotem = 5913;

			private static readonly int[] PaletressDebuffIds =
			{
				66619, //SPELL_SHADOWS_PAST
				67678, //SPELL_SHADOWS_PAST_H
				66538, //SPELL_HOLY_FIRE
				67676, //SPELL_HOLY_FIRE_H

				// Argent Priestess's Shadow Word: Pain
				34941,
				34942,
			};

			private static readonly int[] PaletressRenewIds =
			{
				66537, //SPELL_RENEW
				67675, //SPELL_RENEW_H
			};
			
			private static readonly int[] ArgentAdds =
			{
				35307, //NPC_PRIESTESS
				35309, //NPC_ARGENT_LIGHWIELDER
				35305, //NPC_ARGENT_MONK
			};



			private static readonly int PaletressReflectiveShield = 66515;

			public ArgentConfessorPaletress(TrialOfTheChampion totch)
			{
				this.totch = totch;
				this.mbox = totch.mbox;

				root = new Sequence(true, ConfessorPaletressBehaviour().ToArray());
			}

			private List<BTNode> ConfessorPaletressBehaviour()
			{
				var nodes = new List<BTNode>();

				GameObject memoryMonster = null;

				var shaman = mbox.clients.FirstOrDefault(client =>
					client.Player.UnitClass == UnitClass.Shaman);
				var pala = mbox.clients.FirstOrDefault(client =>
					client.Player.UnitClass == UnitClass.Paladin);
				var priest = mbox.clients.FirstOrDefault(client =>
					client.Player.UnitClass == UnitClass.Priest);

				var common = new LeafAction((() =>
				{
					if (paletress == null)
					{
						paletress = mbox.me.ObjectManager.FirstOrDefault(obj =>
							obj.Type == GameObjectType.Unit && totch.GetName(mbox.me.ControlInterface, obj) == PaletressName);

						if (paletress != null)
						{
							paletressGUID = paletress.GUID;
						}
					}

					// ensure tremor totem
					if (!mbox.me.ObjectManager.Any(obj => obj.EntryID == TremorTotem))
					{
						if (shaman != null && shaman.Player.HealthPct > 0)
						{
							shaman.EnqueuePrioritySpellCast(
								new SpellCast
								{
									Coordinates = null,
									SpellName = "Tremor Totem",
									TargetGUID = 0
								}
							);
							Thread.Sleep(1000);
						}
					}

					// cleanse Paletress's debuffs
					int lowPlayerCount = 0;
					foreach (var player in mbox.clients.Select(cli => cli.Player).Where(pla => pla.HealthPct > 0))
					{
						if (player.HealthPct < 50)
						{
							lowPlayerCount++;
						}

						foreach (var aura in player.Auras)
						{
							if (PaletressDebuffIds.Contains(aura.auraID))
							{
								if (pala != null && pala.Player.HealthPct > 0)
								{
									pala.EnqueuePrioritySpellCast(
										new SpellCast
										{
											Coordinates = null,
											SpellName = "Cleanse",
											TargetGUID = player.GUID
										}
									);
									break;
								}
							}
						}
					}

					if (lowPlayerCount >= 2)
					{
						if (pala != null && pala.Player.HealthPct > 0)
						{
							if (!pala.IsOnCooldown("Divine Sacrifice"))
							{
								pala.EnqueuePrioritySpellCast(
									new SpellCast
									{
										Coordinates = null,
										SpellName = "Divine Sacrifice",
										TargetGUID = 0
									}
								);
							}
						}
					}

					return BTStatus.Success;
				}));

				// Paletress disappeared - end condition
				var paletressGone =
					new Decorator(
					(() => { return !mbox.me.ObjectManager.Any(obj => obj.GUID == paletressGUID); }),
					new LeafAction((() =>
					{
						return BTStatus.Success;
					})));

				var argentAdds =
					new Decorator(
					(() => { return !totch.IsAttackableByTank(paletress); }),
					new LeafAction((() =>
					{

						foreach (var argentAdd in ArgentAdds)
						{
							foreach (var unit in mbox.me.ObjectManager.Where(obj => obj.Type == GameObjectType.Unit && obj.IsInCombat()))
							{
								if (argentAdd == unit.EntryID)
								{
									mbox.GroupTargetGuids[0] = unit.GUID;
									goto end;
								}
							}
						}

						
						end:
						return BTStatus.Failed;
					})));

				var memoryAsTarget = new Decorator((() =>
				{
					return paletress.Auras.Select(aura => aura.auraID).Contains(PaletressReflectiveShield);
				}), new LeafAction((() =>
				{
					if (memoryMonster != null)
					{
						mbox.GroupTargetGuids[0] = memoryMonster.GUID;
						mbox.me.ControlInterface.remoteControl.SelectUnit(memoryMonster.GUID);
						mbox.me.CastSpellOnGuid(GenericGroupManager.GetTauntAbilityForTankClass(mbox.me.Player.UnitClass), memoryMonster.GUID);

						foreach (var aura in memoryMonster.Auras)
						{
							if (PaletressRenewIds.Contains(aura.auraID))
							{
								if (priest != null && priest.Player.HealthPct > 0)
								{
									priest.EnqueuePrioritySpellCast(
										new SpellCast
										{
											Coordinates = null,
											SpellName = "Dispel Magic",
											TargetGUID = memoryMonster.GUID
										}
									);
									break;
								}
							}
						}

						if (priest != null && priest.Player.HealthPct > 0)
						{
							if (!priest.IsOnCooldown("Power Infusion"))
							{
								priest.EnqueuePrioritySpellCast(
									new SpellCast
									{
										Coordinates = null,
										SpellName = "Power Infusion",
										TargetGUID = priest.Player.GUID
									}
								);
							}
						}

						if (shaman != null && shaman.Player.HealthPct > 0)
						{
							if (!shaman.IsOnCooldown("Bloodlust"))
							{
								shaman.EnqueuePrioritySpellCast(
									new SpellCast
									{
										Coordinates = null,
										SpellName = "Bloodlust",
										TargetGUID = 0
									}
								);
							}
						}
					}
					else
					{
						mbox.GroupTargetGuids[0] = 0;
						memoryMonster = mbox.me.ObjectManager.FirstOrDefault(obj =>
							obj.Type == GameObjectType.Unit && totch.GetName(mbox.me.ControlInterface, obj).Contains("Memory"));
					}

					return BTStatus.Failed;
				})));


				var paletressAsTarget = new Decorator((() =>
				{
					return totch.IsAttackableByTank(paletress) && !paletress.Auras.Select(aura => aura.auraID).Contains(PaletressReflectiveShield);
				}), new LeafAction((() =>
				{
					mbox.GroupTargetGuids[0] = paletress.GUID;
					mbox.me.ControlInterface.remoteControl.SelectUnit(paletress.GUID);
					mbox.me.CastSpellOnGuid(GenericGroupManager.GetTauntAbilityForTankClass(mbox.me.Player.UnitClass), paletress.GUID);

					foreach (var aura in paletress.Auras)
					{
						if (PaletressRenewIds.Contains(aura.auraID))
						{
							var priest = mbox.clients.FirstOrDefault(client =>
								client.Player.UnitClass == UnitClass.Priest);
							if (priest != null && priest.Player.HealthPct > 0)
							{
								priest.EnqueuePrioritySpellCast(
									new SpellCast
									{
										Coordinates = null,
										SpellName = "Dispel Magic",
										TargetGUID = paletress.GUID
									}
								);
								break;
							}
						}
					}

					return BTStatus.Failed;
				})));


				var select = new Selector(paletressGone, argentAdds, memoryAsTarget, paletressAsTarget);

				nodes.Add(common);
				nodes.Add(select);


				return nodes;
			}

			public override BTStatus Execute() => root.Execute();
		}

		public class TheBlackKnight : BTNode
		{
			private readonly TrialOfTheChampion totch;

			public TheBlackKnight(TrialOfTheChampion totch)
			{
				this.totch = totch;
			}


			public override BTStatus Execute()
			{
				throw new NotImplementedException();
			}
		}
	}
}
