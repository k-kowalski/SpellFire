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

		private ControlInterface ci;

		public Fishing(Client client) : base(client)
		{
			// start fishing initially
			client.CastSpell("Fishing");

			ci = client.ControlInterface;

			this.Active = true;
		}

		public override void Tick()
		{
			Thread.Sleep(1000);

			if (!me.GetObjectMgrAndPlayer())
			{
				return;
			}

			foreach (GameObject gameObject in me.ObjectManager)
			{
				Int64 gameObjectGUID = gameObject.GUID;
				if (gameObject.Type == GameObjectType.GameWorldObject && ( ! lastBobberGUIDs.Contains(gameObjectGUID)))
				{
					if (gameObject.WorldObjectName.Contains("Fishing Bobber"))
					{
						byte state = me.Memory.Read(gameObject.GetAddress() + 0xBC, 1)[0];
						if (state == 1)
						{
							RandomSleep();
							me.Memory.Write(IntPtr.Zero + Offset.MouseoverGUID, BitConverter.GetBytes(gameObjectGUID));
							ci.remoteControl.FrameScript__Execute("InteractUnit('mouseover')", 0, 0);
							RandomSleep();
							me.CastSpell("Fishing");

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

		public override void Dispose() {}

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
