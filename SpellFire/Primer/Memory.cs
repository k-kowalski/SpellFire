using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class Memory
	{
		private readonly IntPtr processHandle;
		private Int32 lastOpProcessedBytes;

		public Memory(Process process)
		{
			if (process == null)
			{
				throw new InvalidOperationException("Process is invalid.");
			}

			this.processHandle = SystemWin32.OpenProcess(SystemWin32.PROCESS_ALL_ACCESS, false, process.Id);
		}

		public byte[] Read(IntPtr address, Int32 size)
		{
			lastOpProcessedBytes = 0;
			byte[] buffer = new byte[size];
			SystemWin32.ReadProcessMemory(processHandle, address, buffer, buffer.Length, ref lastOpProcessedBytes);
			return buffer;
		}

		public bool Write(IntPtr address, byte[] buffer)
		{
			lastOpProcessedBytes = 0;
			return SystemWin32.WriteProcessMemory(processHandle, address, buffer, buffer.Length, ref lastOpProcessedBytes);
		}

		public string ReadString(IntPtr address)
		{
			throw new NotImplementedException();
		}

		public Ty ReadStruct<Ty>(IntPtr address) where Ty : struct
		{
			Type type = typeof(Ty).IsEnum ? Enum.GetUnderlyingType(typeof(Ty)) : typeof(Ty);

			byte[] data = Read(address, Marshal.SizeOf(type));

			Ty result;
			GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				result = (Ty) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
			}
			finally
			{
				handle.Free();
			}

			return result;
		}

		public Int64 ReadInt64(IntPtr address)
		{
			return BitConverter.ToInt64(Read(address, sizeof(Int64)), 0);
		}

		public Int32 ReadInt32(IntPtr address)
		{
			return BitConverter.ToInt32( Read(address, sizeof(Int32)) , 0);
		}

		public Int16 ReadInt16(IntPtr address)
		{
			return BitConverter.ToInt16(Read(address, sizeof(Int16)), 0);
		}

		public UInt64 ReadUInt64(IntPtr address)
		{
			return (UInt64)ReadInt64(address);
		}

		public UInt32 ReadUInt32(IntPtr address)
		{
			return (UInt32)ReadInt32(address);
		}

		public UInt16 ReadUInt16(IntPtr address)
		{
			return (UInt16)ReadInt16(address);
		}

		public IntPtr ReadPointer86(IntPtr address)
		{
			return new IntPtr( ReadInt32(address) );
		}
	}
}
