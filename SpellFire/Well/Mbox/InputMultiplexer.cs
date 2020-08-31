using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Well.Controller;
using SpellFire.Well.Util;

namespace SpellFire.Well.Mbox
{
	public class InputMultiplexer
	{
		private readonly ControlInterface.HostControl source;
		public ICollection<IntPtr> Sinks { get; }
		public List<Keys> BroadcastKeys { get; }
		public List<Keys> ConditionalBroadcastKeys { get; }
		public bool ConditionalBroadcastOn { get; set; }

	public InputMultiplexer(ControlInterface.HostControl source, ICollection<IntPtr> sinks)
		{
			BroadcastKeys = new List<Keys>();
			ConditionalBroadcastKeys = new List<Keys>();

			this.source = source;
			this.Sinks = sinks;

			source.WindowMessageDispatched += MessageDispatcher;
		}

		~InputMultiplexer()
		{
			source.WindowMessageDispatched -= MessageDispatcher;
		}
			
		private void MessageDispatcher(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			if (BroadcastKeys.Contains((Keys)wParam))
			{
				foreach (IntPtr sink in Sinks)
				{
					SystemWin32.PostMessage(sink, msg, wParam, lParam);
				}
			}

			if (ConditionalBroadcastOn)
			{
				if (ConditionalBroadcastKeys.Contains((Keys)wParam))
				{
					foreach (IntPtr sink in Sinks)
					{
						SystemWin32.PostMessage(sink, msg, wParam, lParam);
					}
				}
			}
		}
	}
}
