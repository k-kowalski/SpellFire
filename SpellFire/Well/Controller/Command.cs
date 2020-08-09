using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SpellFire.Well.Lua;
using SpellFire.Well.Net;

namespace SpellFire.Well.Controller
{
	public static class CommandCallback
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate Int32 LuaEventCallback(IntPtr luaState);

		public delegate Int32 EndScene(IntPtr thisDevice);

		public delegate Int32 FrameScript__Execute(String command, int a1, int a2);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate String FrameScript__GetLocalizedText(IntPtr thisActivePlayerObject, String luaVariable, Int32 a1);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate bool CGPlayer_C__ClickToMove(IntPtr thisActivePlayerObject, ClickToMoveType clickType, [In] ref Int64 interactGUID, [In] ref Vector3 clickPosition, float precision);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate IntPtr CGPlayer_C__ClickToMoveStop(IntPtr thisActivePlayerObject);

		public delegate IntPtr ClntObjMgrGetActivePlayerObj();

		public delegate Int32 SelectUnit(Int64 GUID);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate Int32 InteractUnit(IntPtr thisObject);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate Int32 InvalidPtrCheck(IntPtr ptr);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate Int32 LuaGetTop(IntPtr luaState);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate string LuaToString(IntPtr luaState, Int32 index, Int32 length);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate Int32 FrameScript__RegisterFunction(String luaFunctionName, IntPtr functionPointer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate Int32 FrameScript__UnregisterFunction(string luaFunctionName);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void WorldSendPacket(IntPtr data);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void ClientSendPacket(IntPtr thisObject, DataStore data);

		public delegate IntPtr NetGetCurrentConnection();

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate UnitReaction CGUnit_C__UnitReaction(IntPtr thisObject, IntPtr unit);

		/* returns pointer to string, cannot marshal it via delegate signature */
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate IntPtr GetUnitName(IntPtr thisObject);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate IntPtr CGUnit_C__UpdateDisplayInfo(IntPtr thisObject, bool a1);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate bool Spell_C__CastSpell(Int32 spellID, IntPtr item, Int64 targetGUID, bool isTrade);
	}

	public class CommandHandler : TimelessMarshalByRefObject, IDisposable
	{
		private readonly GlobalConfig config;
		[NonSerialized]
		private CommandQueue commandQueue;
		[NonSerialized]
		private ControlInterface ctrlInterface;

		private CommandCallback.EndScene EndScene;
		private CommandCallback.FrameScript__Execute FrameScript__Execute;
		private CommandCallback.FrameScript__GetLocalizedText FrameScript__GetLocalizedText;
		private CommandCallback.ClntObjMgrGetActivePlayerObj ClntObjMgrGetActivePlayerObj;
		private CommandCallback.CGPlayer_C__ClickToMove CGPlayer_C__ClickToMove;
		private CommandCallback.CGPlayer_C__ClickToMoveStop CGPlayer_C__ClickToMoveStop;
		private CommandCallback.SelectUnit SelectUnit;
		private CommandCallback.InvalidPtrCheck InvalidPtrCheck;
		private CommandCallback.LuaGetTop LuaGetTop;
		private CommandCallback.LuaToString LuaToString;
		private CommandCallback.FrameScript__RegisterFunction FrameScript__RegisterFunction;
		private CommandCallback.FrameScript__UnregisterFunction FrameScript__UnregisterFunction;
		private CommandCallback.WorldSendPacket WorldSendPacket;
		private CommandCallback.ClientSendPacket ClientSendPacket;
		public CommandCallback.NetGetCurrentConnection NetGetCurrentConnection;
		private CommandCallback.CGUnit_C__UnitReaction CGUnit_C__UnitReaction;
		private CommandCallback.CGUnit_C__UpdateDisplayInfo CGUnit_C__UpdateDisplayInfo;
		private CommandCallback.Spell_C__CastSpell Spell_C__CastSpell;

