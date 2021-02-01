using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Model
{
	[Flags]
	public enum CollisonFlag : uint
	{
		HitTestNothing = 0x0,
		HitTestBoundingModels = 0x1,
		HitTestWMO = 0x10,
		HitTestUnknown = 0x40,
		HitTestGround = 0x100,
		HitTestLiquid = 0x10000,
		HitTestUnknown2 = 0x20000,
		HitTestMovableObjects = 0x100000,
		HitTestLOS = HitTestWMO | HitTestBoundingModels | HitTestMovableObjects,
		HitTestGroundAndStructures = HitTestLOS | HitTestGround
	}
}
