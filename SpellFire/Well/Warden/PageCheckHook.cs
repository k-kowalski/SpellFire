using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Util;

namespace SpellFire.Well.Warden
{
	public class PageCheckHook : IDisposable
	{
		#region PageCheckShellCode
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void PageCheck(IntPtr src, int offset, int size, IntPtr dest);

		private const int PageCheckShellCode_Length = 26;
		private const int PageCheckShellCode_CallHookOffset = 19; /* offset of "call HOOK" in our shellcode */
		private static byte[] PageCheckShellCode(Int32 hookJump, Int32 endJump)
		{
			byte[] hookJumpBytes = BitConverter.GetBytes(hookJump);
			byte[] endJumpBytes = BitConverter.GetBytes(endJump);
			return new byte[PageCheckShellCode_Length]
			{
				/* mov eax,[ebp+08] */			0x8B, 0x45, 0x08,
				/* pushfd		    */			0x9C,
				/* pushad		    */			0x60,
				/* mov ecx,esi      */			0x89, 0xF1,
				/* add ecx,0x1C     */			0x83, 0xC1, 0x1C,
				/* push ecx - PARAM: dest */	0x51,
				/* push ebx - PARAM: size */	0x53,
				/* push edi - PARAM: offset */  0x57,
				/* push eax - PARAM: src */		0x50, 
				/* call HOOK*/					0xE8, hookJumpBytes[0], hookJumpBytes[1], hookJumpBytes[2], hookJumpBytes[3],
				/* popad */						0x61,
				/* popfd */						0x9D,
				/* jmp END */					0xE9, endJumpBytes[0], endJumpBytes[1], endJumpBytes[2], endJumpBytes[3],
			};
		}
		#endregion

		private readonly Memory memory;

		private readonly PageCheck pageCheckHandlerInstance;

		private readonly IntPtr codeCaveAddress;
		
		private readonly IntPtr targetAddress;
		private readonly byte[] originalPageCheckBytes;

		public PageCheckHook(Memory memory, PageCheck pageCheckHandlerInstance, IntPtr targetAddress)
		{
			this.memory = memory;
			this.pageCheckHandlerInstance = pageCheckHandlerInstance;

			IntPtr hookAddress = Marshal.GetFunctionPointerForDelegate(pageCheckHandlerInstance);

			IntPtr endAddress = targetAddress + Offset.PageCheckReturnOffset;

			byte[] jmpShellcode = new byte[5];

			uint oldProtect = 0;
			SystemWin32.VirtualProtect(targetAddress, (UIntPtr)jmpShellcode.Length, SystemWin32.MemoryProtection.PAGE_EXECUTE_READWRITE, ref oldProtect);

			codeCaveAddress = Marshal.AllocHGlobal(PageCheckShellCode_Length);
			SystemWin32.VirtualProtect(codeCaveAddress, (UIntPtr)PageCheckShellCode_Length, SystemWin32.MemoryProtection.PAGE_EXECUTE_READWRITE, ref oldProtect);

			int hookJumpOffset = hookAddress.ToInt32() - codeCaveAddress.ToInt32() - PageCheckShellCode_CallHookOffset;
			int endJumpOffset = endAddress.ToInt32() - codeCaveAddress.ToInt32() - PageCheckShellCode_Length;

			byte[] pageCheckShellcodeAssembly = PageCheckShellCode(hookJumpOffset, endJumpOffset);

			memory.Write(codeCaveAddress, pageCheckShellcodeAssembly);


			int offsetCodeCave = codeCaveAddress.ToInt32() - targetAddress.ToInt32() - jmpShellcode.Length;

			byte[] offsetCodeCaveBytes = BitConverter.GetBytes(offsetCodeCave);
			jmpShellcode[0] = 0xE9; // JMP
			jmpShellcode[1] = offsetCodeCaveBytes[0];
			jmpShellcode[2] = offsetCodeCaveBytes[1];
			jmpShellcode[3] = offsetCodeCaveBytes[2];
			jmpShellcode[4] = offsetCodeCaveBytes[3];

			originalPageCheckBytes = memory.Read(targetAddress, jmpShellcode.Length);
			memory.Write(targetAddress, jmpShellcode);
		}

		public void Dispose()
		{
			memory.Write(targetAddress, originalPageCheckBytes);
			Marshal.FreeHGlobal(codeCaveAddress);
		}
	}
}