		private CommandCallback.LuaEventCallback eventCallback;
		private IntPtr luaEventCallbackPtr;

		private string luaEventFunctionName;
		private string frameName;

		private SystemWin32.WndProc originalWndProc;
		private SystemWin32.WndProc WndProcPatchInstance;

		private bool wardenScan = false;

		public CommandHandler(ControlInterface ctrlInterface, GlobalConfig config)
		{
			this.commandQueue = new CommandQueue(ctrlInterface);
			this.ctrlInterface = ctrlInterface;
			this.config = config;

			eventCallback = LuaEventHandler;
			luaEventCallbackPtr = Marshal.GetFunctionPointerForDelegate(eventCallback);
			frameName = SFUtil.GetRandomAsciiString(5);

			ResolveEndSceneAddress();
			RegisterFunctions();
		}

		public void DetourWndProc()
		{
			WndProcPatchInstance = WndProcPatch;

			IntPtr hwnd = Process.GetCurrentProcess().MainWindowHandle;
			IntPtr originalWndProcAddress = (IntPtr) SystemWin32.GetWindowLong(hwnd, SystemWin32.GWL_WNDPROC);

			originalWndProc = Marshal.GetDelegateForFunctionPointer<SystemWin32.WndProc>(originalWndProcAddress);

			SystemWin32.SetWindowLong(hwnd, SystemWin32.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(WndProcPatchInstance));
		}

		public IntPtr WndProcPatch(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam)
		{
			if(msg == SystemWin32.WM_KEYDOWN || msg == SystemWin32.WM_KEYUP)
			{
				ctrlInterface.hostControl.DispatchWindowMessage(hWnd, msg, wParam, lParam);
			}

			return originalWndProc(hWnd, msg, wParam, lParam);
		}

		private void ResolveEndSceneAddress()
		{

			IntPtr dxDeviceObject = Marshal.ReadIntPtr(IntPtr.Zero + Offset.DirectX.Device);
			IntPtr vTablePointer = Marshal.ReadIntPtr(dxDeviceObject + Offset.DirectX.VirtualMethodTable);
			IntPtr vTableData = Marshal.ReadIntPtr(vTablePointer);

			/*
             * virtual method table consists of pointers so multiply by pointer size
             * since Marshal.ReadIntPtr counts in bytes 
             */
			Offset.Function.EndScene = Marshal.ReadInt32(vTableData + (Offset.DirectX.EndSceneVMTableIndex * IntPtr.Size));
		}

		private void RegisterFunctions()
		{
			Type commandHandlerType = typeof(CommandHandler);

			/* bind delegates to functions at specified addresses */
			FieldInfo[] functionOffsetFields =
				typeof(Offset.Function)
					.GetFields(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);

			FieldInfo[] chFields =
				commandHandlerType
					.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

			foreach (FieldInfo offsetField in functionOffsetFields)
			{
				FieldInfo delegateFieldInfo = chFields.FirstOrDefault(chField => chField.Name == offsetField.Name);

				if (delegateFieldInfo != null)
				{
					delegateFieldInfo.SetValue(this,
						Marshal.GetDelegateForFunctionPointer(IntPtr.Zero + (Int32)offsetField.GetValue(null), delegateFieldInfo.FieldType));
				}
			}


			/* bind handlers to events */
			MethodInfo[] chMethods =
				commandHandlerType
					.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);

			foreach (EventInfo ciEventInfo in typeof(ControlInterface.RemoteControl).GetEvents())
			{
				MethodInfo handlerInfo = chMethods
					.FirstOrDefault(handler =>
						handler.Name.RemoveFromEnd("Handler") == ciEventInfo.Name.RemoveFromEnd("Event"));

				if (handlerInfo != null)
				{
					ctrlInterface.hostControl.PrintMessage($"Binding {handlerInfo.Name} to {ciEventInfo.Name}");
					Delegate handlerInstance = Delegate.CreateDelegate(ciEventInfo.EventHandlerType, this, handlerInfo);

					ciEventInfo.AddEventHandler( ctrlInterface.remoteControl, handlerInstance);
				}
			}

		}

