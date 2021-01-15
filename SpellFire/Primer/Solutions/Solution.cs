using System;
using System.Collections.Generic;
using System.Drawing;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	public abstract class Solution : IDisposable
	{
		public bool Active { get; set; }

		public readonly Client me;

		protected Solution(Client me)
		{
			this.me = me;
		}

		public abstract void Tick();
		public virtual void Stop()
		{
			this.Active = false;
		}
		public abstract void Dispose();

		public virtual void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
		{
			if (me.Player != null && me.ObjectManager != null)
			{
				RadarCanvas.BasicRadar(radarCanvas, radarBackBuffer,
					me.Player, me.ObjectManager, me.GetTargetGUID(), me.ControlInterface);
			}
		}

		public virtual Action<IList<string>> GetCommand(string cmd)
		{
			return null;
		}
	}
}