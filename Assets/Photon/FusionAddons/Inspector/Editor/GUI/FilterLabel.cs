namespace Fusion.Addons.Inspector.Editor
{
	public sealed class FilterLabel
	{
		private int    _totalCount    = -1;
		private int    _filteredCount = -1;
		private string _labelString   = "---";

		public string GetLabel(string text, int totalCount, int filteredCount)
		{
			if (_totalCount != totalCount || _filteredCount != filteredCount)
			{
				_totalCount    = totalCount;
				_filteredCount = filteredCount;

				if (totalCount > 0)
				{
					if (filteredCount == totalCount)
					{
						_labelString = $"{text} ({filteredCount})";
					}
					else
					{
						_labelString = $"{text} ({filteredCount} / {totalCount})";
					}
				}
				else
				{
					_labelString = text;
				}
			}

			return _labelString;
		}
	}
}