		public void InitializeLuaEventFrameHandler()
		{
			if (luaEventFunctionName != null)
			{
				return;
			}

			while (wardenScan)
			{
				Thread.Sleep(200);
			}

			luaEventFunctionName = config.LuaPlugFunctionName ?? SFUtil.GetRandomAsciiString(4);

			commandQueue.Submit<object>((() =>
			{
				FrameScript__RegisterFunction(luaEventFunctionName, luaEventCallbackPtr);
				FrameScript__Execute($"if not {frameName} then  {frameName} = CreateFrame('Frame'); end; {frameName}:SetScript('OnEvent', function(self, eventName, ...) {luaEventFunctionName}(eventName, ...) end); {frameName}:RegisterAllEvents();", 0, 0);

				return null;
			}));
		}

		public void InitializeLuaEventFrameHandler_W()
		{
			if (luaEventFunctionName != null)
			{
				return;
			}

			luaEventFunctionName = config.LuaPlugFunctionName ?? SFUtil.GetRandomAsciiString(4);

			FrameScript__RegisterFunction(luaEventFunctionName, luaEventCallbackPtr);
			FrameScript__Execute($"if not {frameName} then  {frameName} = CreateFrame('Frame'); end; {frameName}:SetScript('OnEvent', function(self, eventName, ...) {luaEventFunctionName}(eventName, ...) end); {frameName}:RegisterAllEvents();", 0, 0);

			wardenScan = false;
		}

		public void DestroyLuaEventFrameHandler_W()
		{
			wardenScan = true;

			if (luaEventFunctionName == null)
			{
				return;
			}

			FrameScript__Execute($"{frameName}:UnregisterAllEvents(); {frameName}:SetScript('OnEvent', nil);",
				0, 0);
			FrameScript__UnregisterFunction(luaEventFunctionName);

			luaEventFunctionName = null;
		}

		public void DestroyLuaEventFrameHandler()
		{

			if (luaEventFunctionName == null)
			{
				return;
			}

			commandQueue.Submit<object>(() =>
			{
				FrameScript__Execute($"{frameName}:UnregisterAllEvents(); {frameName}:SetScript('OnEvent', nil);",
					0, 0);
				FrameScript__UnregisterFunction(luaEventFunctionName);

				return null;
			});

			luaEventFunctionName = null;
		}

		public Int32 InvalidPtrCheckPatch(IntPtr ptr)
		{
			if (ptr == luaEventCallbackPtr)
			{
				/*
                 * skip check for injected pointer
                 */
				return 0;
			}

			return InvalidPtrCheck(ptr);
		}

		public Int32 EndScenePatch(IntPtr thisDevice)
		{
			commandQueue.RunCommands();
			return EndScene(thisDevice);
		}

		public Int32 LuaEventHandler(IntPtr luaState)
		{
			Int32 argCount = LuaGetTop(luaState);
			List<string> luaEventArgs = new List<string>(argCount);
			for (Int32 i = 0; i <= argCount; i++)
			{
				luaEventArgs.Add(LuaToString(luaState, i + 1, 0));
			}

			try
			{
				ctrlInterface.hostControl.LuaEventTrigger(new LuaEventArgs(luaEventArgs));
			}
			catch (Exception)
			{
				/* host connection broken, ignore exception and die */
			}

			return 0;
		}

#region WoW Engine API
		public Int32 FrameScript__ExecuteHandler(String command, Int32 a1, Int32 a2)
		{
			return commandQueue.Submit<Int32>((() => FrameScript__Execute(command, a1, a2)));
		}

