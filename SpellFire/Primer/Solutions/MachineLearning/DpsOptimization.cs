using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.MachineLearning
{
	/// <summary>
	///
	/// Solution uses Reinforcement Learning techniques
	/// in order to learn best ability rotation to achieve highest Damage Per Second(DPS)
	///
	/// Algorithm: Classic Q-Learning
	/// </summary>
	public class DpsOptimization : Solution
	{
		private const string TrainingTargetName = "Grandmaster's Training Dummy";

		private ControlInterface ci;



		private int TotalDamage;
		private DateTime start;
		private ActionPool actionPool;
		private float[,] QMatrix;

		private bool isTraining = true;

		private EnvironmentState currentState;

		private const float LearningRate = 0.007f;
		private const float DiscountFactor = 0.15f;


		private int previousDps;

		public DpsOptimization(Client client) : base(client)
		{
			ci = client.ControlInterface;

			me.LuaEventListener.Bind("SWING_DAMAGE", DamageEvent);
			me.LuaEventListener.Bind("RANGE_DAMAGE", DamageEvent);
			me.LuaEventListener.Bind("SPELL_DAMAGE", DamageEvent);
			me.LuaEventListener.Bind("SPELL_PERIODIC_DAMAGE", DamageEvent);
			me.LuaEventListener.Bind("DAMAGE_SHIELD", DamageEvent);
			me.LuaEventListener.Bind("DAMAGE_SPLIT", DamageEvent);

			me.LuaEventListener.Bind("do", ControlEvent);


			/*
				turn on no-cost spells
				assume training agent has GM powers(training on own server emulator)
			*/
			ci.remoteControl.FrameScript__Execute("SendChatMessage('.cheat power on')",0,0);

			actionPool = new ActionPool(me);

			QMatrix = new float[
				EnvironmentState.GetTotalStateCount(),
				actionPool.ActionCount()
			];

			ResetSession();
			Console.WriteLine("Running.");
			this.Active = true;

			/*
			 * perform initial random action
			 * initial state is needed to not perform actions prohibited by state
			 */
			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}
			GameObject target = SelectTrainingTarget();
			if (target == null)
			{
				return;
			}

			/* initialize learning */
			currentState = GetState(target);
		}
		
		private void PrintCurrentQMatrix()
		{
			Console.WriteLine($"QMatrix: ");
			for (int i = 0; i < QMatrix.GetLength(0); i++)
			{
				Console.Write($"{Convert.ToString(i,2).PadLeft(5, '0')}:\t");
				for (int j = 0; j < QMatrix.GetLength(1); j++)
				{
					Console.Write($"{QMatrix[i,j], -10} ");
				}
				Console.WriteLine();
			}
		}

		private void DamageEvent(LuaEventArgs args)
		{
			long srcGuid = Convert.ToInt64(args.Args[2], 16);
			if (srcGuid == me.Player.GUID)
			{
				lock (this)
				{
					TotalDamage += Int32.Parse(args.Args[11]);
				}
			}
		}

		private void ControlEvent(LuaEventArgs args)
		{
			if (args.Args[0] == "mode")
			{
				isTraining = !isTraining;
				string status = isTraining ? "Train" : "Test";
				Console.WriteLine($"Mode: {status}");

				if (isTraining)
				{
					ResetSession();
				}
			}
		}

		public override void Tick()
		{
			Thread.Sleep(10);
			me.RefreshLastHardwareEvent();

			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}

			if (isTraining)
			{
				Train();
			}
			else
			{
				Execute();
			}
		}

		private void Train()
		{
			if (!Ready())
			{
				return;
			}

			GameObject target = SelectTrainingTarget();
			if (target == null)
			{
				return;
			}







			var currentAction = actionPool.GetViableRandomAction(currentState);
			currentAction?.Perform();
			/*
			 * in order to see what next state is, perform action
			 *
			 * wait, so action can be performed in game world
			 * and it's impact on both reward and state can be registered
			 */
			do
			{
				Thread.Sleep(200);
			} while (!Ready());
			Thread.Sleep(1000);

			var newState = GetState(target);
			var reward = GetReward();
			Console.WriteLine("--- Current state ---");
			Console.WriteLine(currentState);
			Console.WriteLine("--- New state ---");
			Console.WriteLine(newState);
			Console.WriteLine($"Reward: {reward}");

			/* update Q-Matrix */
			float qValueOld = QMatrix[currentState.ToInt(), currentAction.Id];
			float qValue =
				qValueOld +
				LearningRate * (reward + DiscountFactor * GetBestActionForState(newState).Item2 - qValueOld);

			QMatrix[currentState.ToInt(), currentAction.Id] = qValue;
			PrintCurrentQMatrix();

			currentState = newState;
		}

		private Tuple<Action, float> GetBestActionForState(EnvironmentState state)
		{
			int stateId = state.ToInt();
			float qValueMax = Single.MinValue;

			int bestActionId = 0;

			for (int i = 0; i < actionPool.ActionCount(); i++)
			{
				var qValue = QMatrix[stateId, i];
				if (qValue > qValueMax)
				{
					qValueMax = qValue;
					bestActionId = i;
				}
			}

			return new Tuple<Action, float>(
				actionPool.GetAction(bestActionId),
				qValueMax);
		}

		private void Execute()
		{
			if (!Ready())
			{
				return;
			}

			GameObject target = SelectTrainingTarget();
			if (target == null)
			{
				return;
			}

			var state = GetState(target);
			Console.WriteLine("--- For state ---");
			Console.WriteLine(state);
			PrintCurrentQMatrix();

			var action = GetBestActionForState(state).Item1;
			action?.Perform();
		}

		private bool Ready()
		{
			return !me.IsOnCooldown(GcdCheckSpell) && !me.Player.IsCastingOrChanneling();
		}

		private void ResetSession()
		{
			Console.WriteLine($"[{DateTime.Now}] SESSION RESET");
			Console.WriteLine($"Total damage done: {TotalDamage}");

			/* clear QMatrix */
			for (int i = 0; i < QMatrix.GetLength(0); i++)
			{
				for (int j = 0; j < QMatrix.GetLength(1); j++)
				{
					QMatrix[i, j] = 0.0f;
				}
			}

			TotalDamage = 0;
			start = DateTime.Now;
		}

		private GameObject SelectTrainingTarget()
		{
			GameObject target = me.ObjectManager.FirstOrDefault(obj =>
			{
				var objName = ci.remoteControl.GetUnitName(obj.GetAddress());
				return objName == TrainingTargetName;
			});

			if (target == null)
			{
				Console.WriteLine($"No training target found!");
				ResetSession();
			}
			else
			{
				ci.remoteControl.SelectUnit(target.GUID);
			}

			return target;
		}

		public override void Dispose()
		{
			me.LuaEventListener.Dispose();
		}

		private int GetCurrentDamagePerSecond()
		{
			lock (this)
			{
				var elapsedSeconds = (int)(DateTime.Now - start).TotalSeconds;
				if (elapsedSeconds == 0)
				{
					return 0;
				}
				return TotalDamage / elapsedSeconds;
			}
		}

		private int GetReward()
		{
			var currentDps = GetCurrentDamagePerSecond();
			if (previousDps == 0)
			{
				previousDps = currentDps;
			}
			var dpsDiff = currentDps - previousDps;
			previousDps = currentDps;
			return dpsDiff;
		}

		private class ActionPool
		{
			private IList<Action> actions = new List<Action>();

			public ActionPool(Client performer)
			{
				/* include desired actions */
				var actionTypes = new[]
				{
					typeof(WrathAction),
					typeof(StarfireAction),
				};

				int actionId = 0;
				foreach (var actionType in actionTypes)
				{
					Action action = Activator.CreateInstance(actionType, actionId, performer) as Action;

					actions.Add(action);
					Console.WriteLine($"Added action {action.Name} with id {actionId}");

					actionId++;
				}
			}

			public Action GetViableRandomAction(EnvironmentState state)
			{
				Action action = null;

				List<Action> untriedActions = new List<Action>(actions);

				/* select random action that can be performed in given state */
				do
				{
					if (untriedActions.Count == 0)
					{
						Console.WriteLine("All action possibilities exhausted!");
						break;
					}

					action = untriedActions[SFUtil.RandomGenerator.Next(0, untriedActions.Count)];
					untriedActions.Remove(action);
				} while (!action.CanPerform(state));

				return action;
			}

			public int ActionCount()
			{
				return actions.Count;
			}

			public Action GetAction(int id)
			{
				return actions.First(action => action.Id == id);
			}
		}

		#region EnvironmentAndActions

		private const string GcdCheckSpell = "Wrath";

		private EnvironmentState GetState(GameObject target)
		{
			const Int32 LunarEclipse = 48518;
			const Int32 SolarEclipse = 48517;
			return new EnvironmentState
			{
				PlayerHasLunarEclipse = me.HasAura(me.Player, LunarEclipse, me.Player),
				PlayerHasSolarEclipse = me.HasAura(me.Player, SolarEclipse, me.Player),
			};
		}

		private struct EnvironmentState
		{
			public bool PlayerHasLunarEclipse;
			public bool PlayerHasSolarEclipse;

			/// <summary>
			/// Returns state in int form, as if it was bit vector
			/// </summary>
			public int ToInt()
			{
				int result = 0;
				int fieldIndex = 0;
				foreach (var fieldInfo in GetType().GetFields())
				{
					var fieldVal = (bool)fieldInfo.GetValue(this);
					if (fieldVal)
					{
						result |= (1 << fieldIndex);
					}
					fieldIndex++;
				}

				return result;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"lunar: {PlayerHasLunarEclipse}");
				sb.AppendLine($"solar: {PlayerHasSolarEclipse}");
				sb.AppendLine($"as int: {ToInt()}");
				return sb.ToString();
			}

			public static int GetTotalStateCount()
			{
				return (int)Math.Pow(2, typeof(EnvironmentState).GetFields().Length);
			}
		}




		private class WrathAction : Action
		{
			public WrathAction(int id, Client performer) : base(id, performer)
			{
				this.Name = "Wrath";
			}

			public override void Perform()
			{
				base.Perform();
				performer.CastSpell("Wrath");
			}
		}

		private class StarfireAction : Action
		{
			public StarfireAction(int id, Client performer) : base(id, performer)
			{
				this.Name = "Starfire";
			}

			public override void Perform()
			{
				base.Perform();
				performer.CastSpell("Starfire");
			}
		}
		#endregion

		private abstract class Action
		{
			public string Name { get; protected set; }
			public readonly int Id;
			protected readonly Client performer;

			protected Action(int id, Client performer)
			{
				this.Id = id;
				this.performer = performer;
			}

			public virtual void Perform()
			{
				Console.WriteLine($"Performing action {Name}");
			}

			public virtual bool CanPerform(EnvironmentState state)
			{
				return true;
			}
		}
	}
}
