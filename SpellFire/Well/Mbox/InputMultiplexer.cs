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

		public InputMultiplexer(ControlInterface.HostControl source, ICollection<IntPtr> sinks)
		{
			BroadcastKeys = new List<Keys>();

			this.source = source;
			this.Sinks = sinks;

			source.WindowMessageDispatched += MessageDispatcher;
		}

		~InputMultiplexer()
		{
			try
			{
				source.WindowMessageDispatched -= MessageDispatcher;
			}
			catch (RemotingException e)
			{
				Console.WriteLine(e);
			}
		}
			
		private void MessageDispatcher(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			if (msg == SystemWin32.WM_KEYUP || msg == SystemWin32.WM_KEYDOWN)
			{
				if (BroadcastKeys.Contains((Keys)wParam))
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
