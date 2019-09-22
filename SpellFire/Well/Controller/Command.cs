using System;
using System.Runtime.InteropServices;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Well.Controller
{
	public static class CommandCallback
	{
		public delegate Int32 LuaEventDelegate(IntPtr p);

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
		public delegate Int32 FrameScript__RegisterFunction(String luaFunctionName, IntPtr functionPointer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate Int32 InvalidPtrCheck(IntPtr ptr);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FrameScript__SignalEvent(UInt32 id, String format, RuntimeArgumentHandle args);

	}

	class CommandHandler : MarshalByRefObject
	{
		[NonSerialized]
		private static CommandQueue commandQueue;

		private static CommandCallback.EndScene EndScene;
		private static CommandCallback.FrameScript__Execute FrameScript__Execute;
		private static CommandCallback.FrameScript__GetLocalizedText FrameScript__GetLocalizedText;
		private static CommandCallback.ClntObjMgrGetActivePlayerObj ClntObjMgrGetActivePlayerObj;
		private static CommandCallback.CGPlayer_C__ClickToMove CGPlayer_C__ClickToMove;
		private static CommandCallback.CGPlayer_C__ClickToMoveStop CGPlayer_C__ClickToMoveStop;
		private static CommandCallback.SelectUnit SelectUnit;

		public CommandHandler(ControlInterface ctrlInterface, IntPtr EndSceneAddress)
		{
			CommandHandler.commandQueue = new CommandQueue();

			RegisterFunctions(ctrlInterface, EndSceneAddress);
		}

		private void RegisterFunctions(ControlInterface ctrlInterface, IntPtr EndSceneAddress)
		{
			EndScene = Marshal.GetDelegateForFunctionPointer<CommandCallback.EndScene>(EndSceneAddress);
			FrameScript__Execute = Marshal.GetDelegateForFunctionPointer<CommandCallback.FrameScript__Execute>(IntPtr.Zero + Offset.FrameScript__Execute);
			FrameScript__GetLocalizedText = Marshal.GetDelegateForFunctionPointer<CommandCallback.FrameScript__GetLocalizedText>(IntPtr.Zero + Offset.FrameScript__GetLocalizedText);
			CGPlayer_C__ClickToMove = Marshal.GetDelegateForFunctionPointer<CommandCallback.CGPlayer_C__ClickToMove>(IntPtr.Zero + Offset.CGPlayer_C__ClickToMove);
			CGPlayer_C__ClickToMoveStop = Marshal.GetDelegateForFunctionPointer<CommandCallback.CGPlayer_C__ClickToMoveStop>(IntPtr.Zero + Offset.CGPlayer_C__ClickToMoveStop);
			ClntObjMgrGetActivePlayerObj = Marshal.GetDelegateForFunctionPointer<CommandCallback.ClntObjMgrGetActivePlayerObj>(IntPtr.Zero + Offset.ClntObjMgrGetActivePlayerObj);
			SelectUnit = Marshal.GetDelegateForFunctionPointer<CommandCallback.SelectUnit>(IntPtr.Zero + Offset.SelectUnit);

			ctrlInterface.remoteControl.FrameScript__ExecuteEvent += FrameScript__ExecuteHandler;
			ctrlInterface.remoteControl.FrameScript__GetLocalizedTextEvent += FrameScript__GetLocalizedTextHandler;
			ctrlInterface.remoteControl.CGPlayer_C__ClickToMoveEvent += CGPlayer_C__ClickToMoveHandler;
			ctrlInterface.remoteControl.CGPlayer_C__ClickToMoveStopEvent += CGPlayer_C__ClickToMoveStopHandler;
			ctrlInterface.remoteControl.ClntObjMgrGetActivePlayerObjEvent += ClntObjMgrGetActivePlayerObjHandler;
			ctrlInterface.remoteControl.SelectUnitEvent += SelectUnitHandler;
			ctrlInterface.remoteControl.InteractUnitEvent += InteractUnitHandler;
		}

		public Int32 EndScenePatch(IntPtr thisDevice)
		{
			commandQueue.RunCommands();
			return EndScene(thisDevice);
		}

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

		/*
		 * @KKovs:
		 * why such call? is this some ad-hoc function pointer?
		 */
		public Int32 InteractUnitHandler(IntPtr thisObject)
		{
			CommandCallback.InteractUnit InteractUnit = Marshal.GetDelegateForFunctionPointer<CommandCallback.InteractUnit>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(thisObject) + Offset.InteractUnit));
			return commandQueue.Submit<Int32>((() => InteractUnit(thisObject)));
		}
	}
}