		public String FrameScript__GetLocalizedTextHandler(IntPtr thisActivePlayerObject, String luaVariable, Int32 a1)
		{
			return commandQueue.Submit<string>((() => FrameScript__GetLocalizedText(thisActivePlayerObject, luaVariable, a1)));
		}

		public bool CGPlayer_C__ClickToMoveHandler(IntPtr thisActivePlayerObject, ClickToMoveType clickType, [In] ref Int64 interactGUID,
			[In] ref Vector3 clickPosition, float precision)
		{
			Int64 _interactGUID = interactGUID;
			Vector3 _clickPosition = clickPosition;

			return commandQueue.Submit<bool>((() => CGPlayer_C__ClickToMove(thisActivePlayerObject, clickType, ref _interactGUID, ref _clickPosition, precision)));
		}

		public IntPtr CGPlayer_C__ClickToMoveStopHandler(IntPtr thisActivePlayerObject)
		{
			return commandQueue.Submit<IntPtr>((() => CGPlayer_C__ClickToMoveStop(thisActivePlayerObject)));
		}

		public IntPtr ClntObjMgrGetActivePlayerObjHandler()
		{
			return commandQueue.Submit<IntPtr>((() => ClntObjMgrGetActivePlayerObj()));
		}

		public Int32 SelectUnitHandler(Int64 GUID)
		{
			return commandQueue.Submit<Int32>((() => SelectUnit(GUID)));
		}

		public Int32 InteractUnitHandler(IntPtr thisObject)
		{
			CommandCallback.InteractUnit InteractUnit =
				Marshal.GetDelegateForFunctionPointer<CommandCallback.InteractUnit>(
					Marshal.ReadIntPtr(Marshal.ReadIntPtr(thisObject) + Offset.VirtualFunction.InteractUnit));
			return commandQueue.Submit<Int32>((() => InteractUnit(thisObject)));
		}

		public void WorldSendPacketHandler(IntPtr data)
		{
			_ = commandQueue.Submit<object>(() =>
			{
				WorldSendPacket(data);
				return null;
			});
		}

		public void ClientSendPacketHandler(IntPtr thisObject, DataStore data)
		{
			_ = commandQueue.Submit<object>(() =>
			{
				ClientSendPacket(thisObject, data);
				return null;
			});
		}

		public UnitReaction CGUnit_C__UnitReactionHandler(IntPtr thisObject, IntPtr unit)
		{
			return commandQueue.Submit<UnitReaction>((() => CGUnit_C__UnitReaction(thisObject, unit)));
		}

		public string GetUnitNameHandler(IntPtr thisObject)
		{
			CommandCallback.GetUnitName GetUnitName =
				Marshal.GetDelegateForFunctionPointer<CommandCallback.GetUnitName>(
					Marshal.ReadIntPtr(Marshal.ReadIntPtr(thisObject) + Offset.VirtualFunction.GetUnitName));

			return commandQueue.Submit<string>((() => Marshal.PtrToStringAnsi(GetUnitName(thisObject))));
		}

		public IntPtr CGUnit_C__UpdateDisplayInfoHandler(IntPtr thisObject, bool a1)
		{
			return commandQueue.Submit<IntPtr>((() => CGUnit_C__UpdateDisplayInfo(thisObject, a1)));
		}

		public bool Spell_C__CastSpellHandler(Int32 spellID, IntPtr item, Int64 targetGUID, bool isTrade)
		{
			return commandQueue.Submit<bool>((() => Spell_C__CastSpell(spellID, item, targetGUID, isTrade)));
		}
		#endregion

		public void Dispose()
		{
			DestroyLuaEventFrameHandler();

			if (originalWndProc != null)
			{
				/* restore original wnd proc if hooked */
				SystemWin32.SetWindowLong(
					Process.GetCurrentProcess().MainWindowHandle,
					SystemWin32.GWL_WNDPROC,
					Marshal.GetFunctionPointerForDelegate(originalWndProc));
			}
		}
	}
}
