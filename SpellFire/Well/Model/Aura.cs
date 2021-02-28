using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Model
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Aura
	{
		public Int64 creatorGuid;
		public Int32 auraID;
		public byte flags;
		public byte level;
		public byte stackCount;
		public byte unknown1;
		public UInt32 duration;
		public UInt32 endTime;
	}
}
