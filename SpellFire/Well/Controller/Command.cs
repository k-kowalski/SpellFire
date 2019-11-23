using SpellFire.Well.Model;
using SpellFire.Well.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SpellFire.Well.LuaEvents;
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
	}

	public class CommandHandler : MarshalByRefObject, IDisposable
	{
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

		private CommandCallback.LuaEventCallback eventCallback;
		private IntPtr luaEventCallbackPtr;

		private string luaEventFunctionName;
		private string frameName;

		public CommandHandler(ControlInterface ctrlInterface, IntPtr EndSceneAddress)
		{
			this.commandQueue = new CommandQueue();
			this.ctrlInterface = ctrlInterface;

			RegisterFunctions(ctrlInterface, EndSceneAddress);
		}

		private void RegisterFunctions(ControlInterface ctrlInterface, IntPtr EndSceneAddress)
		{
			#region WoW Engine API initialization
			EndScene = Marshal.GetDelegateForFunctionPointer<CommandCallback.EndScene>(EndSceneAddress);
			FrameScript__Execute = Marshal.GetDelegateForFunctionPointer<CommandCallback.FrameScript__Execute>(IntPtr.Zero + Offset.FrameScript__Execute);
			FrameScript__GetLocalizedText = Marshal.GetDelegateForFunctionPointer<CommandCallback.FrameScript__GetLocalizedText>(IntPtr.Zero + Offset.FrameScript__GetLocalizedText);
			CGPlayer_C__ClickToMove = Marshal.GetDelegateForFunctionPointer<CommandCallback.CGPlayer_C__ClickToMove>(IntPtr.Zero + Offset.CGPlayer_C__ClickToMove);
			CGPlayer_C__ClickToMoveStop = Marshal.GetDelegateForFunctionPointer<CommandCallback.CGPlayer_C__ClickToMoveStop>(IntPtr.Zero + Offset.CGPlayer_C__ClickToMoveStop);
			ClntObjMgrGetActivePlayerObj = Marshal.GetDelegateForFunctionPointer<CommandCallback.ClntObjMgrGetActivePlayerObj>(IntPtr.Zero + Offset.ClntObjMgrGetActivePlayerObj);
			SelectUnit = Marshal.GetDelegateForFunctionPointer<CommandCallback.SelectUnit>(IntPtr.Zero + Offset.SelectUnit);
			InvalidPtrCheck = Marshal.GetDelegateForFunctionPointer<CommandCallback.InvalidPtrCheck>(IntPtr.Zero + Offset.InvalidPtrCheck);
			LuaGetTop = Marshal.GetDelegateForFunctionPointer<CommandCallback.LuaGetTop>(IntPtr.Zero + Offset.LuaGetTop);
			LuaToString = Marshal.GetDelegateForFunctionPointer<CommandCallback.LuaToString>(IntPtr.Zero + Offset.LuaToString);
			FrameScript__RegisterFunction = Marshal.GetDelegateForFunctionPointer<CommandCallback.FrameScript__RegisterFunction>(IntPtr.Zero + Offset.FrameScript__RegisterFunction);
			FrameScript__UnregisterFunction = Marshal.GetDelegateForFunctionPointer<CommandCallback.FrameScript__UnregisterFunction>(IntPtr.Zero + Offset.FrameScript__UnregisterFunction);
			WorldSendPacket = Marshal.GetDelegateForFunctionPointer<CommandCallback.WorldSendPacket>(IntPtr.Zero + Offset.WorldSendPacket);
			ClientSendPacket = Marshal.GetDelegateForFunctionPointer<CommandCallback.ClientSendPacket>(IntPtr.Zero + Offset.ClientSendPacket);
			NetGetCurrentConnection = Marshal.GetDelegateForFunctionPointer<CommandCallback.NetGetCurrentConnection>(IntPtr.Zero + Offset.NetGetCurrentConnection);

			ctrlInterface.remoteControl.FrameScript__ExecuteEvent += FrameScript__ExecuteHandler;
			ctrlInterface.remoteControl.FrameScript__GetLocalizedTextEvent += FrameScript__GetLocalizedTextHandler;
			ctrlInterface.remoteControl.CGPlayer_C__ClickToMoveEvent += CGPlayer_C__ClickToMoveHandler;
			ctrlInterface.remoteControl.CGPlayer_C__ClickToMoveStopEvent += CGPlayer_C__ClickToMoveStopHandler;
			ctrlInterface.remoteControl.ClntObjMgrGetActivePlayerObjEvent += ClntObjMgrGetActivePlayerObjHandler;
			ctrlInterface.remoteControl.SelectUnitEvent += SelectUnitHandler;
			ctrlInterface.remoteControl.InteractUnitEvent += InteractUnitHandler;
			#endregion

			ctrlInterface.remoteControl.InitializeLuaEventFrameEvent += InitializeLuaEventFrame;
			ctrlInterface.remoteControl.DestroyLuaEventFrameEvent += DestroyLuaEventFrame;
		}


		public void InitializeLuaEventFrame()
		{
			eventCallback = LuaEventHandler;
			luaEventCallbackPtr = Marshal.GetFunctionPointerForDelegate(eventCallback);

			luaEventFunctionName = SFUtil.GetRandomAsciiString(4);
			frameName = SFUtil.GetRandomAsciiString(5);

			commandQueue.Submit<object>((() =>
			{
				FrameScript__RegisterFunction(luaEventFunctionName, luaEventCallbackPtr);
				FrameScript__Execute($"{frameName} = CreateFrame('Frame'); {frameName}:SetScript('OnEvent', {luaEventFunctionName}); {frameName}:RegisterAllEvents();", 0, 0);

				return null;
			}));
		}

		public void DestroyLuaEventFrame()
		{
			commandQueue.Submit<object>((() =>
			{
				if (frameName != null)
				{
					FrameScript__Execute($"{frameName}:UnregisterAllEvents(); {frameName}:SetScript('OnEvent', nil);", 0, 0);
				}

				if (luaEventFunctionName != null)
				{
					FrameScript__UnregisterFunction(luaEventFunctionName);
				}

				return null;
			}));
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
			/*
			 * LuaToString takes parameters starting from 1
			 * and we discard first event argument, hence
			 * we start from 2
			 */
			List<string> luaEventArgs = new List<string>(argCount - 1);
			for (Int32 i = 2; i <= argCount; i++)
			{
				luaEventArgs.Add(LuaToString(luaState, i, 0));
			}

			try
			{
				ctrlInterface.hostControl.LuaEventTrigger( new LuaEventArgs(luaEventArgs) );
			}
			catch (Exception)
			{
				/* host connection broken, ignore exception and die */
				//TODO: Lua message queue and there catch connection exceptions
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
			CommandCallback.InteractUnit InteractUnit = Marshal.GetDelegateForFunctionPointer<CommandCallback.InteractUnit>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(thisObject) + Offset.InteractUnit));
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
		#endregion

		public void Dispose()
		{
			DestroyLuaEventFrame();
		}
	}
}
