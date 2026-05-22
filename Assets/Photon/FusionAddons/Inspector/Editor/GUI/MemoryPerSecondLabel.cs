namespace Fusion.Addons.Inspector.Editor
{
	public sealed class MemoryPerSecondLabel
	{
		public int Value;

		private int    _labelBytes  = 0;
		private string _labelString = CachedUnits.GetMemoryPerSecondString(0);

		public string GetLabel()
		{
			if (_labelBytes != Value)
			{
				_labelBytes  = Value;
				_labelString = CachedUnits.GetMemoryPerSecondString(Value);
			}

			return _labelString;
		}

		public void Clear()
		{
			Value = 0;
		}
	}
}
