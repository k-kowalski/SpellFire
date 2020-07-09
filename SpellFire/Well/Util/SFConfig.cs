using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SpellFire.Well.Util
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SFConfig
	{
		private const string DefaultConfigFile = "config.json";

		[JsonProperty("global")]
		public static GlobalConfig Global { get; set; }

		[JsonProperty("presets")]
		public Preset[] Presets { get; set; }

		public static SFConfig LoadConfig(string configFile = DefaultConfigFile)
		{
			var content = Encoding.UTF8.GetString(File.ReadAllBytes(configFile));
			return JsonConvert.DeserializeObject<SFConfig>(content);
		}
	}

	[Serializable]
	public class GlobalConfig
	{
		[JsonProperty("wowDir")]
		public string WowDir { get; set; }

		[JsonProperty("luaPlugFunctionName")]
		public string LuaPlugFunctionName { get; set; }

		[JsonProperty("dllName")]
		public string DllName { get; set; }

		[JsonProperty("mmapsDirPath")]
		public string MovementMapsDirectoryPath { get; set; }
	}

	public class Preset
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("clients")]
		public ClientLaunchSettings[] Clients { get; set; }

		public override string ToString()
		{
			return Id;
		}
	}

	public class ClientLaunchSettings
	{
		[JsonProperty("login")]
		public string Login { get; set; }

		[JsonProperty("password")]
		public string Password { get; set; }

		[JsonProperty("character")]
		public string Character { get; set; }

		[JsonProperty("gameConfig")]
		public string GameConfig { get; set; }

		[JsonProperty("solution")]
		public string Solution { get; set; }
	}
}
