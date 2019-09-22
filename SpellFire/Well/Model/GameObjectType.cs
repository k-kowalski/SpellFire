using System;

namespace SpellFire.Well.Model
{
	public enum GameObjectType : Int32
	{
		None,
		Item,
		Container,
		Unit,
		Player,
		GameWorldObject,
		DynamicObject,
		Corpse,
	}
}
