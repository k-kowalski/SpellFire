using System;
using System.Drawing;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public abstract class Solution : IDisposable
	{
		public bool Active { get; set; }

		protected readonly Client client;

		protected Solution(Client client)
		{
			this.client = client;
		}

		public abstract void Tick();
		public virtual void Stop()
		{
			this.Active = false;
		}
		public abstract void Dispose();

		public virtual void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
		{
			if (client.Player != null && client.ObjectManager != null)
			{
				RadarCanvas.BasicRadar(radarCanvas, radarBackBuffer,
					client.Player, client.ObjectManager, client.GetTargetGUID(), client.ControlInterface);
			}
		}
	}
}