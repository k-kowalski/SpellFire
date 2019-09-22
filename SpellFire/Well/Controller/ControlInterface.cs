using System;
using System.Runtime.InteropServices;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

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
			public void ReportMessages(string[] messages)
			{
				foreach (string message in messages)
				{
					Console.WriteLine(message);
				}
			}

			public void Ping() {/* used to check connection */}
		}


		public readonly RemoteControl remoteControl;
		public class RemoteControl : MarshalByRefObject
		{
			public event CommandCallback.FrameScript__Execute FrameScript__ExecuteEvent;
			public Int32 FrameScript__Execute(String command, int a1, int a2) =>
				FrameScript__ExecuteEvent(command, a1, a2);

			public event CommandCallback.FrameScript__GetLocalizedText FrameScript__GetLocalizedTextEvent;
			public String FrameScript__GetLocalizedText(IntPtr thisActivePlayerObject, String luaVariable, Int32 a1) =>
				FrameScript__GetLocalizedTextEvent(thisActivePlayerObject, luaVariable, a1);

			public event CommandCallback.CGPlayer_C__ClickToMove CGPlayer_C__ClickToMoveEvent;
			public bool CGPlayer_C__ClickToMove(IntPtr thisActivePlayerObject, ClickToMoveType clickType, [In] ref Int64 interactGUID, [In] ref Vector3 clickPosition, float precision) =>
				CGPlayer_C__ClickToMoveEvent(thisActivePlayerObject, clickType, ref interactGUID, ref clickPosition, precision);

			public event CommandCallback.CGPlayer_C__ClickToMoveStop CGPlayer_C__ClickToMoveStopEvent;
			public IntPtr CGPlayer_C__ClickToMoveStop(IntPtr thisActivePlayerObject) =>
				CGPlayer_C__ClickToMoveStopEvent(thisActivePlayerObject);

			public event CommandCallback.ClntObjMgrGetActivePlayerObj ClntObjMgrGetActivePlayerObjEvent;
			public IntPtr ClntObjMgrGetActivePlayerObj() =>
				ClntObjMgrGetActivePlayerObjEvent();

			public event CommandCallback.SelectUnit SelectUnitEvent;
			public Int32 SelectUnit(Int64 GUID) =>
				SelectUnitEvent(GUID);

			public event CommandCallback.InteractUnit InteractUnitEvent;
			public Int32 InteractUnit(IntPtr thisObject) =>
				InteractUnitEvent(thisObject);
		}
	}
}
