using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.Net;

namespace SpellFire.Primer.Solutions
{
	class AutoLogin : Solution
	{
		private const String CredentialsFile = "cred.txt";

		public AutoLogin(ControlInterface ci, Memory memory) : base(ci, memory)
		{
			string[] credentials = File.ReadAllLines(CredentialsFile);
			if (credentials.Length == 2)
			{
				string username = credentials[0];
				string password = credentials[1];
				ci.remoteControl.FrameScript__Execute($"DefaultServerLogin('{username}', '{password}')", 0, 0);
			}
		}

		public override void Tick()
		{
		}

		public override void Finish()
		{
		}
	}
}
