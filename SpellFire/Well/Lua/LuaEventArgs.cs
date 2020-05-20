using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SpellFire.Well.Util;

namespace SpellFire.Well.Lua
{
	public class LuaEventArgs : TimelessMarshalByRefObject
	{
		public string Name { get; }

		public IList<string> Args { get; }

		public LuaEventArgs(IList<string> rawLuaArgs)
		{
			this.Name = rawLuaArgs[0];
			this.Args = new List<string>(rawLuaArgs.Skip(1));
		}
	}
}
