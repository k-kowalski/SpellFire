using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Net
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public abstract class Packet
	{
		public UInt32 opcode;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class SpellPacket : Packet
	{ 
		public byte castCount;
		public UInt16 spellID;
		public byte castFlags;
		public UInt32 targetMask;
		public UInt32 __unknown1;
		public UInt16 __unknown2;

		public SpellPacket()
		{
			opcode = Opcode.CMSG_CAST_SPELL;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class UpdateRaidMarkPacket : Packet
	{
		public byte raidMark;
		public Int64 targetGUID;

		public UpdateRaidMarkPacket()
		{
			opcode = Opcode.MSG_RAID_TARGET_UPDATE;
		}
	}
}
