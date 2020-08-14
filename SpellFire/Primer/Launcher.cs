using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

		private MainForm mainForm;

		private Solution mainSolution;
		private Task solutionTask;
		private Task radarTask;

		private readonly List<Client> injectedClients;

		public Launcher(MainForm mainForm)
		{
			/* automatically rename injection dll */
			File.Copy("Well.dll", SFConfig.Global.DllName, true);

			injectedClients = new List<Client>();
			this.mainForm = mainForm;
		}

		public void AttachAndLaunch(ProcessEntry processEntry, SolutionTypeEntry solutionTypeEntry)
		{
			if (solutionTypeEntry == null)
			{
				MessageBox.Show("Select solution to run.");
				return;
			}

			if (mainSolution != null)
			{
				TerminateRunningSolution();
				return;
			}

			if (processEntry == null)
			{
				MessageBox.Show("No WoW process selected.");
				return;
			}

			mainForm.PostInfo("Launching...", Color.Gold);

			Client clientToRun = AttachToProcess(processEntry.GetProcess(), null);

			RunSolution(solutionTypeEntry.GetSolutionType(), clientToRun);
		}

		private Client AttachToProcess(Process process, ClientLaunchSettings settings)
		{
			var injectedClient = injectedClients.FirstOrDefault(client => client.Process.Id == process.Id);
			if (injectedClient == null)
			{
				injectedClient = new Client(process, settings);
				injectedClients.Add(injectedClient);
				process.Exited += (sender, args) => injectedClients.Remove(injectedClient);
			}

			return injectedClient;
		}

		public void WarmupPreset(Preset preset)
		{
			var launchTasks = new List<Task>();
			var clientsToRun = new List<Client>();
			if (preset.Clients.Length > 0)
			{
				/* take 2nd client preset */
				File.Copy(preset.Clients[1].GameConfig, SFConfig.Global.WowDir + @"\WTF\Config.wtf", true);

				foreach (ClientLaunchSettings settings in preset.Clients)
				{
					Process process = Process.Start(SFConfig.Global.WowDir + @"\Wow.exe");

					launchTasks.Add(Task.Run((() =>
					{
						if (process.WaitForInputIdle())
						{
							Thread.Sleep(1500);
							var client = AttachToProcess(process, settings);
							clientsToRun.Add(client);

							Launcher.LoginClient(client, settings.Login, settings.Password, settings.Character);
						}
					})));
					Thread.Sleep(1000);
				}
			}
			else
			{
				MessageBox.Show("No clients specified in preset.");
				return;
			}

			foreach (var task in launchTasks)
			{
				task.Wait();
			}

			var orderedClientsToRun = clientsToRun.OrderBy(client => Array.IndexOf(preset.Clients, client.LaunchSettings));

			string solutionName = preset.Clients[0].Solution;
			if (!String.IsNullOrEmpty(solutionName))
			{
				RunSolution(Type.GetType(solutionName),
					orderedClientsToRun.Count() == 1 ? (object)orderedClientsToRun.First() : orderedClientsToRun);
			}
		}

		public void LaunchPreset(Preset preset)
		{
			if (preset == null)
			{
				MessageBox.Show("No preset selected.");
				return;
			}

			var clientsToRun = new List<Client>();
			if (preset.Clients.Length > 0)
			{
				foreach (ClientLaunchSettings settings in preset.Clients)
				{
					Client client = LaunchClient(settings);
					clientsToRun.Add(client);
				}
			}
			else
			{
				MessageBox.Show("No clients specified in preset.");
				return;
			}

			string solutionName = preset.Clients[0].Solution;
			if ( ! String.IsNullOrEmpty(solutionName))
			{
				RunSolution(Type.GetType(solutionName),
					clientsToRun.Count() == 1 ? (object)clientsToRun[0] : clientsToRun);
			}
		}

		private Client LaunchClient(ClientLaunchSettings settings)
		{
			File.Copy(settings.GameConfig, SFConfig.Global.WowDir + @"\WTF\Config.wtf", true);

			Process process = Process.Start(SFConfig.Global.WowDir + @"\Wow.exe");

			if (process.WaitForInputIdle())
			{
				Thread.Sleep(1500);
				Client client = AttachToProcess(process, settings);

				Launcher.LoginClient(client, settings.Login, settings.Password, settings.Character);

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
				client.RefreshLastHardwareEvent();
				while (!client.IsInWorld())
				{
					client.ControlInterface.remoteControl.FrameScript__Execute(loginScriptFmt, 0, 0);
					System.Threading.Thread.Sleep(1000);
				}
			}
		}

		private void RunSolution(Type solutionType, object solutionArg)
		{
			/* for multibox solutions extract wrapping solution */
			if (solutionType.IsNested)
			{
				solutionType = solutionType.DeclaringType;
			}

			mainSolution = Activator.CreateInstance(solutionType, solutionArg) as Solution;

			mainForm.PostInfo($"Running solution {solutionType.Name}", Color.Green);
			mainForm.SetToggleButtonText("Stop");

			solutionTask = Task.Run((() =>
			{
				while (mainSolution.Active)
				{
					mainSolution.Tick();
				}
				mainSolution.Dispose();

				mainForm.PostInfo($"Solution {mainSolution.GetType().Name} stopped.", Color.DarkRed);
				mainForm.SetToggleButtonText("Start");
			}));

			radarTask = Task.Run((() =>
			{
				while (mainSolution.Active)
				{
					mainSolution.RenderRadar(mainForm.GetRadarCanvas(), mainForm.GetRadarBackBuffer());
					mainForm.RadarSwapBuffers();
				}
			}));
		}

		private void TerminateRunningSolution()
		{
			mainSolution.Stop();
			solutionTask.Wait();
			radarTask.Wait();
			mainSolution = null;
		}
	}
}
