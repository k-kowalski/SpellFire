using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Primer.Solutions;

namespace SpellFire.Primer.Solutions
{
	public abstract class MultiboxSolution : Solution
	{
		protected IEnumerable<Client> slaves;

		protected MultiboxSolution(IEnumerable<Client> clients) : base(clients.First())
		{
			slaves = clients.Skip(1);
		}
	}
}
