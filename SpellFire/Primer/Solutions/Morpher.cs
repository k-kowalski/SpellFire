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
		private readonly GameObject player;
		private readonly GameObjectManager objectManager;

		public Morpher(ControlInterface ci, Memory memory) : base(ci, memory)
		{
			IntPtr clientConnection = memory.ReadPointer86(IntPtr.Zero + Offset.ClientConnection);
			IntPtr objectManagerAddress = memory.ReadPointer86(clientConnection + Offset.GameObjectManager);

			player = new GameObject(memory, ci.remoteControl.ClntObjMgrGetActivePlayerObj());
			objectManager = new GameObjectManager(memory, objectManagerAddress);

			Morph();
		}

		private void Morph()
		{
			IntPtr fields = memory.ReadPointer86(player.GetAddress() + Offset.Info);


			var targetMorphItemID = 31980;
			var slot = 1;

			if (slot > 19 || slot < 0)
			{
				Console.WriteLine("Item slot id must be number 1-19.");
				return;
			}

			var offset = (Offset.PlayerItem1ID + ((slot - 1) * 2)) * 4;

			memory.Write(fields + offset, BitConverter.GetBytes(targetMorphItemID));
			ci.remoteControl.CGUnit_C__UpdateDisplayInfo(player.GetAddress(), true);
		}

		public override void Tick()
		{
			
		}

		public override void Finish()
		{
			
		}
	}
}
