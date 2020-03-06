using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpellFire.Primer.Gui
{
	public partial class MainForm : Form
	{
		private MainFormController mfController;

		public MainForm()
		{
			InitializeComponent();

			this.mfController = new MainFormController(this);

			this.mfController.InitializeSolutionListBox(this.listBoxSolutions);

			this.TopMost = true;
		}

		public void PostInfo(string info, Color color)
		{
			this.labelInfo.Text = info;
			this.labelInfo.ForeColor = color;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			this.mfController.RefreshProcessList(comboBoxProcesses);
		}

		/* makes combo box readonly */
		private void comboBoxProcesses_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		private void buttonToggle_Click(object sender, EventArgs e)
		{
			this.mfController.ToggleRunState(
				listBoxSolutions.SelectedItem as string,
				comboBoxProcesses.SelectedItem as ProcessEntry);
		}

		public void SetToggleButtonText(string text)
		{
			this.buttonToggle.Text = text;
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			this.mfController.RefreshProcessList(comboBoxProcesses);
		}
	}
}
