using System;
using System.Runtime.InteropServices;

namespace SpellFire.Primer
{
	public static class SystemWin32
	{
		public static Int32 PROCESS_ALL_ACCESS = 0x1F0FFF;

		[DllImport("user32.dll")]
		public static extern Int16 VkKeyScan(char ch);

		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(Int32 dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 dwSize, ref Int32 lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 dwSize, ref Int32 lpNumberOfBytesWritten);

		[DllImport("user32.dll")]
		public static extern Int32 SendMessage(IntPtr hWnd, Int32 Msg, UInt32 wParam, UInt32 lParam);
	}
}
