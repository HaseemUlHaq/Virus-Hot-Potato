namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;

	internal static class GUIColor
	{
		private static Color _guiColor;

		public static void Set(Color color)
		{
			_guiColor = GUI.color;
			GUI.color = color;
		}

		public static void Reset()
		{
			GUI.color = _guiColor;
		}
	}
}
