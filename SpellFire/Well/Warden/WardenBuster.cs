using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
		private readonly byte[] WardenScanMemoryCode = {
			0x56, 0x57, 0xFC, 0x8B, 0x54,
			0x24, 0x14, 0x8B, 0x74, 0x24,
			0x10, 0x8B, 0x44, 0x24, 0x0C,
			0x8B, 0xCA, 0x8B, 0xF8, 0xC1,
			0xE9, 0x02, 0x74, 0x02, 0xF3,
			0xA5, 0xB1, 0x03, 0x23, 0xCA,
			0x74, 0x02, 0xF3, 0xA4, 0x5F,
			0x5E, 0xC3
		};

		private readonly byte[] WardenPageCheckCode = {
			0x8B, 0x45, 0x08, 0x8A, 0x04, 0x07,
			0x88, 0x44, 0x3E, 0x1C, 0x47, 0x3B,
			0xFB, 0x72, 0xF1, 0x5F, 0x5B, 0xC9,
			0xC2, 0x04, 0x00
		};

		private readonly Memory memory;

		private readonly ControlInterface.HostControl hc;
		private readonly CommandHandler commandHandler;

		private LocalHook virtualProtectPatch;
		private SystemWin32.VirtualProtectDelegate originalVirtualProtect;

		private LocalHook virtualQueryPatch;
		private SystemWin32.VirtualQueryDelegate originalVirtualQuery;

		private LocalHook wardenScanPatch;
		private WardenScan originalWardenScan;

		private LocalHook invalidPtrCheckPatch;

		private readonly uint thisModuleBase;
		private readonly uint thisModuleEnd;

		private PageCheckHook wardenPageCheckPatch;

		public WardenBuster(ControlInterface.HostControl hc, CommandHandler commandHandler)
		{
			this.hc = hc;
			this.commandHandler = commandHandler;

			memory = new Memory(Process.GetCurrentProcess());

			/* our host module is EasyHook32.dll */
			ProcessModule thisModule = Process.GetCurrentProcess().GetModule("EasyHook32.dll");
			thisModuleBase = (uint)thisModule.BaseAddress.ToInt32();
			thisModuleEnd = (uint)thisModule.BaseAddress.ToInt32() + (uint)thisModule.ModuleMemorySize;

			PatchInvalidPtrCheck();

			PatchVirtualQuery();
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

		private bool VirtualProtectPatchHandler(IntPtr lpAddress, UIntPtr dwSize, SystemWin32.MemoryProtection flNewProtect,
			ref uint lpflOldProtect)
		{
			if (flNewProtect == SystemWin32.MemoryProtection.PAGE_EXECUTE_READ)
			{
				IntPtr wardenMemoryScan =
					FindWardenSignature(lpAddress.GetUIntPtr(), dwSize.ToUInt32(), WardenScanMemoryCode);
				if (wardenMemoryScan != IntPtr.Zero)
				{
					hc.PrintMessage($"Found Warden Memory Scan function at 0x{wardenMemoryScan.ToInt32():X}, offset from base module: 0x{(wardenMemoryScan.ToInt32() - lpAddress.ToInt32()):X}");

					wardenScanPatch?.Dispose();
					PatchWardenScan(wardenMemoryScan);
				}

				IntPtr wardenPageCheck = FindWardenSignature(lpAddress.GetUIntPtr(), dwSize.ToUInt32(), WardenPageCheckCode);
				if (wardenPageCheck != IntPtr.Zero)
				{
					hc.PrintMessage($"Found Warden Page Check code at 0x{wardenPageCheck.ToInt32():X}, offset from base module: 0x{(wardenPageCheck.ToInt32() - lpAddress.ToInt32()):X}");
					wardenPageCheckPatch?.Dispose();
					wardenPageCheckPatch = new PageCheckHook(memory, PageCheckPatchHandler, wardenPageCheck);
				}

				if (wardenMemoryScan != IntPtr.Zero || wardenPageCheck != IntPtr.Zero)
				{
					hc.PrintMessage($"Warden base module starts at 0x{lpAddress.ToInt32():X}, size {dwSize.ToUInt32()}");
				}
			}


			return originalVirtualProtect(lpAddress, dwSize, flNewProtect, ref lpflOldProtect);
		}

		private void PatchVirtualQuery()
		{
			IntPtr vqAddress = SystemWin32.GetProcAddress(SystemWin32.GetModuleHandle("kernel32.dll"), "VirtualQueryEx");
			originalVirtualQuery = Marshal.GetDelegateForFunctionPointer<SystemWin32.VirtualQueryDelegate>(vqAddress);

			virtualQueryPatch = LocalHook.Create(
				vqAddress,
				new SystemWin32.VirtualQueryDelegate(VirtualQueryPatchHandler), 
				this);

			virtualQueryPatch.ThreadACL.SetExclusiveACL(new Int32[] { });
		}

		private int VirtualQueryPatchHandler(IntPtr lpAddress, ref SystemWin32.MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength)
		{
			var res = originalVirtualQuery(lpAddress, ref lpBuffer, dwLength);

			UInt32 regionBase = (uint)lpBuffer.BaseAddress.ToInt32();
			UInt32 regionEnd = (uint)lpBuffer.BaseAddress.ToInt32() + (uint)lpBuffer.RegionSize.ToInt32();

			if (regionBase >= thisModuleBase && regionEnd <= thisModuleEnd)
			{
				hc.PrintMessage($"Warden[{DateTime.Now}] Querying occupied address: {lpAddress}, shadowing region as MEM_FREE & PAGE_NOACCESS");
				lpBuffer.State = SystemWin32.MemoryState.MEM_FREE;
				lpBuffer.Protect = SystemWin32.MemoryProtection.PAGE_NOACCESS;
			}

			return res;
		}

		private IntPtr FindWardenSignature(UIntPtr startAddress, UInt32 searchSpan, byte[] signature)
		{
			long endOffset = startAddress.ToUInt32() + searchSpan - signature.Length;
			for (UIntPtr address = startAddress; address.ToUInt32() < endOffset; address += 1)
			{
				byte[] readBytes = memory.Read(address.GetIntPtr(), signature.Length);
				if (readBytes.SequenceEqual(signature))
				{
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
				PatchInvalidPtrCheck();
				commandHandler.InitializeLuaEventFrameHandler_W();
			}

			return res;
		}

		private void PageCheckPatchHandler(IntPtr src, int offset, int size, IntPtr dest)
		{
			bool overlapped = FixOverlappingModifications(src + offset, size);

			memory.Write(dest + offset, memory.Read(src + offset, size - offset));

			if (overlapped)
			{
				PatchInvalidPtrCheck();
				commandHandler.InitializeLuaEventFrameHandler_W();
			}
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
				0xE9 AddressByte[0] AddressByte[1] AddressByte[2] AddressByte[3]

			 */
			Int32 modEnd = Offset.Function.InvalidPtrCheck + 5;

			if (scanEnd > modStart)
			{
				if (scanEnd < modEnd || scanStart < modEnd)
				{
					hc.PrintMessage($"Warden[{DateTime.Now}] Preventing scan at 0x{scanStart:X}, size {size}. Unhooking...");

					commandHandler.DestroyLuaEventFrameHandler_W();
					invalidPtrCheckPatch.Dispose();
					LocalHook.Release();
					return true;
				}
			}

			return false;
		}

		public void Dispose()
		{
			invalidPtrCheckPatch?.Dispose();
			virtualProtectPatch?.Dispose();
			wardenScanPatch?.Dispose();
			wardenPageCheckPatch?.Dispose();
			LocalHook.Release();
		}
	}
}
