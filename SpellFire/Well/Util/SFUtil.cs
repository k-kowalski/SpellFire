using System;
using System.Collections.Generic;
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
	}
}
