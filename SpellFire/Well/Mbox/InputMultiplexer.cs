using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Well.Controller;
using SpellFire.Well.Util;

namespace SpellFire.Well.Mbox
{
	public class InputMultiplexer : IDisposable
	{
		private const int inputPollingIntervalMs = 5;

		private readonly ControlInterface.RemoteControl source;
		public ICollection<IntPtr> Sinks { get; }
		public List<Keys> BroadcastKeys { get; }
		public List<Keys> ConditionalBroadcastKeys { get; }
		public bool ConditionalBroadcastOn { get; set; }


		private bool isTaskOn;
		private Task inputGrabDispatchTask;

		public InputMultiplexer(ControlInterface.RemoteControl source, ICollection<IntPtr> sinks)
		{
			BroadcastKeys = new List<Keys>();
			ConditionalBroadcastKeys = new List<Keys>();

			this.source = source;
			this.Sinks = sinks;

			inputGrabDispatchTask = Task.Run(() => 
			{
				isTaskOn = true;
				while (isTaskOn)
				{
					var msgs = source.GrabWindowMessages();
					if (msgs != null)
					{
						foreach (var msg in msgs)
						{
							DispatchWindowMessage(msg);
						}
					}

					Thread.Sleep(inputPollingIntervalMs);
				}
			});
		}

		public void Dispose()
		{
			isTaskOn = false;
			inputGrabDispatchTask.Wait();
		}
			
		private void DispatchWindowMessage(SystemWin32.WindowMessage wmsg)
		{
			if (BroadcastKeys.Contains((Keys)wmsg.wParam))
			{
				BroadcastMessage(wmsg);
			}

			if (ConditionalBroadcastOn && ConditionalBroadcastKeys.Contains((Keys)wmsg.wParam))
			{
				BroadcastMessage(wmsg);
			}
		}

		private void BroadcastMessage(SystemWin32.WindowMessage wmsg)
		{
			foreach (IntPtr sink in Sinks)
			{
				SystemWin32.PostMessage(sink, wmsg.msg, wmsg.wParam, wmsg.lParam);
			}
		}
	}
}
