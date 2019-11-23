using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Controller;

namespace SpellFire.Well.Net
{
	public class PacketManager : MarshalByRefObject
	{
		[NonSerialized]
		private readonly ControlInterface ci;
		[NonSerialized]
		private readonly CommandHandler ch;

		public PacketManager(ControlInterface ci, CommandHandler ch)
		{
			this.ci = ci;
			this.ch = ch;

			ci.remoteControl.SendPacketEvent += SendPacketHandler;
		}

		public void SendPacketHandler(Packet outboundPacket)
		{
			GCHandle packetHandle = default;
			try
			{
				DataStore dataStore = new DataStore();
				packetHandle = GCHandle.Alloc(outboundPacket, GCHandleType.Pinned);

				dataStore.packet = packetHandle.AddrOfPinnedObject();
				dataStore.packetLength = Marshal.SizeOf(outboundPacket);

				ch.ClientSendPacketHandler(ch.NetGetCurrentConnection(), dataStore);
			}
			finally
			{
				packetHandle.Free();
			}
		}
	}
}