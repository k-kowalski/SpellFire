using EasyHook;
using SpellFire.Well.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Threading.Tasks;
using SpellFire.Well.Lua;
using SpellFire.Well.Net;
using SpellFire.Well.Warden;

namespace SpellFire.Well.Controller
{
	public class RemoteMain : IEntryPoint
	{
		private readonly ControlInterface ctrlInterface;
		private readonly CommandHandler commandHandler;
		private readonly PacketManager packetManager;

		private readonly Warden.WardenBuster wardenBuster;

		private bool remoteMainOn = true;

		public RemoteMain(RemoteHooking.IContext context, GlobalConfig config)
		{
			try
			{
				ctrlInterface = new ControlInterface();

				SetupRemotingServer();

				commandHandler = new CommandHandler(ctrlInterface, config);

				packetManager = new PacketManager(ctrlInterface, commandHandler);

				commandHandler.DetourWndProc();

				wardenBuster = new WardenBuster(ctrlInterface.hostControl, commandHandler);

				ctrlInterface.hostControl.PrintMessage($"Ready");
			}
			catch (Exception e)
			{
				ctrlInterface.hostControl.PrintMessage(e.ToString());
			}
		}


		public void Run(RemoteHooking.IContext context, GlobalConfig config)
		{
			LocalHook endScenePatch = null;

			try
			{
				endScenePatch = LocalHook.Create(
					IntPtr.Zero + Offset.Function.EndScene,
					new CommandCallback.EndScene(commandHandler.EndScenePatch),
					this);

				endScenePatch.ThreadACL.SetExclusiveACL(new Int32[] { });
			}
			catch (Exception e)
			{
				ctrlInterface.hostControl.PrintMessage(e.ToString());
			}

			/* keep the remote from unloading */
			while (remoteMainOn)
			{
				Thread.Sleep(5000);
			}

			commandHandler?.Dispose();
			endScenePatch?.Dispose();
			wardenBuster?.Dispose();
		}

		private void SetupRemotingServer()
		{
			string channelName = $"sfRpc{Process.GetCurrentProcess().Id}";
			
			SFUtil.RegisterRemoteServer(channelName);

			RemotingServices.Marshal(ctrlInterface, channelName);
		}
	}
}
