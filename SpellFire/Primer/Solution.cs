using System;
using System.Drawing;
using SpellFire.Primer.Gui;
using SpellFire.Well.Controller;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public abstract class Solution : IDisposable
	{
		public const string SolutionAssemblyQualifier = "SpellFire.Primer.Solutions.";

		public bool Active { get; set; }

		protected readonly ControlInterface ci;
		protected readonly Memory memory;
		protected GameObjectManager objectManager;
		protected GameObject player;

		protected Solution(ControlInterface ci, Memory memory)
		{
			this.ci = ci;
			this.memory = memory;
		}

		protected bool GetObjectMgrAndPlayer()
		{
			bool isInWorld = IsInWorld();
			if (isInWorld)
			{
				IntPtr clientConnection = memory.ReadPointer86(IntPtr.Zero + Offset.ClientConnection);
				IntPtr objectManagerAddress = memory.ReadPointer86(clientConnection + Offset.GameObjectManager);
				objectManager = new GameObjectManager(memory, objectManagerAddress);
				player = new GameObject(memory, ci.remoteControl.ClntObjMgrGetActivePlayerObj());
			}
			else
			{
				objectManager = null;
				player = null;
			}

			return isInWorld;
		}

		protected bool IsOnCooldown(string spellName)
		{
			string result = ExecLuaAndGetResult($"start = GetSpellCooldown('{spellName}')", "start");
			return result != null && result[0] != '0';
		}

		protected bool HasAura(GameObject gameObject, string auraName, GameObject ownedBy = null)
		{
			int currentAuraIndex = 0;
			while (true)
			{
				IntPtr auraPtr = ci.remoteControl.CGUnit_C__GetAura(gameObject.GetAddress(), currentAuraIndex++);
				if (auraPtr == IntPtr.Zero)
				{
					return false;
				}

				Aura aura = memory.ReadStruct<Aura>(auraPtr);
				if (auraName == ExecLuaAndGetResult($"name = GetSpellInfo({aura.auraID})", "name"))
				{
					if (ownedBy != null)
					{
						if (aura.creatorGuid == ownedBy.GUID)
						{
							return true;
						}
						else
						{
							continue;
						}
					}
					else
					{
						return true;
					}
				}
			}
		}

		protected bool IsInWorld()
		{
			return memory.ReadInt32(IntPtr.Zero + Offset.WorldLoaded) == 1;
		}

		protected Int64 GetTargetGUID()
		{
			return memory.ReadInt64(IntPtr.Zero + Offset.TargetGUID);
		}

		protected void CastSpell(string spellName)
		{
			ci.remoteControl.FrameScript__Execute($"CastSpellByName('{spellName}')", 0, 0);
		}

		protected string ExecLuaAndGetResult(string luaScript, string resultLuaVariable)
		{
			ci.remoteControl.FrameScript__Execute(luaScript, 0, 0);
			return ci.remoteControl.FrameScript__GetLocalizedText(player.GetAddress(), resultLuaVariable, 0);
		}

		public abstract void Tick();
		public virtual void Stop()
		{
			this.Active = false;
		}
		public abstract void Dispose();

		public virtual void RenderRadar(RadarCanvas radarCanvas, Bitmap radarBackBuffer)
		{
			if (player != null && objectManager != null)
			{
				RadarCanvas.BasicRadar(radarCanvas, radarBackBuffer, player, objectManager, GetTargetGUID(), ci);
			}
		}
	}
}