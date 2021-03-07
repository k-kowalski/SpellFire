using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using LuaEventHandler = System.Action<SpellFire.Well.Lua.LuaEventArgs>;

namespace SpellFire.Well.Lua
{
	public class LuaEventListener : IDisposable
	{
		private const int eventPollingIntervalMs = 50;
		private readonly IDictionary<string, LuaEventHandler> eventHandlers;
		private readonly ControlInterface ci;

		private bool isTaskOn;
		private Task eventGrabDispatchTask;

		private bool _active;
		public bool Active
		{
			get => _active;
			set
			{
				if (value)
				{
					ci.remoteControl.InitializeLuaEventFrame();
				}
				else
				{
					ci.remoteControl.DestroyLuaEventFrame();
				}

				_active = value;
			}
		}

		public LuaEventListener(ControlInterface ci)
		{
			eventHandlers = new Dictionary<string, LuaEventHandler>();

			eventGrabDispatchTask = Task.Run(() =>
			{
				isTaskOn = true;
				while (isTaskOn)
				{
					if (Active)
					{
						var luaEvents = ci.remoteControl.GrabLuaEvents();
						if (luaEvents != null)
						{
							foreach (var luaEvent in luaEvents)
							{
								DispatchLuaEvent(luaEvent);
							}
						}
					}

					Thread.Sleep(eventPollingIntervalMs);
				}
			});

			this.ci = ci;
		}

		public void Dispose()
		{
			eventHandlers.Clear();
			Active = false;
			isTaskOn = false;
			eventGrabDispatchTask.Wait();
		}

		public void Bind(string eventName, LuaEventHandler handler)
		{
			eventHandlers.Add(eventName, handler);
		}

		public void Unbind(string eventName)
		{
			eventHandlers.Remove(eventName);
		}

		private void DispatchLuaEvent(LuaEventArgs luaEventArgs)
		{
#if false
			/* trace events and their arguments */
			Console.WriteLine(luaEventArgs.Name);
			foreach (string arg in luaEventArgs.Args)
			{
				Console.WriteLine("\t" + arg);
			}
#endif
			string key = luaEventArgs.Name == "COMBAT_LOG_EVENT_UNFILTERED" ? luaEventArgs.Args[1] : luaEventArgs.Name;
			if (eventHandlers.TryGetValue(key, out LuaEventHandler handler))
			{
				Task.Run(() =>
				{
					try
					{
						handler.Invoke(luaEventArgs);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				});
			}
		}
	}
}
