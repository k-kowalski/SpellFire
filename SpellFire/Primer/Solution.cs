namespace SpellFire.Primer
{
	public abstract class Solution
	{
		public bool Active { get; set; } = true;

		public abstract void Tick();
		public abstract void Stop();
		public abstract void Finish();
	}
}