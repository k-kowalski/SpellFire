using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellFire.Well.Controller;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	public class Morpher : Solution
	{
		public Morpher(ControlInterface ci, Memory memory) : base(ci, memory)
		{
			GetObjectMgrAndPlayer();
			Morph();
		}

		private void Morph()
		{
			IntPtr fields = memory.ReadPointer86(player.GetAddress() + Offset.Info);

			var targetMorphItemID = 19334;
			var targetMorphEnchantID = 0;
			var slot = 16;

			if (slot > 19 || slot < 0)
			{
				Console.WriteLine("Item slot id must be number 1-19.");
				return;
			}

			var offsetItem = (Offset.PlayerItem1ID + ((slot - 1) * 2)) * 4;
			var offsetEnchant = offsetItem + sizeof(Int32); /* enchant immediately follows item */

			memory.Write(fields + offsetItem, BitConverter.GetBytes(targetMorphItemID));
			memory.Write(fields + offsetEnchant, BitConverter.GetBytes(targetMorphEnchantID));
			ci.remoteControl.CGUnit_C__UpdateDisplayInfo(player.GetAddress(), true);
		}

		public override void Tick()
		{
			
		}

		public override void Dispose()
		{
			
		}
	}
}
