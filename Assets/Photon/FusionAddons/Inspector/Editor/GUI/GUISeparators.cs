namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;

	public static class GUISeparators
	{
		internal static readonly List<GUISeparator> X = new List<GUISeparator>();
		internal static readonly List<GUISeparator> Y = new List<GUISeparator>();

		public static void AddX(float position)
		{
			X.Add(new GUISeparator(position, FusionInspector.SeparatorSize));
		}

		public static void AddY(float position)
		{
			Y.Add(new GUISeparator(position, FusionInspector.SeparatorSize));
		}

		public static void AddX(float position, float thickness)
		{
			X.Add(new GUISeparator(position, thickness));
		}

		public static void AddY(float position, float thickness)
		{
			Y.Add(new GUISeparator(position, thickness));
		}

		public static void DrawAll(Rect region, float headerPosition, float headerHeight, float contentPosition, float contentHeight)
		{
			foreach (GUISeparator separator in X)
			{
				float xOffset = region.xMin - separator.Thickness * 0.5f;
				float yOffset = region.yMin;

				if (headerHeight > 0.0f)
				{
					TextureUtility.DrawTexture(new Rect(separator.Position + xOffset, headerPosition + yOffset, separator.Thickness, headerHeight), FusionInspector.SeparatorColor);
				}

				if (contentHeight > 0.0f)
				{
					TextureUtility.DrawTexture(new Rect(separator.Position + xOffset, contentPosition + yOffset, separator.Thickness, contentHeight), FusionInspector.SeparatorColor);
				}
			}

			foreach (GUISeparator separator in Y)
			{
				float xOffset = region.xMin;
				float yOffset = region.yMin - separator.Thickness * 0.5f;

				TextureUtility.DrawTexture(new Rect(xOffset, separator.Position + yOffset, region.width, separator.Thickness), FusionInspector.SeparatorColor);
			}

			X.Clear();
			Y.Clear();
		}

		public static void Clear()
		{
			X.Clear();
			Y.Clear();
		}
	}

	internal readonly struct GUISeparator
	{
		public readonly float Position;
		public readonly float Thickness;

		public GUISeparator(float position, float thickness)
		{
			Position  = position;
			Thickness = thickness;
		}
	}
}
