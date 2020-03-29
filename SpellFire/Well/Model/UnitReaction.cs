using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Model
{
	public enum UnitReaction : Int32
	{
		Unknown = -1,
		Hated,
		Hostile,
		Unfriendly,
		Neutral,
		Friendly,
		Honored,
		Revered,
		Exalted,
	}
}
