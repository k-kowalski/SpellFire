using System;
using System.Text;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class GameObject : MemoryMappedObject
	{
		public Int64 GUID => memory.ReadInt64(address + Offset.GUID);
		public GameObjectType Type => (GameObjectType) memory.ReadInt32(address + Offset.Type);

		/*
		 * TODO: names better implementation(i.e. read till null-terminator)
		 */
		/*
		 * TODO: inheritance or maybe try composition
		 */
		private const Int32 NameProbeLength = 40;
		public string UnitName => Encoding.UTF8.GetString( memory.Read(memory.ReadPointer86(memory.ReadPointer86(address + 0x964) + 0x05C),
			NameProbeLength) );
		public string WorldObjectName => Encoding.UTF8.GetString(memory.Read(memory.ReadPointer86(memory.ReadPointer86(address + 0x1A4) + 0x90),
			NameProbeLength));
		public Int32 CastingSpellId => memory.ReadInt32(address + Offset.CastingSpellId);
		public Int32 ChannelSpellId => memory.ReadInt32(address + Offset.ChannelSpellId);
		public bool IsCastingOrChanneling() 
		{
			return this.CastingSpellId != 0 || this.ChannelSpellId != 0;
		}
		public Vector3 Coordinates => memory.ReadStruct<Vector3>(address + Offset.PositionX);
		public float GetDistance(GameObject other)
		{
			return (this.Coordinates - other.Coordinates).Length();
		}
		public bool IsMoving()
		{
			IntPtr movInfo = memory.ReadPointer86(address + 216);
			return memory.ReadInt32(movInfo + 96) != 0;
		}

		public bool IsAlive()
		{
			IntPtr unitInfo = memory.ReadPointer86(address + 0x8);
			Int32 health = memory.ReadInt32(unitInfo + (0x18 * 4));
			return health != 0;
		}

		public GameObject(Memory memory, IntPtr address) : base(memory, address) { }
	}
}
