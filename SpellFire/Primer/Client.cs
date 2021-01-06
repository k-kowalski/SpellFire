using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
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
		public ClientLaunchSettings LaunchSettings { get; set; }

		private Queue<SpellCast> prioritySpellcastsQueue = new Queue<SpellCast>();

		public Client(Process process, ClientLaunchSettings launchSettings)
		{
			Process = process;
			LaunchSettings = launchSettings;
			Memory = new Memory(this.Process);

			InjectClient();
		}

		private void InjectClient()
		{
			this.ControlInterface = new ControlInterface();

			string channelName = null;
			EasyHook.RemoteHooking.IpcCreateServer(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, this.ControlInterface);

			string injectionLibraryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), SFConfig.Global.DllName);
			EasyHook.RemoteHooking.Inject(this.Process.Id, injectionLibraryPath, null,
				channelName, SFConfig.Global);

			this.LuaEventListener = new LuaEventListener(this.ControlInterface);
		}

		public bool GetObjectMgrAndPlayer()
		{
			bool isInWorld = IsInWorld();
			if (isInWorld)
			{
				if (!LuaEventListener.Active)
				{
					/* when entering world */
					LuaEventListener.Active = true;
				}
				IntPtr clientConnection = Memory.ReadPointer32(IntPtr.Zero + Offset.ClientConnection);
				IntPtr objectManagerAddress = Memory.ReadPointer32(clientConnection + Offset.GameObjectManager);
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

		public ThreatInfo GetUnitThreat(GameObject unit)
		{
			var info = new ThreatInfo();
			var playerGuid = Player.GUID;

			byte status = 0;
			ControlInterface.remoteControl.CGUnit_C__CalculateThreat(
				unit.GetAddress(),
				ref playerGuid, ref status, ref info.ThreatPct, ref info.ThreatPctRaw, ref info.TotalThreatValue);

			info.Status = (ThreatStatus)status;

			return info;
		}

		public bool IsOnCooldown(string spellName)
		{
			string result = ExecLuaAndGetResult($"start = GetSpellCooldown(\"{spellName}\")", "start");
			if (String.IsNullOrEmpty(result))
			{
				return false;
			}
			else
			{
				return result[0] != '0';
			}
		}

		public bool CanBeCasted(string spellName)
		{
			bool alwaysCastable =
				SpellCast.AlwaysCastableSpells.Any(name => name == spellName);
			if (alwaysCastable)
				return true;

			else
			{
				string result = ExecLuaAndGetResult($"isUsable, notEnoughMana = IsUsableSpell(\"{spellName}\")",
					"isUsable");
				if (String.IsNullOrEmpty(result))
				{
					return false;
				}
				else
				{
					return result[0] == '1';
				}
			}
		}

		/* remaining cooldown in seconds */
		public float GetRemainingCooldown(string spellName)
		{
			string result = ExecLuaAndGetResult(
				$"start, duration = GetSpellCooldown(\"{spellName}\"); res = (start + duration) - GetTime()",
				"res");
			if (String.IsNullOrEmpty(result))
			{
				return 0f;
			}
			else
			{
				float resSingle = Single.Parse(result);
				return Math.Max(resSingle, 0f);
			}
		}

		public bool HasAura(GameObject gameObject, string auraName, GameObject ownedBy = null)
		{
			int spellId = GetSpellId(auraName);
			if (spellId == 0)
			{
				return HasAuraEx(gameObject, auraName, ownedBy);
			}

			return HasAura(gameObject, spellId, ownedBy);
		}

		public bool HasAura(GameObject gameObject, Int32 spellId, GameObject ownedBy = null)
		{
			foreach (var aura in gameObject.Auras)
			{
				if (aura.auraID <= 0)
				{
					continue;
				}

				if (aura.auraID == spellId)
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

		public bool HasAuraEx(GameObject gameObject, string auraName, GameObject ownedBy = null)
		{
			foreach (var aura in gameObject.Auras)
			{
				if (aura.auraID <= 0)
				{
					continue;
				}

				if (auraName == ExecLuaAndGetResult(
				$"name = GetSpellInfo({aura.auraID})", "name"))
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
			return Player.TargetGUID;
		}

		public void CastSpell(string spellName)
		{
			ControlInterface.remoteControl.FrameScript__Execute($"CastSpellByName(\"{spellName}\")", 0, 0);
		}

		public void CastSpellOnGuid(string spellName, Int64 targetGuid)
		{
			int spellId = GetSpellId(spellName);
			ControlInterface.remoteControl.Spell_C__CastSpell(spellId, IntPtr.Zero,
				targetGuid, false);
		}

		public void CastSpellOnGuid(int spellId, Int64 targetGuid)
		{
			ControlInterface.remoteControl.Spell_C__CastSpell(spellId, IntPtr.Zero,
				targetGuid, false);
		}

		public string ExecLuaAndGetResult(string luaScript, string resultLuaVariable)
		{
			ExecLua(luaScript);
			return ControlInterface.remoteControl.FrameScript__GetLocalizedText(Player.GetAddress(), resultLuaVariable, 0);
		}

		public void ExecLua(string luaScript)
		{
			ControlInterface.remoteControl.FrameScript__Execute(luaScript, 0, 0);
		}

		public bool IsInWorld()
		{
			return Memory.ReadInt32(IntPtr.Zero + Offset.WorldLoaded) == 1;
		}

		public void RefreshLastHardwareEvent()
		{
			Memory.Write(IntPtr.Zero + Offset.LastHardwareEvent, BitConverter.GetBytes(Environment.TickCount));
		}

		public void EnqueuePrioritySpellCast(SpellCast spellCast)
		{
			lock (this)
			{
				prioritySpellcastsQueue.Enqueue(spellCast);
			}
		}

		public bool CastPrioritySpell()
		{
			lock (this)
			{
				if (prioritySpellcastsQueue.Any())
				{
					SpellCast pendingSpellCast = prioritySpellcastsQueue.Peek();

					float remainingCdSecs = GetRemainingCooldown(pendingSpellCast.SpellName);

					const float PrioritySpellCooldownThreshold = 2.5f;

					if (remainingCdSecs < PrioritySpellCooldownThreshold && CanBeCasted(pendingSpellCast.SpellName))
					{
						int spellId = GetSpellId(pendingSpellCast.SpellName);
						if (Player.CastingSpellId == spellId || Player.ChannelSpellId == spellId)
						{
							/* do not interrupt spell wanted to be casted */
							prioritySpellcastsQueue.Dequeue();
							return false;
						}

						ExecLua("SpellStopCasting()");
						if (!IsOnCooldown(pendingSpellCast.SpellName))
						{
							if (pendingSpellCast.Coordinates != null)
							{
								/* terrain targeted spell */
								var terrainClick = new TerrainClick { Coordinates = pendingSpellCast.Coordinates.Value };
								CastSpell(pendingSpellCast.SpellName);
								ControlInterface.remoteControl.Spell_C__HandleTerrainClick(ref terrainClick);
							}
							else
							{
								CastSpellOnGuid(spellId, pendingSpellCast.TargetGUID);
							}

							prioritySpellcastsQueue.Dequeue();
							return true;
						}
						else
						{
							/* try again ASAP */
							return true;
						}
					}
					else
					{
						Console.WriteLine($"Spell [{pendingSpellCast.SpellName}] beyond cooldown threshold(left {remainingCdSecs}s) or not usable. Not prioritizing.");
						prioritySpellcastsQueue.Dequeue();
					}
				}
			}

			return false;
		}

		private Int32 GetSpellId(string spellName)
		{
			string spellLink = ExecLuaAndGetResult(
				$"link = GetSpellLink(\"{spellName}\")",
				"link");
			if (String.IsNullOrEmpty(spellLink))
			{
				return 0;
			}
			string spellID = spellLink.Split('|')[2].Split(':')[1];

			return Int32.Parse(spellID);
		}
	}
}
