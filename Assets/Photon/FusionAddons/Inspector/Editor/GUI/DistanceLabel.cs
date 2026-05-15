namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;

	public sealed class DistanceLabel
	{
		public float Value;

		private int    _labelValue  = -1;
		private string _labelString = "---";

		public string GetLabel()
		{
			int ceilValue = Mathf.CeilToInt(Value);

			if (_labelValue != ceilValue)
			{
				_labelValue  = ceilValue;
				_labelString = CachedUnits.GetDistanceString(ceilValue);
			}

			return _labelString;
		}

		public void Clear()
		{
			_labelValue  = -1;
			_labelString = "---";
		}
	}
}
