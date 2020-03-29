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
		public const Int32 FrameScript__Execute = 0x819210;
		public const Int32 FrameScript__GetLocalizedText = 0x7225E0;
		public const Int32 CGPlayer_C__ClickToMove = 0x727400;
		public const Int32 CGPlayer_C__ClickToMoveStop = 0x72B3A0;
		public const Int32 ClntObjMgrGetActivePlayerObj = 0x4038F0;
		public const Int32 SelectUnit = 0x524BF0;
		/// <summary>
		/// game object
		/// </summary>
		public const Int32 InteractUnit = 0xB0;
		public const Int32 InvalidPtrCheck = 0x86B5A0;
		public const Int32 LuaGetTop = 0x84DBD0;
		public const Int32 LuaToString = 0x84E0E0;
		public const Int32 FrameScript__RegisterFunction = 0x817F90;
		public const Int32 FrameScript__UnregisterFunction = 0x817FD0;
		public const Int32 WorldSendPacket = 0x406F40;
		public const Int32 ClientSendPacket = 0x632B50;
		public const Int32 NetGetCurrentConnection = 0x6B0970;
		public const Int32 CGUnit_C__UnitReaction = 0x7251C0;
		public const Int32 CGUnit_C__GetAura = 0x556E10;







		public const Int32 TargetGUID = 0xBD07B0;
		public const Int32 MouseoverGUID = 0xBD07A0;
		public const Int32 ClientConnection = 0xC79CE0;
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
		public const Int32 Flags = 0x13C;
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
		public const Int32 RunicPower = 0x7C;
		/// <summary>
		/// info
		/// </summary>
		public const Int32 MountDisplayID = 0x114;

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





		public const Int32 EndSceneVMTableIndex = 42;
		public const Int32 dxDevice = 0xC5DF88;
		/// <summary>
		/// dxDevice
		/// </summary>
		public const Int32 dxVirtualMethodTable = 0x397C;
	}
}
