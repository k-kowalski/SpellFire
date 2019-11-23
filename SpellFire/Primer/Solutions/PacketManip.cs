using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.Net;

namespace SpellFire.Primer.Solutions
{
	class PacketManip : Solution
	{
		private readonly ControlInterface ci;

		public PacketManip(ControlInterface ci, Memory memory)
		{
			this.ci = ci;


			SpellPacket spellPacket = new SpellPacket
			{
				castCount = 0,
				spellID = 20589, // 20589 - escape artist
				castFlags = 0
			};


			ci.remoteControl.SendPacket(spellPacket);
		}

		public override void Tick()
		{
		}

		public override void Finish()
		{
		}
	}
}
