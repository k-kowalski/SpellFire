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
		private static readonly string LoginLuaScript = Encoding.UTF8.GetString(File.ReadAllBytes("Scripts/Login.lua"));
		public Process Process { get; }
		public ControlInterface ControlInterface { get; private set; }
		public Memory Memory { get; }

		public Client(Process process)
		{
			Process = process;
			Memory = new Memory(this.Process);

			InjectClient();
		}

		/// <summary>
		/// Launches WoW clients, injects them and performs initialization actions
		/// based on config
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public static List<Client> LaunchClientsFromConfig(SpellFire.Well.Util.Config config)
		{
			string wowDir = config["wowDir"];

			List<Client> clients = new List<Client>();
			foreach (string key in config.Keys())
			{
				if (key.StartsWith("creds"))
				{
					char id = key[key.Length - 1];
					string[] creds = config[key].Split(':');

					clients.Add( LaunchClient(config, wowDir, creds, id) );
				}
			}

			return clients;
		}

		public static Client LaunchClient(Config config, string wowDir, string[] credentials, int id)
		{
			File.Copy(config[$"config{id}"],
				wowDir + @"\WTF\Config.wtf", true);
			Process wowProcess = Process.Start(wowDir + @"\Wow.exe");

			if (wowProcess.WaitForInputIdle())
			{
				Client client = new Client(wowProcess);

				string loginCharacter = config[$"character{id}"];
				client.Login(credentials[0], credentials[1], loginCharacter);

				return client;
			}

			return null;
		}

		private void Login(string username, string password, string loginCharacter)
		{
			string loginScriptFmt = String.Format(LoginLuaScript, username, password, loginCharacter);

			if (loginCharacter == null)
			{
				ControlInterface.remoteControl.FrameScript__Execute(loginScriptFmt, 0, 0);
			}
			else
			{
				Memory.Write(IntPtr.Zero + Offset.LastHardwareEvent, BitConverter.GetBytes(Environment.TickCount));
				while (!IsInWorld())
				{
					ControlInterface.remoteControl.FrameScript__Execute(loginScriptFmt, 0, 0);
					Thread.Sleep(1000);
				}
			}
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
