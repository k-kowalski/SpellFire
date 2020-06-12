using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Util
{
	public static class SFUtil
	{
		public static readonly Random RandomGenerator = new Random();

		/// <summary>
		/// Generate random ASCII string that consists only of letters
		/// </summary>
		public static string GetRandomAsciiString(Int32 length)
		{
			char[] result = new char[length];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = RandomGenerator.Next(0, 2) == 0
					? (char) RandomGenerator.Next(65, 91)
					: (char) RandomGenerator.Next(97, 123);
			}

			return new string(result);
		}

		public static string RemoveFromEnd(this string str, string suffix)
		{
			if (str.EndsWith(suffix))
			{
				return str.Substring(0, str.Length - suffix.Length);
			}
			else
			{
				return str;
			}
		}

		public static IntPtr GetIntPtr(this UIntPtr uintptr)
		{
			return unchecked((IntPtr) (long) (ulong) uintptr);
		}

		public static UIntPtr GetUIntPtr(this IntPtr intptr)
		{
			return unchecked((UIntPtr) (ulong) (long) intptr);
		}

		public static string ReadMemoryAsHex(Memory memory, IntPtr address, Int32 len)
		{
			var ba = memory.Read(address, len);
			return BitConverter.ToString(ba).Replace("-", "");
		}

		public static ProcessModule GetModule(this Process process, string name)
		{
			foreach (ProcessModule module in process.Modules)
			{
				if (module.ModuleName == name)
				{
					return module;
				}
			}

			throw new Exception("Module not found.");
		}

		public static void DumpMemory(Memory memory, IntPtr address, UInt32 size, string fileName)
		{
			File.WriteAllBytes(fileName, memory.Read(address, (int)size));
		}
	}
}
