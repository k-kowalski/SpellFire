using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Model
{
	public enum DeathKnightRune : byte
	{
		Blood1,
		Blood2,

		Frost1,
		Frost2,

		Unholy1,
		Unholy2,
	}

	public struct DeathKnightRunesState
	{
		public Int32 bloodReady;
		public Int32 frostReady;
		public Int32 unholyReady;
	}
}
