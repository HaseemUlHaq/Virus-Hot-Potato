namespace Fusion.Addons.Inspector.Editor
{
	public sealed class IntLabel
	{
		public int Value;

		private int    _labelValue;
		private string _labelString;

		public IntLabel() : this(0)
		{
		}

		public IntLabel(int value) : this(value, value.ToString())
		{
		}

		public IntLabel(int value, string label)
		{
			Value = value;

			_labelValue  = value;
			_labelString = label;
		}

		public string GetLabel()
		{
			if (_labelValue != Value)
			{
				_labelValue  = Value;
				_labelString = Value.ToString();
			}

			return _labelString;
		}
	}
}
