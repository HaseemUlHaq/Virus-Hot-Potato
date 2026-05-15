namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;

	public sealed class MemoryLabel
	{
		public int Value;

		private bool   _convert;
		private int    _labelValue;
		private string _labelString;

		public MemoryLabel(bool convertToBytes) : this(convertToBytes, 0)
		{
		}

		public MemoryLabel(bool convertToBytes, int value) : this(convertToBytes, value, CachedUnits.GetMemoryString(convertToBytes == true ? Mathf.CeilToInt(0.125f * value) : value))
		{
		}

		public MemoryLabel(bool convertToBytes, int value, string label)
		{
			Value = value;

			_convert     = convertToBytes;
			_labelValue  = value;
			_labelString = label;
		}

		public string GetLabel()
		{
			if (_labelValue != Value)
			{
				_labelValue  = Value;
				_labelString = CachedUnits.GetMemoryString(_convert == true ? Mathf.CeilToInt(0.125f * Value) : Value);
			}

			return _labelString;
		}

		public void Clear()
		{
			Value = 0;
		}
	}
}
