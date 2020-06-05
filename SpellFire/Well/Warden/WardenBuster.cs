using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyHook;
using SpellFire.Well.Controller;
using SpellFire.Well.Util;

namespace SpellFire.Well.Warden
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void WardenScan(IntPtr outBuffer, IntPtr scanAddress, Int32 size);

	public class WardenBuster : IDisposable
	{
		private readonly byte[] WardenScanCode = {
			0x56, 0x57, 0xFC, 0x8B, 0x54,
			0x24, 0x14, 0x8B, 0x74, 0x24,
			0x10, 0x8B, 0x44, 0x24, 0x0C,
			0x8B, 0xCA, 0x8B, 0xF8, 0xC1,
			0xE9, 0x02, 0x74, 0x02, 0xF3,
			0xA5, 0xB1, 0x03, 0x23, 0xCA,
			0x74, 0x02, 0xF3, 0xA4, 0x5F,
			0x5E, 0xC3
		};

		private readonly Memory memory;

		private readonly ControlInterface.HostControl hc;

		private LocalHook virtualProtectPatch;
		private SystemWin32.VirtualProtectDelegate originalVirtualProtect;

		private IntPtr wardenScanAddress;
		private LocalHook wardenScanPatch;
		private WardenScan originalWardenScan;

		public WardenBuster(ControlInterface.HostControl hc)
		{
			this.hc = hc;

			memory = new Memory(Process.GetCurrentProcess());

			PatchVirtualProtect();
		}

		private void PatchVirtualProtect()
		{
			IntPtr vpAddress = SystemWin32.GetProcAddress(SystemWin32.GetModuleHandle("kernel32.dll"), "VirtualProtect");
			originalVirtualProtect = Marshal.GetDelegateForFunctionPointer<SystemWin32.VirtualProtectDelegate>(vpAddress);

			virtualProtectPatch = LocalHook.Create(
				vpAddress,
				new SystemWin32.VirtualProtectDelegate(VirtualProtectPatchHandler),
				this);

			virtualProtectPatch.ThreadACL.SetExclusiveACL(new Int32[] { });
		}

		bool VirtualProtectPatchHandler(IntPtr lpAddress, UIntPtr dwSize, SystemWin32.MemoryProtection flNewProtect,
			ref uint lpflOldProtect)
		{
			if (flNewProtect == SystemWin32.MemoryProtection.PAGE_EXECUTE_READ)
			{
				IntPtr wardenScan = FindWardenScan(lpAddress.GetUIntPtr(), dwSize.ToUInt32());
				if (wardenScan != IntPtr.Zero)
				{
					PatchWardenScan(wardenScan);
				}
			}

			return originalVirtualProtect(lpAddress, dwSize, flNewProtect, ref lpflOldProtect);
		}

		private IntPtr FindWardenScan(UIntPtr startAddress, UInt32 searchSpan)
		{
			long endOffset = startAddress.ToUInt32() + searchSpan - WardenScanCode.Length;
			for (UIntPtr address = startAddress; address.ToUInt32() < endOffset; address += 1)
			{
				byte[] readBytes = memory.Read(address.GetIntPtr(), WardenScanCode.Length);
				if (readBytes.SequenceEqual(WardenScanCode))
				{
					hc.PrintMessage($"Found Warden scan function at 0x{(int)address.ToUInt32():X}, offset from base module: {address - (int)startAddress.ToUInt32()}");
					return address.GetIntPtr();
				}
			}

			return IntPtr.Zero;
		}

		private void PatchWardenScan(IntPtr wardenScanAddress)
		{
			originalWardenScan = Marshal.GetDelegateForFunctionPointer<WardenScan>(wardenScanAddress);

			wardenScanPatch = LocalHook.Create(
				wardenScanAddress,
				new WardenScan(WardenScanPatchHandler), 
				this);

			wardenScanPatch.ThreadACL.SetExclusiveACL(new Int32[] { });
		}

		void WardenScanPatchHandler(IntPtr outBuffer, IntPtr scanAddress, Int32 size)
		{
			hc.PrintMessage($"Warden[{DateTime.Now}] scan: 0x{scanAddress.ToInt32():X}, size {size}");

			originalWardenScan(outBuffer, scanAddress, size);
		}

		public void Dispose()
		{
			virtualProtectPatch.Dispose();
			wardenScanPatch.Dispose();
			LocalHook.Release();
		}
	}
}
