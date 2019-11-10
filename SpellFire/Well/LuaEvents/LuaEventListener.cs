using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.Util;
using LuaEventHandler = System.Action<SpellFire.Well.LuaEvents.LuaEventArgs>;

namespace SpellFire.Well.LuaEvents
{
	public class LuaEventListener : IDisposable
	{
		private readonly IDictionary<string, LuaEventHandler> eventHandlers;
		private readonly ControlInterface ci;

		public LuaEventListener(ControlInterface ci)
		{
			eventHandlers = new Dictionary<string, LuaEventHandler>();

			this.ci = ci;

			ci.remoteControl.InitializeLuaEventFrame();
			ci.hostControl.LuaEventFired += DispatchLuaEvent;
		}

		public void Dispose()
		{
			ci.hostControl.LuaEventFired -= DispatchLuaEvent;
			ci.remoteControl.DestroyLuaEventFrame();
		}

		public void Bind(string eventName, LuaEventHandler handler)
		{
			eventHandlers.Add(eventName, handler);
		}

		private void DispatchLuaEvent(LuaEventArgs luaEventArgs)
		{
			if (eventHandlers.TryGetValue(luaEventArgs.Name, out LuaEventHandler handler))
			{
				Task.Run(() => handler.Invoke(luaEventArgs));
			}
		}
	}
}
