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
		private ControlInterface ctrlInterface;
		private ProcessEntry currentProcessEntry;

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
			if (processEntry == null)
			{
				MessageBox.Show("No WoW process selected.");
				return false;
			}

			if (processEntry.Equals(currentProcessEntry))
			{
				/* do not reinject the same process */
				return true;
			}

			/* establish connection to remote agent */
			InjectProcess(processEntry.GetProcess());

			currentProcessEntry = processEntry;

			return true;
		}

		private void InjectProcess(Process process)
		{
			ctrlInterface = new ControlInterface();

			string channelName = null;
			RemoteHooking.IpcCreateServer(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, ctrlInterface);

			string injectionLibraryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Well.dll");
			try
			{
				EasyHook.RemoteHooking.Inject(process.Id, injectionLibraryPath, null, channelName);
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("There was an error while injecting into target:");
				Console.ResetColor();
				Console.WriteLine(e.ToString());
			}
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

			if ( ! AttachToProcess(processEntry))
			{
				return;
			}

			solution = Activator.CreateInstance(
				Type.GetType(Solution.SolutionAssemblyQualifier + solutionType),
				ctrlInterface,
				new Memory(processEntry.GetProcess())) as Solution;

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

			mainForm.PostInfo($"Running solution {solutionType}", Color.Blue);
			mainForm.SetToggleButtonText("Stop");	
		}
	}
}
