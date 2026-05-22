namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;

	public static class CachedUnits
	{
		private static readonly string[] _distanceLookupTable;
		private static readonly string[] _memoryBLookupTable;
		private static readonly string[] _memoryKBLookupTable;
		private static readonly string[] _memoryMBLookupTable;
		private static readonly string[] _memoryBPerSecondLookupTable;

		static CachedUnits()
		{
			_distanceLookupTable = new string[512];
			for (int i = 0, count = _distanceLookupTable.Length; i < count; ++i)
			{
				_distanceLookupTable[i] = $"{i} m";
			}

			_memoryBLookupTable = new string[1024];
			for (int i = 0, count = _memoryBLookupTable.Length; i < count; ++i)
			{
				_memoryBLookupTable[i] = $"{i} B";
			}

			_memoryKBLookupTable = new string[1024];
			for (int i = 0, count = _memoryKBLookupTable.Length; i < count; ++i)
			{
				_memoryKBLookupTable[i] = $"{i} kB";
			}

			_memoryMBLookupTable = new string[1024];
			for (int i = 0, count = _memoryMBLookupTable.Length; i < count; ++i)
			{
				_memoryMBLookupTable[i] = $"{i} MB";
			}

			_memoryBPerSecondLookupTable = new string[256];
			for (int i = 0, count = _memoryBPerSecondLookupTable.Length; i < count; ++i)
			{
				_memoryBPerSecondLookupTable[i] = $"{i} B/s";
			}
		}

		public static string GetDistanceString(int distance)
		{
			return distance < _distanceLookupTable.Length ? _distanceLookupTable[distance] : $"{distance} m";
		}

		public static string GetMemoryString(int bytes)
		{
			if (bytes < _memoryBLookupTable.Length)
				return _memoryBLookupTable[bytes];

			bytes = Mathf.RoundToInt(bytes / 1024.0f);
			if (bytes < _memoryKBLookupTable.Length)
				return _memoryKBLookupTable[bytes];

			bytes = Mathf.RoundToInt(bytes / 1024.0f);
			if (bytes < _memoryMBLookupTable.Length)
				return _memoryMBLookupTable[bytes];

			return $"{bytes} MB";
		}

		public static string GetMemoryPerSecondString(int bytesPerSecond)
		{
			return bytesPerSecond < _memoryBPerSecondLookupTable.Length ? _memoryBPerSecondLookupTable[bytesPerSecond] : $"{bytesPerSecond} B/s";
		}
	}
}
