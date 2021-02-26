using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2.Behaviours
{
	using BTFunc = Func<BTStatus>;

	public abstract class BTNode
	{
		public abstract BTStatus Execute();

		public virtual void ResetState() {}
	}

	public class LeafAction : BTNode
	{
		public BTFunc action;
		public LeafAction(BTFunc action)
		{
			this.action = action;
		}

		public override BTStatus Execute() => action.Invoke();
	}

	public class Decorator : BTNode
	{
		public Func<bool> check;
		public BTNode decorated;
		public Decorator(Func<bool> check, BTNode decorated)
		{
			this.check = check;
			this.decorated = decorated;
		}

		public override BTStatus Execute() => decorated.Execute();

		public bool CanExecute() => check.Invoke();

		public override void ResetState()
		{
			decorated.ResetState();
		}
	}

	/// <summary>
	/// Executes nodes in sequence
	/// </summary>
	public class Sequence : BTNode
	{
		public BTNode[] children;
		public int currentChildIndex;

		public Sequence(params BTNode[] children)
		{
			this.children = children;
		}

		public override BTStatus Execute()
		{
			for (int i = currentChildIndex; i < children.Length; i++)
			{
				currentChildIndex = i;
				BTStatus status = children[i].Execute();

				if (status != BTStatus.Success)
				{
					return status;
				}
			}

			currentChildIndex = 0;
			return BTStatus.Success;
		}

		public override void ResetState()
		{
			currentChildIndex = 0;
		}

		public void MoveCurrentNode(int offset)
		{
			currentChildIndex += offset;
		}
	}

	/// <summary>
	/// Execute first node, that can be executed
	/// </summary>
	public class Selector : BTNode
	{
		public Decorator[] children;
		public int? lastRunningChildIndex;

		public Selector(params Decorator[] children)
		{
			this.children = children;
		}

		public override BTStatus Execute()
		{
			for (int i = 0; i < children.Length; i++)
			{
				if (!children[i].CanExecute())
				{
					continue;
				}

				if (lastRunningChildIndex != null && i != lastRunningChildIndex)
				{
					children[lastRunningChildIndex.Value].ResetState();
				}

				BTStatus status = children[i].Execute();


				if (status == BTStatus.Success)
				{
					lastRunningChildIndex = null;
					return status;
				}
				else if (status == BTStatus.Running)
				{
					lastRunningChildIndex = i;
					return status;
				}
			}

			lastRunningChildIndex = null;
			return BTStatus.Failed;
		}
	}

	public class Parallel : BTNode
	{
		private readonly int childrenTickInterval;
		private BTStatus lastStatus;
		public BTNode[] children;
		public List<Task<BTStatus>> childrenTasks;

		public Parallel(int childrenTickInterval, params BTNode[] children)
		{
			this.childrenTickInterval = childrenTickInterval;
			this.children = children;

			lastStatus = BTStatus.Failed; /* allow tasks to initialize */
		}

		public override BTStatus Execute()
		{
			if (lastStatus == BTStatus.Success || lastStatus == BTStatus.Failed)
			{
				InitializeAndRunTasks();
			}

			if (childrenTasks.All(task => task.IsCompleted))
			{
				if (childrenTasks.Any(task => task.Result == BTStatus.Failed))
				{
					lastStatus = BTStatus.Failed;
					return BTStatus.Failed;
				}
				else
				{
					lastStatus = BTStatus.Success;
					return BTStatus.Success;
				}
			}

			lastStatus = BTStatus.Running;
			return BTStatus.Running;
		}

		private void InitializeAndRunTasks()
		{
			foreach (var child in children)
			{
				childrenTasks.Add(Task.Run(() =>
				{
					BTStatus status;
					do
					{
						status = child.Execute();
						Thread.Sleep(childrenTickInterval);
					} while (status == BTStatus.Running);

					return status;
				}));
			}
		}
	}

	public enum BTStatus
	{
		Failed,
		Success,
		Running,
	}

	public abstract class BehaviourTree
	{
		protected BTNode root;


		public virtual BTStatus Eval() => root.Execute();
		public virtual void Cmd(IList<string> args)
		{
			Console.WriteLine("Tree's command handler not implemented!");
		}
	}

	
}
