using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EasyHook;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;
using SpellFire.Primer.Solutions;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	static class Primer
	{

		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length == 1 && args[0] == "-c")
			{
				SystemWin32.AllocConsole();
			}

			RunGui();
		}

		private static void RunGui()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
