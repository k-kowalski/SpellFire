using System;
using System.Collections;
using System.Collections.Generic;
using SpellFire.Well.Util;

namespace SpellFire.Primer
{
	public class GameObjectManager : MemoryMappedObject, IEnumerable<GameObject>
	{
		public GameObjectManager(Memory memory, IntPtr address) : base(memory, address) { }

		public IEnumerator<GameObject> GetEnumerator()
		{
			IntPtr currentGameObject = memory.ReadPointer86(this.address + Offset.FirstGameObject);
			while (currentGameObject != IntPtr.Zero)
			{
				yield return new GameObject(memory, currentGameObject);
				currentGameObject = memory.ReadPointer86(currentGameObject + Offset.NextGameObject);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
