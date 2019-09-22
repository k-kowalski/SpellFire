using System;

namespace SpellFire.Primer
{
	public abstract class MemoryMappedObject
	{
		protected readonly Memory memory;
		protected readonly IntPtr address;

		public MemoryMappedObject(Memory memory, IntPtr address)
		{
			this.memory = memory;
			this.address = address;
		}

		public IntPtr GetAddress()
		{
			return address;
		}
	}
}
