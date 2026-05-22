namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;

	internal static class ListUtility
	{
		public static void InsertionSort<T>(List<T> list, Comparison<T> comparison)
		{
			for (int j = 1, count = list.Count; j < count; ++j)
			{
				T key = list[j];

				int i = j - 1;
				while (i >= 0 && comparison(list[i], key) > 0)
				{
					list[i + 1] = list[i];
					 --i;
				}

				list[i + 1] = key;
			}
		}
	}
}
