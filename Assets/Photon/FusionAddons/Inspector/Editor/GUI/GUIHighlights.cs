namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;

	public static class GUIHighlights
	{
		internal static readonly List<GUIHighlight> All = new List<GUIHighlight>();

		public static void Add(Rect rect, Color color)
		{
			All.Add(new GUIHighlight(rect, color));
		}

		public static void DrawAll()
		{
			foreach (GUIHighlight highlight in All)
			{
				TextureUtility.DrawTexture(highlight.Rect, highlight.Color);
			}

			All.Clear();
		}

		public static void Clear()
		{
			All.Clear();
		}
	}

	internal readonly struct GUIHighlight
	{
		public readonly Rect  Rect;
		public readonly Color Color;

		public GUIHighlight(Rect rect, Color color)
		{
			Rect  = rect;
			Color = color;
		}
	}
}
