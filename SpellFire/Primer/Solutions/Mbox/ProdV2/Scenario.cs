using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Primer.Solutions.Mbox.ProdV2
{
	public struct ScenarioAction
	{
		public ScenarioAction(Func<bool> action)
		{
			this.action = action;
		}

		public Func<bool> action;
	}

	public abstract class Scenario
	{
		protected readonly ProdMboxV2 mbox;
		protected Queue<ScenarioAction> scenarioActions = new Queue<ScenarioAction>();
		public bool scenarioDone;

		protected Scenario(ProdMboxV2 mbox)
		{
			this.mbox = mbox;
		}

		public void Eval()
		{
			if (scenarioDone)
			{
				return;
			}

			if (!scenarioActions.Any())
			{
				Console.WriteLine("No actions left to perform. Scenario done.");
				scenarioDone = true;
				return;
			}

			var currentAction = scenarioActions.Peek();
			if (currentAction.action.Invoke())
			{
				scenarioActions.Dequeue();
			}
		}

		public abstract void Cmd(IList<string> args);
	}
}
