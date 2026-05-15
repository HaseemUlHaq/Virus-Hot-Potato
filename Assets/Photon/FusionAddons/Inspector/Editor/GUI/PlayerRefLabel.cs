namespace Fusion.Addons.Inspector.Editor
{
	public sealed class PlayerRefLabel
	{
		public PlayerRef Value;

		private PlayerRef _labelValue;
		private string    _labelString = "None";

		public string GetLabel()
		{
			if (_labelValue != Value)
			{
				_labelValue = Value;

				if (_labelValue.IsRealPlayer == true)
				{
					_labelString = $"Player {_labelValue.PlayerId}";
				}
				else if (_labelValue.IsMasterClient == true)
				{
					_labelString = $"Master";
				}
				else if (_labelValue.IsNone == true)
				{
					_labelString = $"None";
				}
				else
				{
					_labelString = $"Invalid";
				}
			}

			return _labelString;
		}
	}
}
