using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class GameObject : MemoryMappedObject
	{
		public Int64 GUID => memory.ReadInt64(address + Offset.GUID);
		public GameObjectType Type => memory.ReadStruct<GameObjectType>(address + Offset.Type);

		/*
		 * TODO: inheritance or maybe try composition
		 */

		public string WorldObjectName => memory.ReadString(
			memory.ReadPointer86(memory.ReadPointer86(address + 0x1A4) + 0x90));

		public Int32 CastingSpellId => memory.ReadInt32(address + Offset.CastingSpellId);
		public Int32 ChannelSpellId => memory.ReadInt32(address + Offset.ChannelSpellId);

		public IEnumerable<Aura> Auras
		{
			get
			{
				IntPtr auraTableBase;
				Int32 auraTableCapacity = memory.ReadInt32(address + Offset.AuraTableCapacity1);
				if (auraTableCapacity == -1)
				{
					auraTableCapacity = memory.ReadInt32(address + Offset.AuraTableCapacity2);
					auraTableBase = memory.ReadPointer86(address + Offset.AuraTableBase2);
				}
				else
				{
					auraTableBase = address + Offset.AuraTableBase1;
				}

				Aura[] auras = new Aura[auraTableCapacity];
				for (int i = 0; i < auraTableCapacity; i++)
				{
					auras[i] = memory.ReadStruct<Aura>(auraTableBase + (i * Marshal.SizeOf<Aura>()));
				}

				return auras;
			}
		}

		public CreatureType UnitType
		{
			get
			{
				IntPtr creatureEntryAddress = memory.ReadPointer86(address + Offset.CreatureEntryAddress);
				if (creatureEntryAddress != IntPtr.Zero)
				{
					return memory.ReadStruct<CreatureType>(creatureEntryAddress + Offset.CreatureType);
				}
				else
				{
					return CreatureType.Unknown;
				}
			}
		}

		public Int32 Health
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer86(address + Offset.Info);
				return memory.ReadInt32(unitInfo + Offset.Health);
			}
		}

		public Int32 HealthPct
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer86(address + Offset.Info);
				Int32 currentHealth = memory.ReadInt32(unitInfo + Offset.Health);
				Int32 maxHealth = memory.ReadInt32(unitInfo + Offset.MaxHealth);

				return (currentHealth * 100) / maxHealth;
			}
		}

		public Int32 RunicPower
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer86(address + Offset.Info);
				return memory.ReadInt32(unitInfo + Offset.RunicPower) / 10;
			}
		}

		public float Rotation
		{
			get
			{
				return BitConverter.ToSingle(memory.Read(address + Offset.Rotation, sizeof(float)), 0);
			}
		}

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

		public bool IsLootable()
		{
			IntPtr unitInfo = memory.ReadPointer86(address + Offset.Info);
			Int32 flags = memory.ReadInt32(unitInfo + Offset.Flags);
			return (flags & (byte)DynamicUnitFlags.Lootable) != 0;
		}

		public bool IsTaggedByOther()
		{
			IntPtr unitInfo = memory.ReadPointer86(address + Offset.Info);
			Int32 flags = memory.ReadInt32(unitInfo + Offset.Flags);
			return (flags & (byte)DynamicUnitFlags.TaggedByOther) != 0;
		}

		public bool IsMounted()
		{
			IntPtr unitInfo = memory.ReadPointer86(address + Offset.Info);
			return memory.ReadInt32(unitInfo + Offset.MountDisplayID) > 0;
		}

		public GameObject(Memory memory, IntPtr address) : base(memory, address) { }
	}
}
