namespace Fusion.Addons.Inspector.Editor
{
	public sealed class NetworkIdLabel
	{
		public NetworkId Value;

		private NetworkId _labelValue;
		private string    _labelString;

		public NetworkIdLabel() : this(default, "None")
		{
		}

		public NetworkIdLabel(NetworkId value) : this(value, value.Raw.ToString())
		{
		}

		private NetworkIdLabel(NetworkId value, string label)
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
				_labelString = Value.Raw.ToString();
			}

			return _labelString;
		}
	}
}
