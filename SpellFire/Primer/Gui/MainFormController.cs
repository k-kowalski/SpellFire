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
using SpellFire.Well.Util;

namespace SpellFire.Primer.Gui
{
	public class MainFormController
	{
		private Launcher launcher;

		public MainFormController(MainForm mainForm)
		{
			launcher = new Launcher(new Well.Util.Config("config1.txt"), mainForm);
		}

		public void InitializeSolutionListBox(ListBox listBoxSolutions)
		{
			listBoxSolutions.DataSource = new List<SolutionTypeEntry>(SolutionTypeEntry.GetSolutionTypes());
		}

		public void RefreshProcessList(ComboBox comboBoxProcesses)
		{
			comboBoxProcesses.DataSource = new List<ProcessEntry>(ProcessEntry.GetRunningWoWProcessess());
		}

		public void ToggleRunState(SolutionTypeEntry solutionTypeEntry, ProcessEntry processEntry)
		{
			if (solutionTypeEntry == null)
			{
				MessageBox.Show("Select solution to run.");
				return;
			}

			launcher.AttachAndLaunch(processEntry, solutionTypeEntry);
		}
	}
}
