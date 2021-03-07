using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2.Behaviours
{
	public class PitOfSaron : BTNode
	{
		private readonly ProdMboxV2 mbox;
		private Client tank;

		private BTNode root;

		private uint PlaguebornHorrorId = 36879;
		private uint IckId = 36476;

		private readonly int[] ToxicWasteIds = { 70276, 70274, 69024 };


		public PitOfSaron(ProdMboxV2 mbox)
		{
			this.mbox = mbox;
			tank = mbox.me;

			root = new Sequence(false, KaI_AddsBehaviour(), KaI_BossFightBehaviour());
		}

		public override BTStatus Execute() => root.Execute();


		/* handling Plagueborn Horror's */
		private BTNode KaI_AddsBehaviour()
		{
			long _guidForCtm = 0;
			var adds = new LeafAction(() =>
			{
				var horrors = tank.ObjectManager.Where(obj =>
					obj.Type == GameObjectType.Unit && obj.EntryID == PlaguebornHorrorId && obj.Health > 0).ToList();

				if (!horrors.Any())
				{
					return BTStatus.Success;
				}

				// attack horrors in combat
				var target = horrors.FirstOrDefault(horror => horror.IsInCombat());
				if (target != null)
				{
					mbox.GroupTargetGuids[0] = target.GUID;
				}

				// step out of puddles (if not tank)
				foreach (var client in mbox.clients.Where(cli => cli != tank))
				{
					if (client.Player.Auras.Select(aura => aura.auraID).Any(auraId => ToxicWasteIds.Contains(auraId)))
					{
						var playerCoords = client.Player.Coordinates;
						var finalTargetCoords = playerCoords + new Vector3(2.5f, 0f, 0f);
						client
							.ControlInterface
							.remoteControl
							.CGPlayer_C__ClickToMove(
								client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
					}
				}


				return BTStatus.Running;
			});

			return adds;
		}

		private BTNode KaI_BossFightBehaviour()
		{
			long _guidForCtm = 0;

			GameObject ick = null;

			var prep = new LeafAction(() =>
			{
				tank.ExecLua("print('KaI bhv ON!')");


				ick = tank.ObjectManager.FirstOrDefault(obj => obj.EntryID == IckId);

				return ick != null ?  BTStatus.Success : BTStatus.Running;
			});

			var bossDeadCheck = new Decorator((() =>
			{
				return ick.Health == 0;

			}), new LeafAction(() =>
			{
				return BTStatus.Success;
			}));

			var nova = new Decorator((() =>
			{
				/* act when Ick casting Poison Nova */
				return ick.CastingSpellId == 68989;

			}), new LeafAction(() =>
			{
				var targetCoords = ick.Coordinates;

				var desiredDistance = 20;

				foreach (var client in mbox.clients)
				{
					var playerCoords = client.Player.Coordinates;
					var diff = (targetCoords - playerCoords);
					var distance = diff.Length();

					if (distance < desiredDistance)
					{
						var adjusted = ((diff * desiredDistance) / distance);
						var finalTargetCoords = targetCoords - adjusted;

						client
							.ControlInterface
							.remoteControl
							.CGPlayer_C__ClickToMove(
								client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
					}
				}

				return BTStatus.Failed;
			}));

			var mines = new Decorator((() =>
			{
				return true;

			}), new LeafAction(() =>
			{
				return BTStatus.Failed;
			}));

			var pursuit = new Decorator((() =>
			{
				/* act when Ick casting Pursuit */
				return ick.CastingSpellId == 68987;

			}), new LeafAction(() =>
			{
				var targetCoords = ick.Coordinates;

				var desiredDistance = 20;

				foreach (var client in mbox.clients)
				{
					var playerCoords = client.Player.Coordinates;
					var diff = (targetCoords - playerCoords);
					var distance = diff.Length();

					if (distance < desiredDistance)
					{
						var adjusted = ((diff * desiredDistance) / distance);
						var finalTargetCoords = targetCoords - adjusted;

						client
							.ControlInterface
							.remoteControl
							.CGPlayer_C__ClickToMove(
								client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
					}
				}

				return BTStatus.Failed;
			}));

			var basic = new Decorator((() =>
			{
				return true;

			}), new LeafAction(() =>
			{
				mbox.GroupTargetGuids[0] = ick.GUID;

				// step out of puddles (if not tank)
				foreach (var client in mbox.clients.Where(cli => cli != tank))
				{
					if (client.Player.Auras.Select(aura => aura.auraID).Any(auraId => ToxicWasteIds.Contains(auraId)))
					{
						var playerCoords = client.Player.Coordinates;
						var finalTargetCoords = playerCoords + new Vector3(2.5f, 0f, 0f);
						client
							.ControlInterface
							.remoteControl
							.CGPlayer_C__ClickToMove(
								client.Player.GetAddress(), ClickToMoveType.Move, ref _guidForCtm, ref finalTargetCoords, 1f);
					}
				}

				return BTStatus.Failed;
			}));

			var stratSelector = new Selector(bossDeadCheck, nova, mines, pursuit, basic);

			return new Sequence(false, prep, stratSelector);
		}
	}
}
