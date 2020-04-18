using System;
using System.Collections.Generic;
using System.Threading;
using SpellFire.Well.Controller;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Solutions
{
	/// <summary>
	/// Fishbot
	/// </summary>
	public class Fishing : Solution
	{
		private LinkedList<Int64> lastBobberGUIDs = new LinkedList<long>();

		public Fishing(ControlInterface ci, Memory memory) : base(ci, memory)
		{
			// start fishing initially
			CastSpell("Fishing");

			this.Active = true;
		}

		public override void Tick()
		{
			Thread.Sleep(1000);

			if (!GetObjectMgrAndPlayer())
			{
				return;
			}

			foreach (GameObject gameObject in objectManager)
			{
				Int64 gameObjectGUID = gameObject.GUID;
				if (gameObject.Type == GameObjectType.GameWorldObject && ( ! lastBobberGUIDs.Contains(gameObjectGUID)))
				{
					if (gameObject.WorldObjectName.Contains("Fishing Bobber"))
					{
						byte state = memory.Read(gameObject.GetAddress() + 0xBC, 1)[0];
						if (state == 1)
						{
							RandomSleep();
							memory.Write(IntPtr.Zero + Offset.MouseoverGUID, BitConverter.GetBytes(gameObjectGUID));
							ci.remoteControl.FrameScript__Execute("InteractUnit('mouseover')", 0, 0);
							RandomSleep();
							CastSpell("Fishing");

							if (lastBobberGUIDs.Count == 5)
							{
								lastBobberGUIDs.RemoveFirst();
							}
							lastBobberGUIDs.AddLast(gameObjectGUID);
						}
					}
				}
			}
		}

		public override void Dispose()
		{
			/* no finish logic */
		}

		/*
		 * simulate humane behaviour
		 */
		private void RandomSleep()
		{
			Int32 fluctuation = SFUtil.RandomGenerator.Next(500) * (SFUtil.RandomGenerator.Next(0, 2) == 0 ? -1 : 1);
			Thread.Sleep(1000 + fluctuation);
		}
	}
}
