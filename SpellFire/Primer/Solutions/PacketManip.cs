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
		public PacketManip(ControlInterface ci, Memory memory) : base(ci, memory)
		{

			SpellPacket spellPacket = new SpellPacket
			{
				castCount = 0,
				spellID = 20589, // 20589 - escape artist
				castFlags = 0
			};


			ci.remoteControl.SendPacket(spellPacket);

			this.Active = false;
		}

		public override void Tick()
		{
		}

		public override void Finish()
		{
		}
	}
}
