using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
		private ProcessEntry processEntry;

		private Solution solution;
		private Task solutionTask;

		public MainFormController(MainForm mainForm)
		{
			this.mainForm = mainForm;
		}

		public void InitializeSolutionListBox(ListBox listBoxSolutions)
		{
			ICollection<string> solutionTypes = new List<string>
			{
				nameof(AutoLooter),
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

		public void AttachToProcess(object selectedItem)
		{
			if (processEntry != null)
			{
				MessageBox.Show("Already attached to process.");
				return;
			}

			this.processEntry = selectedItem as ProcessEntry;

			if (this.processEntry == null)
			{
				MessageBox.Show("No WoW process selected.");
				return;
			}

			/* establish connection to remote agent */
			InjectProcess(processEntry.GetProcess());

			mainForm.PostInfo($"Attached to process of pid: {processEntry.GetProcess().Id}", Color.LightGreen);
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


				this.processEntry = null;
			}
		}

		public void ToggleRunState(ListBox listBoxSolutions)
		{
			if (solution != null)
			{
				solution.Stop();
				solutionTask.Wait();
				mainForm.PostInfo($"Stopped solution {solution.GetType().Name}.", Color.DarkRed);
				solution = null;

				mainForm.SetToggleButtonText("Start");
				return;
			}

			if (listBoxSolutions.SelectedIndex == -1)
			{
				MessageBox.Show("Select solution to run.");
				return;
			}

			if (processEntry == null)
			{
				MessageBox.Show("Select WoW process to attach to.");
				return;
			}

			string solutionType = listBoxSolutions.SelectedItem as string;


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
				solution.Finish();
			}));

			mainForm.PostInfo($"Running solution {solutionType}", Color.Blue);
			mainForm.SetToggleButtonText("Stop");
		}
	}
}
