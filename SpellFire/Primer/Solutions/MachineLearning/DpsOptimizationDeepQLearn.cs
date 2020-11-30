using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions.MachineLearning
{
	/// <summary>
	///
	/// Deep Q-Learning for DPS Optimization
	///
	///
	/// Solution depends on DQN infrastructure, running in Python server
	///
	/// </summary>
	public class DpsOptimizationDeepQLearn : Solution
	{
		private const string TrainingTargetName = "Grandmaster's Training Dummy";

		private ControlInterface ci;



		private int TotalDamage;
		private DateTime start;
		private ActionPool actionPool;

		private bool isTraining = true;

		private EnvironmentState currentState;

		static int port = 65432;
		static string ip = "127.0.0.1";

		Socket sock;

		public DpsOptimizationDeepQLearn(Client client) : base(client)
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
			ci.remoteControl.FrameScript__Execute("SendChatMessage('.cheat power on')", 0, 0);

			actionPool = new ActionPool(me, new[]
			{
				typeof(WrathAction),
				typeof(StarfireAction),
			});


			SetupConnection(new IPEndPoint(IPAddress.Parse(ip), port));

			SendObject(new
			{
				StateCount = EnvironmentState.GetTotalStateCount(),
				ActionCount = actionPool.ActionCount()
			});

			ResetSession();
			Console.WriteLine("Running.");
			this.Active = true;

			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}
			GameObject target = SelectTrainingTarget();
			if (target == null)
			{
				return;
			}

			currentState = EnvironmentState.GetState(me);
			SendObject(new
			{
				State = currentState.ToInt(),
				Reward = 0
			});
		}

		void SetupConnection(EndPoint endPoint)
		{
			sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Console.WriteLine("Connecting to server...");
			while (!sock.Connected)
			{
				try
				{
					sock.Connect(endPoint);
				}
				catch (Exception e)
				{
					Console.WriteLine("Connection failed. Retrying...");
					Console.WriteLine(e);
					Thread.Sleep(1000);
				}
			}
		}

		bool GetPing()
		{
			var buff = new byte[1024];
			sock.Receive(buff);
			var str = Encoding.UTF8.GetString(buff);
			try
			{
				return Int32.Parse(str) == 1;
			}
			catch (Exception e)
			{
				return false;
			}
		}

		int GetAgentAction()
		{
			var buff = new byte[1024];
			sock.Receive(buff);
			return Int32.Parse(Encoding.UTF8.GetString(buff));
		}

		void SendObject(object obj)
		{
			sock.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
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

			GameObject target = SelectTrainingTarget();
			if (target == null)
			{
				return;
			}



			Action selectedAction;
			bool canPerformAction;
			do
			{
				selectedAction = actionPool.GetAction(GetAgentAction());
				canPerformAction = selectedAction.CanPerform(currentState);

				SendObject(new
				{
					ActionValid = canPerformAction
				});
			} while (!canPerformAction);

			selectedAction.Perform();
			do
			{
				Thread.Sleep(10);
			} while (!Ready());

			var newState = EnvironmentState.GetState(me);
			var reward = GetReward();
			Console.WriteLine("--- Current state ---");
			Console.WriteLine(currentState);
			Console.WriteLine("--- New state ---");
			Console.WriteLine(newState);
			Console.WriteLine($"Reward: {reward}");

			SendObject(new
			{
				State = newState.ToInt(),
				Reward = reward
			});

			currentState = newState;

			/* wait for ping from server */
			if (!GetPing())
			{
				throw new Exception("Error getting pinged from server. Shutting down.");
			}
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

			var state = EnvironmentState.GetState(me);
			SendObject(new
			{
				State = state.ToInt(),
				Reward = 0
			});
			var selectedAction = actionPool.GetAction(GetAgentAction());
			selectedAction.Perform();

			Console.WriteLine("--- For state ---");
			Console.WriteLine(state);
			Console.WriteLine($"performed {selectedAction.Name}");
		}

		private bool Ready()
		{
			return !me.IsOnCooldown(GcdCheckSpell) && !me.Player.IsCastingOrChanneling();
		}

		private void ResetSession()
		{
			Console.WriteLine($"[{DateTime.Now}] SESSION RESET");
			Console.WriteLine($"Total damage done: {TotalDamage}");

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

		private float GetReward()
		{
			var scalingFactor = 1000f;
			var currentDps = GetCurrentDamagePerSecond();
			return currentDps / scalingFactor;
		}

		private class ActionPool
		{
			private IList<Action> actions = new List<Action>();

			public ActionPool(Client performer, Type[] actionTypes)
			{

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

		private struct EnvironmentState
		{
			public bool PlayerHasSolarEclipse;
			public bool PlayerHasLunarEclipse;

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

			public static EnvironmentState GetState(Client client)
			{
				const Int32 LunarEclipse = 48518;
				const Int32 SolarEclipse = 48517;
				return new EnvironmentState
				{
					PlayerHasSolarEclipse = client.HasAura(client.Player, SolarEclipse, client.Player),
					PlayerHasLunarEclipse = client.HasAura(client.Player, LunarEclipse, client.Player),
				};
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
