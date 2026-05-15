namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;

	internal sealed unsafe class NetworkStateTracker
	{
		private readonly byte[] _stateValues;

		private static readonly int[] _bitChangesLookupTable;

		static NetworkStateTracker()
		{
			_bitChangesLookupTable = new int[256];
			for (int i = 0; i < 256; ++i)
			{
				int count = 0;
				int value = i;

				while (value != 0)
				{
					count += value & 1;
					value >>= 1;
				}

				_bitChangesLookupTable[i] = count;
			}
		}

		internal NetworkStateTracker(NetworkBehaviour networkBehaviour)
		{
			if (Application.isPlaying == false)
				return;
			if (networkBehaviour.StateBufferIsValid == false)
				return;

			int stateSize = networkBehaviour.WordInfo.count * 4;
			if (stateSize <= 0)
				return;

			_stateValues = new byte[stateSize];

			fixed (byte* lastStatePtr = _stateValues)
			{
				fixed (byte* currentStatePtr = &networkBehaviour.ReinterpretState<byte>())
				{
					for (int i = 0, count = _stateValues.Length; i < count; ++i)
					{
						*(lastStatePtr + i) = *(currentStatePtr + i);
					}
				}
			}
		}

		internal int ExchangeState(NetworkBehaviour networkBehaviour)
		{
			if (_stateValues == default)
				return default;

			int bitsChanged = default;

			fixed (byte* lastStateFixedPtr = _stateValues)
			{
				fixed (byte* currentStateFixedPtr = &networkBehaviour.ReinterpretState<byte>())
				{
					byte* lastStatePtr       = lastStateFixedPtr;
					byte* currentStatePtr    = currentStateFixedPtr;
					byte* currentEndStatePtr = currentStateFixedPtr + _stateValues.Length;

					while (currentStatePtr != currentEndStatePtr)
					{
					#if FUSION_INSPECTOR_TRACK_BITS
						bitsChanged += _bitChangesLookupTable[(*lastStatePtr) ^ (*currentStatePtr)];
					#else
						if ((*lastStatePtr) != (*currentStatePtr))
						{
							bitsChanged += 8;
						}
					#endif

						*lastStatePtr = *currentStatePtr;

						++lastStatePtr;
						++currentStatePtr;
					}
				}
			}

			return bitsChanged;
		}
	}
}
