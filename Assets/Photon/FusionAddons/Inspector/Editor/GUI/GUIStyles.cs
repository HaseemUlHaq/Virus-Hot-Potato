namespace Fusion.Addons.Inspector.Editor
{
	using UnityEditor;
	using UnityEngine;

	public static class GUIStyles
	{
		public static readonly GUIStyle Icon;
		public static readonly GUIStyle LeftLabel;
		public static readonly GUIStyle RightLabel;
		public static readonly GUIStyle LeftBoldLabel;
		public static readonly GUIStyle RightBoldLabel;
		public static readonly GUIStyle ObjectField;
		public static readonly GUIStyle SearchField;
		public static readonly GUIStyle ToolbarButton;
		public static readonly GUIStyle InactiveToggle;
		public static readonly GUIStyle ActiveToggle;

		static GUIStyles()
		{
			Icon = new GUIStyle(EditorStyles.label);
			Icon.alignment = TextAnchor.MiddleCenter;

			LeftLabel = new GUIStyle(EditorStyles.label);
			LeftLabel.padding = new RectOffset(4, 4, 0, 0);
			LeftLabel.alignment = TextAnchor.MiddleLeft;

			RightLabel = new GUIStyle(EditorStyles.label);
			RightLabel.padding = new RectOffset(4, 4, 0, 0);
			RightLabel.alignment = TextAnchor.MiddleRight;

			LeftBoldLabel = new GUIStyle(EditorStyles.boldLabel);
			LeftBoldLabel.padding = new RectOffset(4, 4, 0, 0);
			LeftBoldLabel.alignment = TextAnchor.MiddleLeft;

			RightBoldLabel = new GUIStyle(EditorStyles.boldLabel);
			RightBoldLabel.padding = new RectOffset(4, 4, 0, 0);
			RightBoldLabel.alignment = TextAnchor.MiddleRight;

			ObjectField = new GUIStyle(EditorStyles.label);
			ObjectField.padding = new RectOffset(3, 3, 1, 1);

			SearchField = new GUIStyle(EditorStyles.toolbarSearchField);

			ToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			ToolbarButton.alignment = TextAnchor.MiddleCenter;

			Color inactiveColor = Color.grey;
			InactiveToggle = new GUIStyle(EditorStyles.label);
			InactiveToggle.normal.textColor    = inactiveColor;
			InactiveToggle.onNormal.textColor  = inactiveColor;
			InactiveToggle.hover.textColor     = inactiveColor;
			InactiveToggle.onHover.textColor   = inactiveColor;
			InactiveToggle.active.textColor    = inactiveColor;
			InactiveToggle.onActive.textColor  = inactiveColor;
			InactiveToggle.focused.textColor   = inactiveColor;
			InactiveToggle.onFocused.textColor = inactiveColor;

			Color activeColor = EditorStyles.label.normal.textColor;
			ActiveToggle = new GUIStyle(EditorStyles.label);
			ActiveToggle.normal.textColor    = activeColor;
			ActiveToggle.onNormal.textColor  = activeColor;
			ActiveToggle.hover.textColor     = activeColor;
			ActiveToggle.onHover.textColor   = activeColor;
			ActiveToggle.active.textColor    = activeColor;
			ActiveToggle.onActive.textColor  = activeColor;
			ActiveToggle.focused.textColor   = activeColor;
			ActiveToggle.onFocused.textColor = activeColor;
		}
	}
}
