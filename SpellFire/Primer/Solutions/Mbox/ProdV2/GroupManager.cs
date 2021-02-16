using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpellFire.Well.Model;
using SpellFire.Well.Navigation;
using SpellFire.Well.Net;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2
{
	public class GroupManager
	{
		private readonly ProdMboxV2 mbox;
		private Client tank;
		private IEnumerable<long> protectedPlayersGuids;
		private string tauntAbility;

		private Dictionary<GameObject, long> unitsTargetingProtectedPlayers = new Dictionary<GameObject, long>();
		private List<GameObject> unitsTargetingTank = new List<GameObject>();


		public Int64[] GroupTargetGuids = new long[10];

		public WaypointMap wpMap;


		public GroupManager(ProdMboxV2 mbox)
		{
			this.mbox = mbox;
			tank = mbox.me;
			protectedPlayersGuids = mbox.Slaves.Select(c => c.Player.GUID);

			tauntAbility = GetTauntAbilityForTankClass(tank.Player.UnitClass);
			navEngine = new NavigationEngine();
		}

		public void ManageGroup()
		{
			SelectThreateningUnits(tank.ObjectManager);

			if (unitsTargetingProtectedPlayers.Any())
			{
				HandleTankReaction();
			}

			var threateners = unitsTargetingProtectedPlayers.Keys.ToList();
			threateners.AddRange(unitsTargetingTank);

			if (threateners.Any())
			{
				canGroupNavigate = false;
				SetGroupTargets(threateners);
			}
			else
			{
				canGroupNavigate = true;
			}

			unitsTargetingProtectedPlayers.Clear();
			unitsTargetingTank.Clear();
		}

		private void HandleTankReaction()
		{
			/* taunt if possible */
			TryTaunting(unitsTargetingProtectedPlayers.Keys.ElementAt(0));

			var tankCoords = tank.Player.Coordinates;
			var closestThreateningUnit = unitsTargetingProtectedPlayers.Aggregate(
				(unit1, unit2) => unit1.Key.Coordinates.Distance(tankCoords) < unit2.Key.Coordinates.Distance(tankCoords) ? unit1 : unit2).Key;

			Console.WriteLine($"Detected {tank.ControlInterface.remoteControl.GetUnitName(closestThreateningUnit.GetAddress())} out of {unitsTargetingProtectedPlayers.Count}");
			/* set unit as tank's target, if in tank's range */
			float dist = closestThreateningUnit.Coordinates.Distance(tankCoords);
			if (dist <= ProdMboxV2.MeleeAttackRange)
			{
				if (tank.GetTargetGUID() != closestThreateningUnit.GUID)
				{
					tank.ControlInterface.remoteControl.SelectUnit(closestThreateningUnit.GUID);
				}
			}
		}

		private void SetGroupTargets(List<GameObject> threateners)
		{
			int currThreatenerIndex = 0;
			for (sbyte currentMarkIndex = 0; currentMarkIndex < GroupTargetGuids.Length; currentMarkIndex++)
			{
				var threatener = threateners.ElementAtOrDefault(currThreatenerIndex++);
				if (threatener != null)
				{
					var markedGuid = GroupTargetGuids[currentMarkIndex];
					if (markedGuid == 0 || !(threateners.Any(t => t.GUID == markedGuid)))
					{
						// mark threatener
						Console.WriteLine($"threatener [{currentMarkIndex}] hp:{threatener.Health} mana:{threatener.ManaPct}");
						GroupTargetGuids[currentMarkIndex] = threatener.GUID;
					}
				}
				else
				{
					break;
				}
			}
		}

		private void SelectThreateningUnits(GameObjectManager gameObjMgr)
		{
			foreach (var gameObj in gameObjMgr)
			{
				if (gameObj.Type == GameObjectType.Unit && gameObj.IsInCombat() && IsAttackableByTank(gameObj))
				{
					var targetedPlayerGuid = protectedPlayersGuids.FirstOrDefault(g => g == gameObj.TargetGUID);
					if (targetedPlayerGuid != 0)
					{
						unitsTargetingProtectedPlayers.Add(gameObj, targetedPlayerGuid);
					}
					else if (gameObj.TargetGUID == tank.Player.GUID)
					{
						unitsTargetingTank.Add(gameObj);
					}
				}
			}
		}

		private bool IsAttackableByTank(GameObject gameObj)
		{
			return tank.ControlInterface.remoteControl.CGUnit_C__UnitReaction(tank.Player.GetAddress(),
				       gameObj.GetAddress()) <= UnitReaction.Neutral;
		}

		private void TryTaunting(GameObject target)
		{
			if (!tank.IsOnCooldown(tauntAbility))
			{
				tank.EnqueuePrioritySpellCast(new SpellCast
				{
					Coordinates = null,
					SpellName = tauntAbility,
					TargetGUID = target.GUID
				});
				Console.WriteLine($"Taunting {tank.ControlInterface.remoteControl.GetUnitName(target.GetAddress())}");
			}
		}

		private static string GetTauntAbilityForTankClass(UnitClass unitClass) => unitClass switch
		{
			UnitClass.Warrior => "Taunt",
			UnitClass.Paladin => "Hand of Reckoning",
			UnitClass.Druid => "Growl",
			UnitClass.DeathKnight => "Dark Command",
			_ => null,
		};


















		IEnumerable<Client> navigableClients;
		NavigationEngine navEngine;
		private bool canGroupNavigate;

		private bool isTraversing;
		private Task traversalTask;

		public void HandleNavigationCommand(IList<string> args)
		{
			var subcmd = args[0];
			switch (subcmd)
			{
				case "run":
					var path = args[1];
					wpMap = JsonConvert.DeserializeObject<WaypointMap>(File.ReadAllText(path));

					// consider clients only on the same map
					navigableClients =
						mbox.clients.Where(c => c.Memory.ReadInt32(IntPtr.Zero + Offset.MapId) == wpMap.mapId).ToList();
					if (!navigableClients.Contains(mbox.me))
					{
						Console.WriteLine($"Master is required to be on navigation map");
						return;
					}

					Console.WriteLine($"Detected {navigableClients.Count()} clients");

					if (!navEngine.SetCurrentMap(wpMap.mapId))
					{
						Console.WriteLine($"Couldn't load map for map id {wpMap.mapId}");
						return;
					}

;
					traversalTask = Task.Run((() =>
					{
						// gather at master's position
						while (!navigableClients.All(c => c.Navigate(mbox.me.Player.Coordinates)))
						{
							Thread.Sleep(1000);
						}

						// follow waypoints
						foreach (var nextWp in wpMap.waypoints)
						{
							isTraversing = true;
							Console.WriteLine($"Next wp is {nextWp}");
							while (true)
							{
								if (!isTraversing)
								{
									Console.WriteLine("Traversing has been interrupted.");
									return;
								}

								if (canGroupNavigate)
								{
									var notYetArrivedClients =
										navigableClients.Where(c => c.Player.Coordinates.Distance(nextWp) > 1f);
									if (!notYetArrivedClients.Any())
									{
										break;
									}

									foreach (var notYetArrivedClient in notYetArrivedClients)
									{
										var start = notYetArrivedClient.Player.Coordinates;
										var next = navEngine.GetNextPathNode(start, nextWp);
										if (next != null)
										{
											notYetArrivedClient.Navigate(next.Value);
										}
									}
								}

								Thread.Sleep(1000);
							}
						}
						Console.WriteLine("Successfully traversed path.");
					}));

					break;
				case "stop":
					Console.WriteLine("Stopping traversal.");
					isTraversing = false;
					traversalTask?.Wait();
					break;
				default:
					break;
			}
		}
	}
}
