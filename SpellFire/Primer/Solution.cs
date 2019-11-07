namespace SpellFire.Primer
{
	public abstract class Solution
	{
		public bool Active { get; set; } = true;

		public abstract void Tick();
		public virtual void Stop()
		{
			this.Active = false;
		}
		public abstract void Finish();
	}
}