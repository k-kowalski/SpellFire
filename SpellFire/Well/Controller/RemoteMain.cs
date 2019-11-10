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

namespace SpellFire.Well.Controller
{
	public class RemoteMain : IEntryPoint
	{
		private ControlInterface ctrlInterface = null;
		private Queue<string> messageQueue = new Queue<string>();
		private CommandHandler commandHandler;
		private IntPtr EndSceneAddress;

		public RemoteMain(RemoteHooking.IContext context, string channelName)
		{
			ctrlInterface = RemoteHooking.IpcConnectClient<ControlInterface>(channelName);
			messageQueue.Enqueue("registered client");

			EstablishReverseConnection(channelName);

			EndSceneAddress = GetEndSceneAddress();
			commandHandler = new CommandHandler(ctrlInterface, EndSceneAddress);
		}

		public void Run(RemoteHooking.IContext context, string channelName)
		{
			LocalHook endScenePatch = null;
			LocalHook invalidPtrPatch = null;
			LocalHook unregisterPatch = null;

			try
			{
				endScenePatch = LocalHook.Create(
					EndSceneAddress,
					new CommandCallback.EndScene(commandHandler.EndScenePatch),
					this);

				endScenePatch.ThreadACL.SetExclusiveACL(new Int32[] { });
			}
			catch (Exception e)
			{
				this.messageQueue.Enqueue(e.ToString());
			}

			try
			{
				invalidPtrPatch = LocalHook.Create(
					IntPtr.Zero + Offset.InvalidPtrCheck,
					new CommandCallback.InvalidPtrCheck(commandHandler.InvalidPtrCheckPatch),
					this);

				invalidPtrPatch.ThreadACL.SetExclusiveACL(new Int32[] { });
			}
			catch (Exception e)
			{
				this.messageQueue.Enqueue(e.ToString());
			}

			try
			{
				while (true)
				{
					Thread.Sleep(500);

					string[] queued = null;
					lock (messageQueue)
					{
						queued = messageQueue.ToArray();
						messageQueue.Clear();
					}

					if (queued != null && queued.Length > 0)
					{
						ctrlInterface.hostControl.ReportMessages(queued);
					}
					else
					{
						ctrlInterface.hostControl.Ping();
					}
				}
			}
			catch
			{ }
			finally
			{
				commandHandler.Dispose();

				endScenePatch.Dispose();
				unregisterPatch.Dispose();
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

		private IntPtr GetEndSceneAddress()
		{

			IntPtr dxDeviceObject = Marshal.ReadIntPtr(IntPtr.Zero + Offset.dxDevice);
			IntPtr vTablePointer = Marshal.ReadIntPtr(dxDeviceObject + Offset.dxVirtualMethodTable);
			IntPtr vTableData = Marshal.ReadIntPtr(vTablePointer);

			/*
             * virtual method table consists of pointers so multiply by pointer size
             * since Marshal.ReadIntPtr counts in bytes 
             */
			return Marshal.ReadIntPtr(vTableData + (Offset.EndSceneVMTableIndex * IntPtr.Size));
		}
	}
}
