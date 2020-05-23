using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using SpellFire.Well.Controller;
using SpellFire.Well.Lua;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class Client
	{
		public Process Process { get; }
		public Memory Memory { get; }
		public ControlInterface ControlInterface { get; private set; }
		public LuaEventListener LuaEventListener { get; private set; }
		public GameObjectManager ObjectManager { get; private set; }
		public GameObject Player { get; private set; }
		public string RoutineName { get; }

		public Client(Process process, Well.Util.Config config, string routineName)
		{
			Process = process;
			RoutineName = routineName;
			Memory = new Memory(this.Process);

			InjectClient(config);
		}

		private void InjectClient(Well.Util.Config config)
		{
			this.ControlInterface = new ControlInterface();

			string channelName = null;
			EasyHook.RemoteHooking.IpcCreateServer(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, this.ControlInterface);

			string injectionLibraryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Well.dll");
			EasyHook.RemoteHooking.Inject(this.Process.Id, injectionLibraryPath, null,
				channelName, config);

			this.LuaEventListener = new LuaEventListener(this.ControlInterface);
		}

		public bool GetObjectMgrAndPlayer()
		{
			bool isInWorld = IsInWorld();
			if (isInWorld)
			{
				if (!LuaEventListener.Active)
				{
					/* enable LEL when entering world */
					LuaEventListener.Active = true;
				}

				IntPtr clientConnection = Memory.ReadPointer86(IntPtr.Zero + Offset.ClientConnection);
				IntPtr objectManagerAddress = Memory.ReadPointer86(clientConnection + Offset.GameObjectManager);
				ObjectManager = new GameObjectManager(Memory, objectManagerAddress);
				Player = new GameObject(Memory, ControlInterface.remoteControl.ClntObjMgrGetActivePlayerObj());
			}
			else
			{
				if (LuaEventListener.Active)
				{
					/* disable LEL when exiting world */
					LuaEventListener.Active = false;
				}

				ObjectManager = null;
				Player = null;
			}

			return isInWorld;
		}

		public bool IsOnCooldown(string spellName)
		{
			string result = ExecLuaAndGetResult($"start = GetSpellCooldown('{spellName}')", "start");
			return result != null && result[0] != '0';
		}

		public bool HasAura(GameObject gameObject, string auraName, GameObject ownedBy = null)
		{
			var auras = gameObject.Auras;
			foreach (var aura in auras)
			{
				if (aura.auraID <= 0)
				{
					continue;
				}

				if (auraName == ExecLuaAndGetResult(
					    $"name = GetSpellInfo({aura.auraID})",
					    "name"))
				{
					if (ownedBy != null)
					{
						if (aura.creatorGuid == ownedBy.GUID)
						{
							return true;
						}
					}
					else
					{
						return true;
					}
				}
			}
			return false;
		}

		public Int64 GetTargetGUID()
		{
			return Memory.ReadInt64(IntPtr.Zero + Offset.TargetGUID);
		}

		public void CastSpell(string spellName)
		{
			ControlInterface.remoteControl.FrameScript__Execute($"CastSpellByName('{spellName}')", 0, 0);
		}

		public void CastSpellOnGuid(string spellName, Int64 targetGuid)
		{
			string spellLink = ExecLuaAndGetResult(
				$"link = GetSpellLink('{spellName}')",
				"link");
			string spellID = spellLink.Split('|')[2].Split(':')[1];
			ControlInterface.remoteControl.Spell_C__CastSpell(Int32.Parse(spellID), IntPtr.Zero,
				targetGuid, false);
		}

		public string ExecLuaAndGetResult(string luaScript, string resultLuaVariable)
		{
			ControlInterface.remoteControl.FrameScript__Execute(luaScript, 0, 0);
			return ControlInterface.remoteControl.FrameScript__GetLocalizedText(Player.GetAddress(), resultLuaVariable, 0);
		}

		public bool IsInWorld()
		{
			return Memory.ReadInt32(IntPtr.Zero + Offset.WorldLoaded) == 1;
		}
	}
}
