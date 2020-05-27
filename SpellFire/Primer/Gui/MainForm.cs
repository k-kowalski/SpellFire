using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Gui
{
	public partial class MainForm : Form
	{
		private readonly Launcher launcher;

		private Bitmap radarFrontBuffer;
		private Bitmap radarBackBuffer;

		public MainForm()
		{
			InitializeComponent();

			radarFrontBuffer = new Bitmap(radarCanvas.Width, radarCanvas.Width);
			radarBackBuffer = new Bitmap(radarCanvas.Width, radarCanvas.Width);

			SFConfig config = SFConfig.LoadConfig();

			launcher = new Launcher(this);

			listBoxSolutions.DataSource = new List<SolutionTypeEntry>(SolutionTypeEntry.GetSolutionTypes());
			comboBoxProcesses.DataSource = new List<ProcessEntry>(ProcessEntry.GetRunningWoWProcessess());
			comboBoxPresets.DataSource = new List<Preset>(config.Presets);
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

		/* makes combo box readonly */
		private void comboBoxProcesses_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		private void buttonToggle_Click(object sender, EventArgs e)
		{
			SolutionTypeEntry solEntry = listBoxSolutions.SelectedItem as SolutionTypeEntry;
			ProcessEntry pEntry = comboBoxProcesses.SelectedItem as ProcessEntry;

			Task.Run(() =>
			{
				launcher.AttachAndLaunch(pEntry, solEntry);
			});
		}

		private void buttonPreset_Click(object sender, EventArgs e)
		{
			Preset preset = comboBoxPresets.SelectedItem as Preset;

			Task.Run(() =>
			{
				launcher.LaunchPreset(preset);
			});
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			comboBoxProcesses.DataSource = null;
			comboBoxProcesses.DataSource = new List<ProcessEntry>(ProcessEntry.GetRunningWoWProcessess());
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
			radarFrontBuffer = radarBackBuffer.Clone() as Bitmap;
			radarCanvas.Invalidate();
		}

		public RadarCanvas GetRadarCanvas()
		{
			return radarCanvas;
		}
	}
}
