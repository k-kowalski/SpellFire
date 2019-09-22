using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpellFire.Well.Controller
{
	public class CommandQueue
	{
		private Queue<Task<dynamic>> commandTasks = new Queue<Task<dynamic>>();

		public Ty Submit<Ty>(Func<dynamic> command)
		{
			Task<dynamic> commandTask = new Task<dynamic>(command);
			lock (commandTasks)
			{
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