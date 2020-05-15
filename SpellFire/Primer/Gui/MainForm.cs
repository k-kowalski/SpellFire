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

			mfController = new MainFormController(this);

			mfController.InitializeSolutionListBox(listBoxSolutions);

			radarFrontBuffer = new Bitmap(radarCanvas.Width, radarCanvas.Width);
			radarBackBuffer = new Bitmap(radarCanvas.Width, radarCanvas.Width);
		}

		public void PostInfo(string info, Color color)
		{
			Invoke(new Action(() =>
			{
				labelInfo.Text = info;
				labelInfo.ForeColor = color;
			}));
		}

		public void SetToggleButtonText(string text)
		{
			Invoke(new Action(() =>
			{
				buttonToggle.Text = text;
			}));
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			mfController.RefreshProcessList(comboBoxProcesses);
		}

		/* makes combo box readonly */
		private void comboBoxProcesses_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		private void buttonToggle_Click(object sender, EventArgs e)
		{
			SolutionTypeEntry solEntry = listBoxSolutions.SelectedItem as SolutionTypeEntry;
			ProcessEntry pEntry = comboBoxProcesses.SelectedItem as ProcessEntry;

			Task.Run(() => this.mfController.ToggleRunState(solEntry, pEntry));
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			mfController.RefreshProcessList(comboBoxProcesses);
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
