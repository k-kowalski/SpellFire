using EasyHook;
using SpellFire.Well.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using SpellFire.Well.Net;

namespace SpellFire.Well.Controller
{
	public class RemoteMain : IEntryPoint
	{
		private readonly ControlInterface ctrlInterface;
		private readonly CommandHandler commandHandler;
		private readonly PacketManager packetManager;

		public RemoteMain(RemoteHooking.IContext context, string channelName)
		{
			ctrlInterface = RemoteHooking.IpcConnectClient<ControlInterface>(channelName);

			EstablishReverseConnection(channelName);

			commandHandler = new CommandHandler(ctrlInterface);

			packetManager = new PacketManager(ctrlInterface, commandHandler);

			commandHandler.DetourWndProc();

			ctrlInterface.hostControl.PrintMessage("Ready");
		}

		public void Run(RemoteHooking.IContext context, string channelName)
		{
			LocalHook endScenePatch = null;
			LocalHook invalidPtrPatch = null;

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

			try
			{
				invalidPtrPatch = LocalHook.Create(
					IntPtr.Zero + Offset.Function.InvalidPtrCheck,
					new CommandCallback.InvalidPtrCheck(commandHandler.InvalidPtrCheckPatch),
					this);

				invalidPtrPatch.ThreadACL.SetExclusiveACL(new Int32[] { });
			}
			catch (Exception e)
			{
				ctrlInterface.hostControl.PrintMessage(e.ToString());
			}

			try
			{
				while (true)
				{
					Thread.Sleep(500);

					ctrlInterface.hostControl.Ping();
				}
			}
			catch
			{ }
			finally
			{
				commandHandler.Dispose();

				endScenePatch.Dispose();
				invalidPtrPatch.Dispose();
				LocalHook.Release();
			}
		}

		private void EstablishReverseConnection(String channelName)
		{
			IDictionary properties = new Hashtable();
			properties["name"] = channelName;
			properties["portName"] = channelName + Guid.NewGuid().ToString("N");

			BinaryServerFormatterSinkProvider binaryProv = new BinaryServerFormatterSinkProvider();
			binaryProv.TypeFilterLevel = TypeFilterLevel.Full;

			IpcServerChannel _clientServerChannel = new IpcServerChannel(properties, binaryProv);
			ChannelServices.RegisterChannel(_clientServerChannel, false);
		}
	}
}
