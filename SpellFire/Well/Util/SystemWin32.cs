using System;
using System.Runtime.InteropServices;

namespace SpellFire.Well.Util
{
	public static class SystemWin32
	{
		public const UInt32 WM_KEYDOWN = 0x100;
		public const UInt32 WM_KEYUP = 0x101;

		public const Int32 GWL_WNDPROC = -0x4;

		public const Int32 PROCESS_ALL_ACCESS = 0x1F0FFF;

		public enum MemoryProtection : Int32
		{
			PAGE_EXECUTE_READ = 0x20,
			PAGE_EXECUTE_READWRITE = 0x40,
			PAGE_NOACCESS = 0x1
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		[DllImport("kernel32.dll")]
		public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, MemoryProtection flNewProtect, ref uint lpflOldProtect);

		[DllImport("user32.dll")]
		public static extern Int16 VkKeyScan(char ch);

		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(Int32 dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 dwSize, ref Int32 lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 dwSize, ref Int32 lpNumberOfBytesWritten);

		[DllImport("kernel32.dll")]
		public static extern Int32 AllocConsole();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern Int32 GetWindowLong(IntPtr hWnd, Int32 nIndex);

		[DllImport("user32.dll", SetLastError=true)]
		public static extern Int32 SetWindowLong(IntPtr hWnd, Int32 nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern Int32 PostMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

		public delegate IntPtr WndProc(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);
		public delegate bool VirtualProtectDelegate(IntPtr lpAddress, UIntPtr dwSize, MemoryProtection flNewProtect, ref uint lpflOldProtect);
		public delegate int VirtualQueryDelegate(IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

		[DllImport("kernel32.dll")]
		public static extern int VirtualQuery(
			IntPtr lpAddress,
			ref MEMORY_BASIC_INFORMATION lpBuffer,
			uint dwLength
		);

		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORY_BASIC_INFORMATION
		{
			public IntPtr BaseAddress;
			public IntPtr AllocationBase;
			public uint AllocationProtect;
			public IntPtr RegionSize;
			public MemoryState State;
			public MemoryProtection Protect;
			public uint Type;
		}

		public enum MemoryState : Int32
		{
			MEM_FREE = 0x10000
		}
	}
}
