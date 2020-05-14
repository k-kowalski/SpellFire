using System;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpellFire.Primer.Gui
{
	public partial class MainForm : Form
	{
		private MainFormController mfController;

		private Bitmap radarFrontBuffer;
		private Bitmap radarBackBuffer;

		public MainForm()
		{
			InitializeComponent();

			this.mfController = new MainFormController(this);

			this.mfController.InitializeSolutionListBox(this.listBoxSolutions);

			radarFrontBuffer = new Bitmap(radarCanvas.Width, radarCanvas.Width);
			radarBackBuffer = new Bitmap(radarCanvas.Width, radarCanvas.Width);
		}

		public void PostInfo(string info, Color color)
		{
			this.labelInfo.Text = info;
			this.labelInfo.ForeColor = color;
		}

		public void SetToggleButtonText(string text)
		{
			this.buttonToggle.Text = text;
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
			Task.Run(() => this.mfController.ToggleRunState(
				listBoxSolutions.SelectedItem as SolutionTypeEntry,
				comboBoxProcesses.SelectedItem as ProcessEntry));
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			this.mfController.RefreshProcessList(comboBoxProcesses);
		}

		private void radarCanvas_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage(radarFrontBuffer, Point.Empty);
		}

		public Bitmap GetRadarBackBuffer()
		{
			return radarBackBuffer;
		}

		public void RadarSwapBuffers()
		{
			radarFrontBuffer = (Bitmap)radarBackBuffer.Clone();
			radarCanvas.Invalidate();
		}

		public RadarCanvas GetRadarCanvas()
		{
			return radarCanvas;
		}

		public bool IsLaunchCheckboxChecked() => checkBoxLaunch.Checked;
	}
}
