using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SpellFire.Well.Lua;
using SpellFire.Well.Net;

namespace SpellFire.Well.Controller
{
	public class ControlInterface : TimelessMarshalByRefObject
	{
		public ControlInterface()
		{
			remoteControl = new RemoteControl();
			hostControl = new HostControl();
		}

		public readonly HostControl hostControl;
		public class HostControl : TimelessMarshalByRefObject
		{
			public event Action<LuaEventArgs> LuaEventFired;
			public event Action<IntPtr, uint, IntPtr, IntPtr> WindowMessageDispatched;

			public void PrintMessage(string message)
			{
				Console.WriteLine($"[Well] {message}");
			}

			public void Ping() {/* used to check connection */}

			public void LuaEventTrigger(LuaEventArgs luaEventArgs)
			{
				LuaEventFired?.Invoke(luaEventArgs);
			}

			public void DispatchWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
			{
				WindowMessageDispatched?.Invoke(hWnd, msg, wParam, lParam);
			}
		}


		public readonly RemoteControl remoteControl;
		public class RemoteControl : TimelessMarshalByRefObject
		{
			#region Control
			public event Action<Packet> SendPacketEvent;
			public void SendPacket(Packet packet) =>
				SendPacketEvent?.Invoke(packet);

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

			public event CommandCallback.CGUnit_C__UnitReaction CGUnit_C__UnitReactionEvent;
			public UnitReaction CGUnit_C__UnitReaction(IntPtr thisObject, IntPtr unit) =>
				CGUnit_C__UnitReactionEvent?.Invoke(thisObject, unit) ?? UnitReaction.Unknown;

			public event Func<IntPtr, string> GetUnitNameEvent;
			public string GetUnitName(IntPtr thisObject) =>
				GetUnitNameEvent?.Invoke(thisObject) ?? null;

			public event CommandCallback.CGUnit_C__UpdateDisplayInfo CGUnit_C__UpdateDisplayInfoEvent;
			public IntPtr CGUnit_C__UpdateDisplayInfo(IntPtr thisObject, bool a1) =>
				CGUnit_C__UpdateDisplayInfoEvent?.Invoke(thisObject, a1) ?? IntPtr.Zero;

			public event CommandCallback.Spell_C__CastSpell Spell_C__CastSpellEvent;
			public bool Spell_C__CastSpell(Int32 spellID, IntPtr item, Int64 targetGUID, bool isTrade) =>
				Spell_C__CastSpellEvent?.Invoke(spellID, item, targetGUID, isTrade) ?? false;

			public event CommandCallback.Spell_C__HandleTerrainClick Spell_C__HandleTerrainClickEvent;
			public bool Spell_C__HandleTerrainClick(ref TerrainClick tc) =>
				Spell_C__HandleTerrainClickEvent?.Invoke(ref tc) ?? false;
			#endregion

		}
	}
}
