using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SpellFire.Well.Util;

namespace SpellFire.Well.Controller
{
	public class CommandQueue
	{
		private readonly ControlInterface ctrlInterface;
		private Queue<Task<dynamic>> commandTasks = new Queue<Task<dynamic>>();

		/* allow those functions to execute when not in game world */
		private string[] worldUnloadedFunctionWhitelist = new[]
		{
			"FrameScript__ExecuteHandler"
		};

		public CommandQueue(ControlInterface ctrlInterface)
		{
			this.ctrlInterface = ctrlInterface;
		}

		private bool IsWorldLoaded()
		{
			return Marshal.ReadInt32(IntPtr.Zero + Offset.WorldLoaded) == 1;
		}

		public Ty Submit<Ty>(Func<dynamic> command)
		{
			bool worldUnloadedWhitelisted =
				worldUnloadedFunctionWhitelist.Any(funcName => command.Method.Name.Contains(funcName));

			Func <dynamic> commandSafeWrapper = () =>
			{
				/* as world is unloaded, fail all non-whitelisted commands that are already on command queue */
				if (!IsWorldLoaded())
				{
					if (!worldUnloadedWhitelisted)
					{
						return default(Ty);
					}
				}

				return command.Invoke();
			};

			Task<dynamic> commandTask = new Task<dynamic>(commandSafeWrapper);
			lock (commandTasks)
			{
				/* as world is unloaded, fail all non-whitelisted incoming commands */
				if (!IsWorldLoaded())
				{
					if (!worldUnloadedWhitelisted)
					{
						return default(Ty);
					}
				}

				commandTasks.Enqueue(commandTask);
			}

			commandTask.Wait();
			return commandTask.Result;
		}

		public void RunCommands()
		{
			lock (commandTasks)
			{
				foreach (var commandTask in commandTasks)
				{
					commandTask.RunSynchronously();
				}

				commandTasks.Clear();
			}
		}
	}
}