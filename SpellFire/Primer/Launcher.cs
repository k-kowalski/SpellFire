using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Primer.Gui;
using SpellFire.Primer.Solutions;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class Launcher
	{
		private static readonly string LoginLuaScript = Encoding.UTF8.GetString(File.ReadAllBytes("Scripts/Login.lua"));

		private Config config;
		private MainForm mainForm;
		private string wowDir;

		private Solution solution;
		private Task solutionTask;
		private Task radarTask;

		public Launcher(Config config, MainForm mainForm)
		{
			this.config = config;
			this.mainForm = mainForm;
			wowDir = config["wowDir"];
		}

		/// <summary>
		/// Launches WoW clients, injects them and performs initialization actions
		/// based on config
		/// </summary>
		/// <param></param>
		/// <returns></returns>
		public IEnumerable<Client> LaunchClientsFromConfig()
		{

			List<Client> clients = new List<Client>();
			foreach (string key in config.Keys())
			{
				if (key.StartsWith("creds"))
				{
					int id = key[key.Length - 1] - '0';
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
				Client client = new Client(wowProcess, config);

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

		public void AttachAndLaunch(ProcessEntry processEntry, SolutionTypeEntry solutionTypeEntry)
		{
			if (solution != null)
			{
				TerminateRunningSolution();
				return;
			}

			mainForm.PostInfo("Launching...", Color.Gold);

			object solutionArg = null;

			if (solutionTypeEntry.IsMultiboxSolution())
			{
				solutionArg = LaunchClientsFromConfig();
			}
			else if (Int32.TryParse(config["quickLaunchId"], out int quickLaunchClientId))
			{
				solutionArg = LaunchClient(quickLaunchClientId);
			}
			else
			{
				if (processEntry == null)
				{
					MessageBox.Show("No WoW process selected.");
					return;
				}
				else
				{
					solutionArg = new Client(processEntry.GetProcess(), config);
				}
			}

			solution = Activator.CreateInstance(solutionTypeEntry.GetSolutionType(), solutionArg) as Solution;

			mainForm.PostInfo($"Running solution {solutionTypeEntry}", Color.Blue);
			mainForm.SetToggleButtonText("Stop");

			solutionTask = Task.Run((() =>
			{
				while (solution.Active)
				{
					solution.Tick();
				}
				solution.Dispose();

				mainForm.PostInfo($"Solution {solution.GetType().Name} stopped.", Color.DarkRed);
				mainForm.SetToggleButtonText("Start");
			}));

			radarTask = Task.Run((() =>
			{
				while (solution.Active)
				{
					solution.RenderRadar(mainForm.GetRadarCanvas(), mainForm.GetRadarBackBuffer());
					mainForm.RadarSwapBuffers();
				}
			}));
		}

		private void TerminateRunningSolution()
		{
			solution.Stop();
			solutionTask.Wait();
			radarTask.Wait();
			solution = null;
		}
	}
}
