using System;

namespace SpellFire.Well.Util
{
	/// <summary>
	/// Offsets for WoW 3.3.5a 12340
	///
	/// Docs directly above offsets tell
	/// from what given offset should be calculated
	/// 
	/// </summary>
	public static class Offset
	{
		public static class Function
		{
			public static Int32 EndScene;
			public const Int32 FrameScript__Execute = 0x819210;
			public const Int32 FrameScript__GetLocalizedText = 0x7225E0;
			public const Int32 CGPlayer_C__ClickToMove = 0x727400;
			public const Int32 CGPlayer_C__ClickToMoveStop = 0x72B3A0;
			public const Int32 ClntObjMgrGetActivePlayerObj = 0x4038F0;
			public const Int32 SelectUnit = 0x524BF0;
			public const Int32 InvalidPtrCheck = 0x86B5A0;
			public const Int32 LuaGetTop = 0x84DBD0;
			public const Int32 LuaToString = 0x84E0E0;
			public const Int32 FrameScript__RegisterFunction = 0x817F90;
			public const Int32 FrameScript__UnregisterFunction = 0x817FD0;
			public const Int32 WorldSendPacket = 0x406F40;
			public const Int32 ClientSendPacket = 0x632B50;
			public const Int32 NetGetCurrentConnection = 0x6B0970;
			public const Int32 CGUnit_C__UnitReaction = 0x7251C0;
			public const Int32 CGUnit_C__UpdateDisplayInfo = 0x73E410;
			public const Int32 Spell_C__CastSpell = 0x80DA40;
			public const Int32 Spell_C__HandleTerrainClick = 0x80C340;
			public const Int32 CGUnit_C__CalculateThreat = 0x7374C0;
			public const Int32 TraceLine = 0x7A3B70;
		}

		public static class VirtualFunction
		{
			/// <summary>
			/// game object
			/// </summary>
			public const Int32 InteractUnit = 0xB0;
			/// <summary>
			/// game object
			/// </summary>
			public const Int32 GetUnitName = 0xD8;
		}




		public const Int32 LastHardwareEvent = 0xB499A4;
		public const Int32 MouseoverGUID = 0xBD07A0;
		public const Int32 ClientConnection = 0xC79CE0;
		public const Int32 RuneCount = 0xC24388;
		public const Int32 PlayerGUID = 0xCA1238;
		public const Int32 WorldLoaded = 0xBD0792;
		public const Int32 MapId = 0xAB63BC;
		public const Int32 RaidTargets = 0xBEB528;
		public const Int32 ComboPoints = 0xBD084D;

		/// <summary>
		/// client connection
		/// </summary>
		public const Int32 GameObjectManager = 0x2ED0;
		/// <summary>
		/// game object manager
		/// </summary>
		public const Int32 FirstGameObject = 0xAC;

		/// <summary>
		/// game object
		/// </summary>
		public const Int32 Info = 0x8;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 SummonedGuid = 0x8 * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 DynamicFlags = 0x13C;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 TargetGUID = 0x12 * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 Health = 0x60;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 MaxHealth = 0x80;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 Power1 = 0x19 * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 Power2 = 0x1A * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 Power3 = 0x1B * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 Power4 = 0x1C * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 Power7 = 0x1F * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 MaxPower1 = 0x21 * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 MaxPower2 = 0x22 * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 MaxPower3 = 0x23 * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 MaxPower4 = 0x24 * 4;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 MountDisplayID = 0x114;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 PlayerItem1ID = 0x11B;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 UnitBytes0 = 0x5C;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 Flags = 0xEC;

		/// <summary>
		/// game object
		/// </summary>
		public const Int32 NextGameObject = 0x3C;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 GUID = 0x30;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 PositionX = 0x798;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 PositionY = 0x79C;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 PositionZ = 0x7A0;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 Rotation = 0x7A8;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 Type = 0x14;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 CastingSpellId = 0xA6C;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 ChannelSpellId = 0xA80;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 CreatureEntryAddress = 0x964;
		/// <summary>
		/// creature entry
		/// </summary>
		public const Int32 CreatureType = 0x10;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 AuraTableCapacity1 = 0xDD0;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 AuraTableBase1 = 0xC50;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 AuraTableCapacity2 = 0xC54;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 AuraTableBase2 = 0xC58;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 IsAutoAttacking = 0xA20;

		/// <summary>
		/// warden page check loop interior
		/// </summary>
		public const Int32 PageCheckReturnOffset = 0xF;

		public static class DirectX
		{
			public const Int32 EndSceneVMTableIndex = 42;
			public const Int32 Device = 0xC5DF88;

			/// <summary>
			/// Device
			/// </summary>
			public const Int32 VirtualMethodTable = 0x397C;
		}
	}
}
