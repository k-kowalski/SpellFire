using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.LuaEvents
{
	public class LuaEventArgs : MarshalByRefObject
	{
		public string Name { get; }

		public ReadOnlyCollection<string> Args { get; }

		public LuaEventArgs(List<string> rawLuaArgs)
		{
			this.Name = rawLuaArgs[0];
			this.Args = rawLuaArgs.GetRange(1, rawLuaArgs.Count - 1).AsReadOnly(); /* TODO: span */
		}
	}
}
