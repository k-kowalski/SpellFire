using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
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

		public static void PlayNotificationSound()
		{
			Console.Beep(600, 500);
			Console.Beep(500, 500);
		}

		public static Ty GetRemoteClientObject<Ty>(string remoteChannelName) where Ty : MarshalByRefObject
		{
			return Activator.GetObject(typeof(Ty),
				"ipc://" + remoteChannelName + "/" + remoteChannelName) as Ty;
		}

		public static void RegisterRemoteServer(string remoteChannelName, string remotePortName = null)
		{
			BinaryServerFormatterSinkProvider binaryProv = new BinaryServerFormatterSinkProvider
			{
				TypeFilterLevel = TypeFilterLevel.Full
			};
			IDictionary properties = new Hashtable();
			properties["name"] = remoteChannelName;
			properties["portName"] = remotePortName ?? remoteChannelName;

			ChannelServices.RegisterChannel(new IpcServerChannel(properties, binaryProv), false);
		}

		public static void DumpString(string str, string fileName)
		{
			File.WriteAllText(str, fileName);
		}
	}
}
