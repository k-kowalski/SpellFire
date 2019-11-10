using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SpellFire.Well.LuaEvents;

namespace SpellFire.Well.Controller
{
	public class ControlInterface : MarshalByRefObject
	{
		public ControlInterface()
		{
			remoteControl = new RemoteControl();
			hostControl = new HostControl();
		}

		public readonly HostControl hostControl;
		public class HostControl : MarshalByRefObject
		{
			public event Action<LuaEventArgs> LuaEventFired;

			public void ReportMessages(string[] messages)
			{
				foreach (string message in messages)
				{
					Console.WriteLine(message);
				}
			}

			public void Ping() {/* used to check connection */}

			public void LuaEventTrigger(LuaEventArgs luaEventArgs)
			{
				LuaEventFired?.Invoke(luaEventArgs);
			}
		}


		public readonly RemoteControl remoteControl;
		public class RemoteControl : MarshalByRefObject
		{
			#region Control
			public event Action InitializeLuaEventFrameEvent;
			public void InitializeLuaEventFrame() =>
				InitializeLuaEventFrameEvent?.Invoke();

			public event Action DestroyLuaEventFrameEvent;
			public void DestroyLuaEventFrame() =>
				DestroyLuaEventFrameEvent?.Invoke();
			#endregion

			#region WoW Engine API
			public event CommandCallback.FrameScript__Execute FrameScript__ExecuteEvent;
			public Int32 FrameScript__Execute(String command, int a1, int a2) =>
				FrameScript__ExecuteEvent?.Invoke(command, a1, a2) ?? 0;

			public event CommandCallback.FrameScript__GetLocalizedText FrameScript__GetLocalizedTextEvent;
			public String FrameScript__GetLocalizedText(IntPtr thisActivePlayerObject, String luaVariable, Int32 a1) =>
				FrameScript__GetLocalizedTextEvent?.Invoke(thisActivePlayerObject, luaVariable, a1);

			public event CommandCallback.CGPlayer_C__ClickToMove CGPlayer_C__ClickToMoveEvent;
			public bool CGPlayer_C__ClickToMove(IntPtr thisActivePlayerObject, ClickToMoveType clickType, [In] ref Int64 interactGUID, [In] ref Vector3 clickPosition, float precision) =>
				CGPlayer_C__ClickToMoveEvent?.Invoke(thisActivePlayerObject, clickType, ref interactGUID, ref clickPosition, precision) ?? false;

			public event CommandCallback.CGPlayer_C__ClickToMoveStop CGPlayer_C__ClickToMoveStopEvent;
			public IntPtr CGPlayer_C__ClickToMoveStop(IntPtr thisActivePlayerObject) =>
				CGPlayer_C__ClickToMoveStopEvent?.Invoke(thisActivePlayerObject) ?? IntPtr.Zero;

			public event CommandCallback.ClntObjMgrGetActivePlayerObj ClntObjMgrGetActivePlayerObjEvent;
			public IntPtr ClntObjMgrGetActivePlayerObj() =>
				ClntObjMgrGetActivePlayerObjEvent?.Invoke() ?? IntPtr.Zero;

			public event CommandCallback.SelectUnit SelectUnitEvent;
			public Int32 SelectUnit(Int64 GUID) =>
				SelectUnitEvent?.Invoke(GUID) ?? 0;

			public event CommandCallback.InteractUnit InteractUnitEvent;
			public Int32 InteractUnit(IntPtr thisObject) =>
				InteractUnitEvent?.Invoke(thisObject) ?? 0;
			#endregion
		}
	}
}
