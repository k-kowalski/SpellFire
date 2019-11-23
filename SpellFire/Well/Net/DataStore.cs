using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Net
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class DataStore
	{
		public UInt32 ptrDataStore = 0x6AECB8;
		public IntPtr packet;
		public Int32 __unknown1;
		public Int32 mayType = 0x100;
		public Int32 packetLength;
		public Int32 __unknown2;
	}
}
