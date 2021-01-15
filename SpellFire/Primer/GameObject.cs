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
		public GameObject(Memory memory, IntPtr address) : base(memory, address) { }

		public Int64 GUID => memory.ReadInt64(address + Offset.GUID);
		public GameObjectType Type => memory.ReadStruct<GameObjectType>(address + Offset.Type);

		/*
		 * TODO: inheritance or maybe try composition
		 */

		public string WorldObjectName => memory.ReadString(
			memory.ReadPointer32(memory.ReadPointer32(address + 0x1A4) + 0x90));

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
					auraTableBase = memory.ReadPointer32(address + Offset.AuraTableBase2);
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
				IntPtr creatureEntryAddress = memory.ReadPointer32(address + Offset.CreatureEntryAddress);
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

		public UnitClass UnitClass
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				/* Unit class is 2nd byte in UnitBytes0 */
				return memory.ReadStruct<UnitClass>(unitInfo + Offset.UnitBytes0 + 1);
			}
		}

		public Int64 TargetGUID
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				return memory.ReadInt64(unitInfo + Offset.TargetGUID);
			}
		}

		public Int32 Health
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				return memory.ReadInt32(unitInfo + Offset.Health);
			}
		}

		public Int32 HealthPct
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				Int32 currentHealth = memory.ReadInt32(unitInfo + Offset.Health);
				Int32 maxHealth = memory.ReadInt32(unitInfo + Offset.MaxHealth);

				return (currentHealth * 100) / maxHealth;
			}
		}

		public Int32 Rage
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				int current = memory.ReadInt32(unitInfo + Offset.Power2);
				int max = memory.ReadInt32(unitInfo + Offset.MaxPower2);

				return (current * 100) / max;
			}
		}

		public Int32 Focus
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				int current = memory.ReadInt32(unitInfo + Offset.Power3);
				int max = memory.ReadInt32(unitInfo + Offset.MaxPower3);

				return (current * 100) / max;
			}
		}

		public Int32 Energy
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				int current = memory.ReadInt32(unitInfo + Offset.Power4);
				int max = memory.ReadInt32(unitInfo + Offset.MaxPower4);

				return (current * 100) / max;
			}
		}

		public Int32 ManaPct
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				int currentMana = memory.ReadInt32(unitInfo + Offset.Power1);
				int maxMana = memory.ReadInt32(unitInfo + Offset.MaxPower1);

				return maxMana != 0 ? (currentMana * 100) / maxMana : -1;
			}
		}

		public Int32 RunicPower
		{
			get
			{
				IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
				return memory.ReadInt32(unitInfo + Offset.Power7) / 10;
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
			IntPtr movInfo = memory.ReadPointer32(address + 216);
			return memory.ReadInt32(movInfo + 96) != 0;
		}

		public bool IsLootable()
		{
			IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
			Int32 flags = memory.ReadInt32(unitInfo + Offset.DynamicFlags);
			return (flags & (byte)DynamicUnitFlags.Lootable) != 0;
		}

		public bool IsTaggedByOther()
		{
			IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
			Int32 flags = memory.ReadInt32(unitInfo + Offset.DynamicFlags);
			return (flags & (byte)DynamicUnitFlags.TaggedByOther) != 0;
		}

		public bool IsMounted()
		{
			IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
			return memory.ReadInt32(unitInfo + Offset.MountDisplayID) > 0;
		}

		public bool IsInCombat()
		{
			IntPtr unitInfo = memory.ReadPointer32(address + Offset.Info);
			Int32 flags = memory.ReadInt32(unitInfo + Offset.Flags);
			return (flags & (int)UnitFlags.IsInCombat) != 0;
		}

		public bool IsAutoAttacking()
		{
			return memory.ReadInt32(address + Offset.IsAutoAttacking) != 0;
		}
	}
}
