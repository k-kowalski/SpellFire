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

namespace SpellFire.Primer.Solutions.Mbox.ProdV2.Behaviours
{
	public class GenericGroupManager : BTNode
	{
		private readonly ProdMboxV2 mbox;
		private Client tank;
		private IEnumerable<long> protectedPlayersGuids;
		private string tauntAbility;

		private Dictionary<GameObject, long> unitsTargetingProtectedPlayers = new Dictionary<GameObject, long>();
		private List<GameObject> unitsTargetingTank = new List<GameObject>();
		public bool AutoTargetThreateners { get; set; } = true;


		public GenericGroupManager(ProdMboxV2 mbox)
		{
			this.mbox = mbox;
			tank = mbox.me;
			protectedPlayersGuids = mbox.Slaves.Select(c => c.Player.GUID);

			tauntAbility = GetTauntAbilityForTankClass(tank.Player.UnitClass);
		}

		public override BTStatus Execute()
		{
			if (tank.ObjectManager != null)
			{
				SelectThreateningUnits(tank.ObjectManager);

				if (unitsTargetingProtectedPlayers.Any())
				{
					HandleTankReaction();
				}

				if (AutoTargetThreateners)
				{
					var threateners = unitsTargetingProtectedPlayers.Keys.ToList();
					threateners.AddRange(unitsTargetingTank);

					if (threateners.Any())
					{
						SetGroupTargets(threateners);
					}
				}

				unitsTargetingProtectedPlayers.Clear();
				unitsTargetingTank.Clear();
			}

			return BTStatus.Running;
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
			for (sbyte currentMarkIndex = 0; currentMarkIndex < mbox.GroupTargetGuids.Length; currentMarkIndex++)
			{
				var threatener = threateners.ElementAtOrDefault(currThreatenerIndex++);
				if (threatener != null)
				{
					var markedGuid = mbox.GroupTargetGuids[currentMarkIndex];
					if (markedGuid == 0 || !(threateners.Any(t => t.GUID == markedGuid)))
					{
						// mark threatener
						Console.WriteLine($"threatener [{currentMarkIndex}] hp:{threatener.Health} mana:{threatener.ManaPct}");
						mbox.GroupTargetGuids[currentMarkIndex] = threatener.GUID;
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

		public static string GetTauntAbilityForTankClass(UnitClass unitClass) => unitClass switch
		{
			UnitClass.Warrior => "Taunt",
			UnitClass.Paladin => "Hand of Reckoning",
			UnitClass.Druid => "Growl",
			UnitClass.DeathKnight => "Dark Command",
			_ => null,
		};
	}
}
