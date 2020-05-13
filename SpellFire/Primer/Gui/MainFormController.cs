using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using EasyHook;
using SpellFire.Primer.Solutions;
using SpellFire.Well.Controller;

namespace SpellFire.Primer.Gui
{
	public class MainFormController
	{
		private MainForm mainForm;

		private Client client;

		private Solution solution;
		private Task solutionTask;
		private Task radarTask;

		public MainFormController(MainForm mainForm)
		{
			this.mainForm = mainForm;
		}

		public void InitializeSolutionListBox(ListBox listBoxSolutions)
		{
			ICollection<string> solutionTypes = new List<string>
			{
				nameof(AutoLooter),
				nameof(UnholyDK),
				nameof(Morpher),
				nameof(Disenchanter),
				nameof(BalanceDruidFarm),
				nameof(Fishing),
				nameof(AutoLogin),
			};

			listBoxSolutions.DataSource = solutionTypes;
		}

		public void RefreshProcessList(ComboBox comboBoxProcesses)
		{
			comboBoxProcesses.DataSource = null;

			ICollection <ProcessEntry> entries = new List<ProcessEntry>();

			foreach (Process process in Process.GetProcessesByName("WoW"))
			{
				entries.Add(new ProcessEntry(process));
			}

			comboBoxProcesses.DataSource = entries;
		}

		public bool AttachToProcess(ProcessEntry processEntry)
		{
			if (mainForm.IsLaunchCheckboxChecked())
			{
				mainForm.PostInfo("Launching...", Color.DarkGoldenrod);

				const int clientId = 1;
				var config = new Well.Util.Config();
				client = Client.LaunchClient(
					config,
					config["wowDir"],
					config[$"creds{clientId}"].Split(':'),
					1);
				return true;
			}

			if (processEntry == null)
			{
				MessageBox.Show("No WoW process selected.");
				return false;
			}

			Process process = processEntry.GetProcess();

			if (client != null && process.Id == client.Process.Id)
			{
				/* do not reinject the same process */
				return true;
			}

			client = new Client(process);

			return true;
		}

		public void ToggleRunState(string solutionType, ProcessEntry processEntry)
		{
			if (solution != null)
			{
				solution.Stop();
				solutionTask.Wait();
				radarTask.Wait();
				solution = null;

				return;
			}

			if (String.IsNullOrEmpty(solutionType))
			{
				MessageBox.Show("Select solution to run.");
				return;
			}

			if (!AttachToProcess(processEntry))
			{
				return;
			}

			solution = Activator.CreateInstance(
					Type.GetType(Solution.SolutionAssemblyQualifier + solutionType),
					client.ControlInterface, client.Memory)
				as Solution;

			mainForm.PostInfo($"Running solution {solutionType}", Color.Blue);
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
	}
}
