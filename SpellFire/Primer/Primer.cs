using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EasyHook;
using Primer;
using SpellFire.Well.Controller;

namespace SpellFire.Primer
{
	static class Primer
	{
		static string channelName = null;

		static void InjectWoW(Process wowProcess)
		{
			string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Well.dll");

			try
			{
				EasyHook.RemoteHooking.Inject(wowProcess.Id, injectionLibrary, null, channelName);
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("There was an error while injecting into target:");
				Console.ResetColor();
				Console.WriteLine(e.ToString());
			}
		}

		static Solution currentSolution;

		[STAThread]
		static void Main()
		{
			Process wowProcess = Process.GetProcessesByName("WoW").First();

			Console.WriteLine("Welcome to SpellFire!");
			Console.WriteLine("W to start");
			Console.WriteLine("Q to pause");

			ControlInterface ctrlInterface = new ControlInterface();
			RemoteHooking.IpcCreateServer(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, ctrlInterface);

			InjectWoW(wowProcess);

			Memory wowMemory = new Memory(wowProcess);

			while (true)
			{
				var key = Console.ReadKey();
				if (key.Key == ConsoleKey.W)
				{
					//StartSolution(new Fishing(ctrlInterface.remoteControl, wowMemory));
					StartSolution(new BalanceDruidFarm(ctrlInterface.remoteControl, wowMemory));
				}
				if (key.Key == ConsoleKey.Q)
				{
					if (currentSolution != null)
					{
						currentSolution.Stop();
					}
				}
			}
		}

		private static void StartSolution(Solution solution)
		{
			if (solution == null)
			{
				return;
			}

			if (currentSolution != null)
			{
				currentSolution.Stop();
			}

			currentSolution = solution;

			new Thread(() =>
			{
				while (solution.Active)
				{
					solution.Tick();
				}
				solution.Finish();

			}).Start();
		}

		private static void RunGUI()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}
}
