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

		public GameObject(Memory memory, IntPtr address) : base(memory, address) { }
	}
}
