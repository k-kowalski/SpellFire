using System.Collections.Generic;
using System.Collections.ObjectModel;
using SpellFire.Well.Util;

namespace SpellFire.Well.Lua
{
	public class LuaEventArgs : TimelessMarshalByRefObject
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
