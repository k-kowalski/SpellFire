using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using SpellFire.Well.Controller;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class Client
	{
		public Process Process { get; }
		public ControlInterface ControlInterface { get; private set; }
		public Memory Memory { get; }

		public Client(Process process)
		{
			Process = process;
			Memory = new Memory(this.Process);

			InjectClient();
		}

		private void InjectClient()
		{
			ControlInterface ctrlInterface = new ControlInterface();

			string channelName = null;
			EasyHook.RemoteHooking.IpcCreateServer(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, ctrlInterface);

			string injectionLibraryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Well.dll");
			EasyHook.RemoteHooking.Inject(this.Process.Id, injectionLibraryPath, null, channelName);

			this.ControlInterface = ctrlInterface;
		}

		public bool IsInWorld()
		{
			return Memory.ReadInt32(IntPtr.Zero + Offset.WorldLoaded) == 1;
		}
	}
}
