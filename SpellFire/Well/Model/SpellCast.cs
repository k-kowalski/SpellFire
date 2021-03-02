using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Util;

namespace SpellFire.Well.Model
{
	public class SpellCast
	{
		public string SpellName;
		public Int64 TargetGUID;
		public Vector3? Coordinates;

		/*  
			for some spells, in game we may have different target than real target of spell ie. CastSpellOnGuid
			hence it is displayed as non castable - override it
		*/
		public static readonly string[] AlwaysCastableSpells =
		{
			"Power Word: Shield",
			"Ressurection"
		};

		public override bool Equals(object obj)
		{
			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
			{
				return false;
			}
			else
			{
				SpellCast sc = (SpellCast)obj;
				return SpellName == sc.SpellName && TargetGUID == sc.TargetGUID;
			}
		}
	}
}
