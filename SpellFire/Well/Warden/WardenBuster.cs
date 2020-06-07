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
	public delegate IntPtr WardenScan(IntPtr outBuffer, IntPtr scanAddress, Int32 scanSize);

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
		private CommandHandler commandHandler;

		private LocalHook virtualProtectPatch;
		private SystemWin32.VirtualProtectDelegate originalVirtualProtect;

		private LocalHook wardenScanPatch;
		private WardenScan originalWardenScan;

		private LocalHook invalidPtrCheckPatch;

		public WardenBuster(ControlInterface.HostControl hc, CommandHandler commandHandler)
		{
			this.hc = hc;
			this.commandHandler = commandHandler;

			memory = new Memory(Process.GetCurrentProcess());

			PatchInvalidPtrCheck();

			PatchVirtualProtect();
		}

		private void PatchInvalidPtrCheck()
		{
			invalidPtrCheckPatch = LocalHook.Create(
				IntPtr.Zero + Offset.Function.InvalidPtrCheck,
				new CommandCallback.InvalidPtrCheck(commandHandler.InvalidPtrCheckPatch),
				this);

			invalidPtrCheckPatch.ThreadACL.SetExclusiveACL(new Int32[] { });
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
					hc.PrintMessage($"Warden base module starts at 0x{lpAddress.ToInt32():X}, ends at 0x{(lpAddress + (int)dwSize.ToUInt32()).ToInt32():X}");
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
					hc.PrintMessage($"Found Warden scan function at 0x{(int)address.ToUInt32():X}, offset from base module: 0x{(address - (int)startAddress.ToUInt32()).ToUInt32():X}");
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

		private IntPtr WardenScanPatchHandler(IntPtr outBuffer, IntPtr scanAddress, Int32 scanSize)
		{
			bool overlapped = FixOverlappingModifications(scanAddress, scanSize);

			IntPtr res = originalWardenScan(outBuffer, scanAddress, scanSize);

			if (overlapped)
			{
				hc.PrintMessage($"Warden[{DateTime.Now}] Restoring hook...");
				PatchInvalidPtrCheck();
				hc.PrintMessage($"Warden[{DateTime.Now}] Hook restored");
			}

			return res;
		}

		private bool FixOverlappingModifications(IntPtr scanAddress, int size)
		{
			Int32 scanStart = scanAddress.ToInt32();
			Int32 scanEnd = scanAddress.ToInt32() + size;

			Int32 modStart = Offset.Function.InvalidPtrCheck;
			/*
				detoured x86 functions have first 5 bytes changed to
				JMP/CALL opcode and 4 bytes of jump address

				i.e. for JMP
				0xE9 AddressByte_1 AddressByte_2 AddressByte_3 AddressByte_4

			 */
			Int32 modEnd = Offset.Function.InvalidPtrCheck + 5;  

			if (scanEnd > modStart)
			{
				if (scanEnd < modEnd || scanStart < modEnd)
				{
					hc.PrintMessage($"Warden[{DateTime.Now}] Preventing scan at 0x{scanStart:X}, size {size}. Unhooking...");

					invalidPtrCheckPatch.Dispose();
					LocalHook.Release();
					return true;
				}
			}

			return false;
		}

		public void Dispose()
		{
			virtualProtectPatch.Dispose();
			wardenScanPatch.Dispose();
			invalidPtrCheckPatch.Dispose();
			LocalHook.Release();
		}
	}
}
