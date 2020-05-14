using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellFire.Well.Util
{
	public class Config
	{
		public const string DefaultConfigFile = "config.txt";
		public const string CommentMark = "#";

		private readonly Dictionary<string, string> Settings = new Dictionary<string, string>();

		public Config(string configFile = DefaultConfigFile)
		{
			string[] configAllLines = File.ReadAllLines(configFile);
			foreach (var line in configAllLines)
			{
				if (String.IsNullOrEmpty(line) || line.StartsWith(Config.CommentMark))
				{
					continue;
				}

				var keyValPair = line.Split('=');
				Settings.Add(keyValPair[0], keyValPair[1]);
			}
		}

		public string this[string key] => Settings.TryGetValue(key, out string result) ? result : null;

		public string[] Keys() => Settings.Keys.ToArray();
	}
}
