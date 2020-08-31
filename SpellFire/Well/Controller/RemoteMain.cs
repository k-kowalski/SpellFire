using EasyHook;
using SpellFire.Well.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

		public RemoteMain(RemoteHooking.IContext context, string channelName, GlobalConfig config)
		{
			ctrlInterface = RemoteHooking.IpcConnectClient<ControlInterface>(channelName);

			EstablishReverseRemotingConnection(channelName);

			commandHandler = new CommandHandler(ctrlInterface, config);

			packetManager = new PacketManager(ctrlInterface, commandHandler);

			commandHandler.DetourWndProc();

			wardenBuster = new WardenBuster(ctrlInterface.hostControl, commandHandler);

			ctrlInterface.hostControl.PrintMessage($"Ready");
		}

		public void Run(RemoteHooking.IContext context, string channelName, GlobalConfig config)
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

			try
			{
				var luaEventProcessingTask = Task.Run((() =>
				{
					while (true)
					{
						if (commandHandler.LuaEventQueue.TryTake(out var luaEvent, 100))
						{
							ctrlInterface.hostControl.LuaEventTrigger(luaEvent);
						}
					}
				}));
				var windowMsgProcessingTask = Task.Run((() =>
				{
					while (true)
					{
						if (commandHandler.WindowMessageQueue.TryTake(out var windowMessage, 100))
						{
							ctrlInterface.hostControl.DispatchWindowMessage(
								windowMessage.hWnd,
								windowMessage.msg,
								windowMessage.wParam,
								windowMessage.lParam
								);
						}
					}
				}));

				luaEventProcessingTask.Wait();
				windowMsgProcessingTask.Wait();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				commandHandler?.Dispose();

				endScenePatch?.Dispose();

				wardenBuster?.Dispose();
			}
		}

		private void EstablishReverseRemotingConnection(String channelName)
		{
			IDictionary properties = new Hashtable();
			properties["name"] = channelName;
			properties["portName"] = channelName + Guid.NewGuid().ToString("N");

			BinaryServerFormatterSinkProvider binaryProv = new BinaryServerFormatterSinkProvider
			{
				TypeFilterLevel = TypeFilterLevel.Full
			};

			ChannelServices.RegisterChannel(new IpcServerChannel(properties, binaryProv), false);
		}
	}
}
