using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Controller;

using LuaEventHandler = System.Action<SpellFire.Well.LuaEvents.LuaEventArgs>;

namespace SpellFire.Well.LuaEvents
{
	public class LuaEventListener
	{
		private readonly IDictionary<string, LuaEventHandler> eventHandlers;
		private readonly ControlInterface.HostControl hostControl;

		public LuaEventListener(ControlInterface.HostControl hostControl)
		{
			eventHandlers = new Dictionary<string, LuaEventHandler>();

			this.hostControl = hostControl;

			hostControl.LuaEventFired += DispatchLuaEvent;
		}
		
		~LuaEventListener()
		{
			hostControl.LuaEventFired -= DispatchLuaEvent;
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
