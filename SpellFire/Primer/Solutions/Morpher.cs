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
		/* fill this function to tell Morpher what to morph into what */
		private static RequestMorphItem GetMorphRequest(string inventorySlot)
		{
			return inventorySlot switch
			{
				"main_hand" => new RequestMorphItem
				{
					itemId = 23499,
					enchantId = 0,
				},
				"head" => new RequestMorphItem
				{
					itemId = 34652,
					enchantId = 0,
				},
				"shoulder" => new RequestMorphItem
				{
					itemId = 0,
					enchantId = 0,
				},
				"legs" => new RequestMorphItem
				{
					itemId = 34656,
					enchantId = 0,
				},
				"hands" => new RequestMorphItem
				{
					itemId = 34649,
					enchantId = 0,
				},
				_ => null,
			};
		}

		/* index is slot id */
		private string[] InventorySlots =
		{
			"ammo",
			"head",
			"neck",
			"shoulder",
			"shirt",
			"chest",
			"waist",
			"legs",
			"feet",
			"wrist",
			"hands",
			"finger1",
			"finger2",
			"trinket1",
			"trinket2",
			"back",
			"main_hand",
			"off_hand",
			"ranged",
			"tabard",
		};

		class RequestMorphItem
		{
			public Int32 itemId;
			public Int32 enchantId;
		}

		public Morpher(Client client) : base(client)
		{
			client.GetObjectMgrAndPlayer();
			Morph();
		}

		private void Morph()
		{
			IntPtr fields = me.Memory.ReadPointer32(me.Player.GetAddress() + Offset.Info);

			for (int slot = 0; slot < InventorySlots.Length; slot++)
			{
				RequestMorphItem request = GetMorphRequest(InventorySlots[slot]);
				if (request == null)
				{
					continue;
				}

				int offsetItem = (Offset.PlayerItem1ID + ((slot - 1) * 2)) * 4;
				int offsetEnchant = offsetItem + sizeof(Int32); /* enchant immediately follows item */

				me.Memory.Write(fields + offsetItem, BitConverter.GetBytes(request.itemId));
				me.Memory.Write(fields + offsetEnchant, BitConverter.GetBytes(request.enchantId));
			}

			me.ControlInterface.remoteControl.CGUnit_C__UpdateDisplayInfo(me.Player.GetAddress(), true);
		}

		public override void Tick()
		{
			
		}

		public override void Dispose()
		{
			
		}
	}
}
