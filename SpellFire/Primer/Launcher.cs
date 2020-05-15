using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class Launcher
	{
		private static readonly string LoginLuaScript = Encoding.UTF8.GetString(File.ReadAllBytes("Scripts/Login.lua"));

		private Config config;
		private string wowDir;

		public Launcher(Config config)
		{
			this.config = config;
			wowDir = config["wowDir"];
		}

		/// <summary>
		/// Launches WoW clients, injects them and performs initialization actions
		/// based on config
		/// </summary>
		/// <param></param>
		/// <returns></returns>
		public List<Client> LaunchClientsFromConfig()
		{

			List<Client> clients = new List<Client>();
			foreach (string key in config.Keys())
			{
				if (key.StartsWith("creds"))
				{
					char id = key[key.Length - 1];
					clients.Add( LaunchClient(id) );
				}
			}

			return clients;
		}

		public Client LaunchClient(int configClientId)
		{
			string[] credentials = config[$"creds{configClientId}"].Split(':');

			File.Copy(config[$"config{configClientId}"], wowDir + @"\WTF\Config.wtf", true);

			Process wowProcess = Process.Start(wowDir + @"\Wow.exe");

			if (wowProcess.WaitForInputIdle())
			{
				Client client = new Client(wowProcess);

				string loginCharacter = config[$"character{configClientId}"];
				Launcher.LoginClient(client, credentials[0], credentials[1], loginCharacter);

				return client;
			}

			return null;
		}

		private static void LoginClient(Client client, string username, string password, string loginCharacter)
		{
			string loginScriptFmt = String.Format(LoginLuaScript, username, password, loginCharacter);

			if (loginCharacter == null)
			{
				client.ControlInterface.remoteControl.FrameScript__Execute(loginScriptFmt, 0, 0);
			}
			else
			{
				client.Memory.Write(IntPtr.Zero + Offset.LastHardwareEvent, BitConverter.GetBytes(Environment.TickCount));
				while (!client.IsInWorld())
				{
					client.ControlInterface.remoteControl.FrameScript__Execute(loginScriptFmt, 0, 0);
					System.Threading.Thread.Sleep(1000);
				}
			}
		}
	}
}
