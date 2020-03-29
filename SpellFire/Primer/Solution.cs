using System.Drawing;
using System.Windows.Forms;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;

namespace SpellFire.Primer
{
	public abstract class Solution
	{
		public const string SolutionAssemblyQualifier = "SpellFire.Primer.Solutions.";

		public bool Active { get; set; }
		protected readonly ControlInterface ci;
		protected readonly Memory memory;

		protected Solution(ControlInterface ci, Memory memory)
		{
			this.ci = ci;
			this.memory = memory;
		}

		public abstract void Tick();
		public virtual void Stop()
		{
			this.Active = false;
		}
		public abstract void Finish();

		public virtual void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer) {}
	}
}