using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Util
{
	public abstract class TimelessMarshalByRefObject : MarshalByRefObject
	{
		/* prevent object expiration */
		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
