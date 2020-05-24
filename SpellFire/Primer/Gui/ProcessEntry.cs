using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Primer.Gui
{
	public class ProcessEntry
	{
		private Process process;

		public ProcessEntry(Process process)
		{
			this.process = process;
		}

		public Process GetProcess()
		{
			return process;
		}

		public static IEnumerable<ProcessEntry> GetRunningWoWProcessess()
		{
			return Process.GetProcessesByName("WoW").Select(wowProcess => new ProcessEntry(wowProcess));
		}

		public override string ToString()
		{
			return $"pid: {process.Id}";
		}
	}
}
