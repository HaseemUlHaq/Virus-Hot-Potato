namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal static class TextureUtility
	{
		private static Dictionary<Color, Texture2D> _colorTextures = new Dictionary<Color, Texture2D>();

		public static Texture2D GetTexture(Color color)
		{
			if (_colorTextures.TryGetValue(color, out Texture2D texture) == false || texture == null)
			{
				texture = new Texture2D(1, 1);
				texture.SetPixel(0, 0, color);
				texture.Apply();

				_colorTextures[color] = texture;
			}

			return texture;
		}

		public static void DrawTexture(Rect rect, Color color)
		{
			Texture2D texture = GetTexture(color);
			EditorGUI.DrawTextureTransparent(rect, texture);
		}
	}
}
