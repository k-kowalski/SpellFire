using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Model
{
	public enum DynamicUnitFlags : byte
	{
		Lootable = 1,
		TrackUnit = 2,
		TaggedByOther = 4,
		TaggedByMe = 8,
		SpecialInfo = 16,
		Dead = 32,
		ReferAFriendLinked = 64,
		TappedByThreat = 128,
	}
}
