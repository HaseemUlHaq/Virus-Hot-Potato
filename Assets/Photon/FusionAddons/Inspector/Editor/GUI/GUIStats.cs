namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using UnityEngine;

	internal class GUIStat
	{
		public readonly GUIStyle   Style;
		public readonly GUIContent Content;

		public float Width;

		public GUIStat(GUIStyle style, string text, string tooltip, float width = 0.0f)
		{
			Style   = style;
			Content = new GUIContent(text, tooltip);
			Width   = width;

			if (width <= 0.0f && style != null)
			{
				Width = style.CalcSize(Content).x;
			}
		}
	}

	internal sealed class GUIIconStat : GUIStat
	{
		public readonly GUIContent IconContent;

		public GUIIconStat(GUIStyle style, string text, string iconText, string tooltip, float width = 0.0f) : base(style, text, tooltip, width)
		{
			IconContent = new GUIContent(iconText, tooltip);
		}
	}

	internal sealed class GUIIntStat : GUIStat
	{
		private string _format;
		private int    _value;

		public GUIIntStat(GUIStyle style, string format, int value, string tooltip, float width = 0.0f) : base(style, string.Format(format, value), tooltip, width)
		{
			_format = format;
			_value  = value;
		}

		public GUIContent GetContent(int value)
		{
			if (_value != value)
			{
				_value = value;

				Content.text = string.Format(_format, value);
			}

			return Content;
		}
	}

	internal sealed class GUITimeStat : GUIStat
	{
		private string _format;
		private int    _seconds;
		private int    _minutes;
		private int    _hours;

		public GUITimeStat(GUIStyle style, string format, TimeSpan value, string tooltip, float width = 0.0f) : base(style, string.Format(format, $"{value:hh\\:mm\\:ss}"), tooltip, width)
		{
			_format  = format;
			_seconds = value.Seconds;
			_minutes = value.Minutes;
			_hours   = value.Hours;
		}

		public GUIContent GetContent(TimeSpan value)
		{
			int seconds = value.Seconds;
			int minutes = value.Minutes;
			int hours   = value.Hours;

			if (_seconds != seconds || _minutes != minutes || _hours != hours)
			{
				_seconds = seconds;
				_minutes = minutes;
				_hours   = hours;

				Content.text = string.Format(_format, $"{value:hh\\:mm\\:ss}");
			}

			return Content;
		}
	}

	internal sealed class GUITrafficStat : GUIStat
	{
		private string _format;
		private int    _value;

		public GUITrafficStat(GUIStyle style, string format, int value, string tooltip, float width = 0.0f) : base(style, GetFormattedValue(format, Mathf.CeilToInt(value / 1024.0f)), tooltip, width)
		{
			_format = format;
			_value  = value;
		}

		public GUIContent GetContent(float value)
		{
			int valueKB = Mathf.CeilToInt(value / 1024.0f);

			if (_value != valueKB)
			{
				_value = valueKB;

				Content.text = GetFormattedValue(_format, valueKB);
			}

			return Content;
		}

		private static string GetFormattedValue(string format, int valueKB)
		{
			return valueKB < 1000 ? string.Format(format, $"{valueKB} kB") : string.Format(format, $"{(0.001f * valueKB):F2} MB");
		}
	}

	internal sealed class GUISortStat : GUIStat
	{
		private string _text;
		private int    _sortMode;
		private int    _ascendingSortMode;
		private int    _descendingSortMode;

		public GUISortStat(GUIStyle style, string text, string tooltip, int ascendingSortMode, int descendingSortMode, float width = 0.0f) : base(style, text, tooltip, width)
		{
			_text               = text;
			_ascendingSortMode  = ascendingSortMode;
			_descendingSortMode = descendingSortMode;
		}

		public GUIContent GetContent(int sortMode)
		{
			if (sortMode != _ascendingSortMode && sortMode != _descendingSortMode)
			{
				sortMode = default;
			}

			if (_sortMode == sortMode)
				return Content;

			_sortMode = sortMode;

			if (sortMode == _ascendingSortMode && sortMode != default)
			{
				Content.text = $"{GUISymbols.ARROW_UP} {_text}";
			}
			else if (sortMode == _descendingSortMode && sortMode != default)
			{
				Content.text = $"{GUISymbols.ARROW_DOWN} {_text}";
			}
			else
			{
				Content.text = _text;
			}

			return Content;
		}
	}

	internal sealed class GUINameStat : GUIStat
	{
		private string _text;
		private int    _total;
		private int    _filtered;
		private int    _selected;
		private int    _sortMode;
		private int    _ascendingSortMode;
		private int    _descendingSortMode;

		public GUINameStat(GUIStyle style, string text, string tooltip, int ascendingSortMode, int descendingSortMode, float width = 0.0f) : base(style, text, tooltip, width)
		{
			_text               = text;
			_ascendingSortMode  = ascendingSortMode;
			_descendingSortMode = descendingSortMode;
		}

		public bool IsSorted(int sortMode) => sortMode == _ascendingSortMode || sortMode == _descendingSortMode;

		public GUIContent GetContent(int total, int filtered, int selected, int sortMode)
		{
			if (sortMode != _ascendingSortMode && sortMode != _descendingSortMode)
			{
				sortMode = default;
			}

			if (_total == total && _filtered == filtered && _selected == selected && _sortMode == sortMode)
				return Content;

			_total    = total;
			_filtered = filtered;
			_selected = selected;
			_sortMode = sortMode;

			string text;

			if (sortMode == _ascendingSortMode && sortMode != default)
			{
				text = total == filtered ? $"{GUISymbols.ARROW_UP} {_text} ({total})" : $"{GUISymbols.ARROW_UP} {_text} ({filtered} / {total})";
			}
			else if (sortMode == _descendingSortMode && sortMode != default)
			{
				text = total == filtered ? $"{GUISymbols.ARROW_DOWN} {_text} ({total})" : $"{GUISymbols.ARROW_DOWN} {_text} ({filtered} / {total})";
			}
			else
			{
				text = total == filtered ? $"{_text} ({total})" : $"{_text} ({filtered} / {total})";
			}

			if (selected > 1)
			{
				text += $" - {selected} Selected";
			}

			Content.text = text;

			return Content;
		}
	}
}
