namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;

	internal sealed unsafe class MemoryTracker
	{
		public int BytesPerSecond { get; private set; }

		private Queue<MemoryChange> _changes;
		private int                 _accumulatedChanges;

		public void AccumulateChange(int bits)
		{
			_accumulatedChanges += bits;
		}

		public int ProcessAccumulatedChanges()
		{
			int accumulatedChanges = _accumulatedChanges;

			_accumulatedChanges = default;

			float currentTime    = Time.unscaledTime;
			int   bytesPerSecond = default;

			if (_changes == null)
			{
				_changes = new Queue<MemoryChange>(32);
			}

			_changes.Enqueue(new MemoryChange() { Time = currentTime, Bits = accumulatedChanges } );

			if (_changes.Count > 31)
			{
				float baseTime         = _changes.Dequeue().Time;
				int   totalBitsChanged = default;

				foreach (MemoryChange change in _changes)
				{
					totalBitsChanged += change.Bits;
				}

				float deltaTime = currentTime - baseTime;
				if (deltaTime > 0.001f)
				{
					float totalBytesChanged = 0.125f * totalBitsChanged;
					bytesPerSecond = Mathf.CeilToInt(totalBytesChanged / deltaTime);
				}
			}

			BytesPerSecond = bytesPerSecond;

			return accumulatedChanges;
		}

		public void Clear()
		{
			if (_changes != null)
			{
				_changes.Clear();
			}

			_accumulatedChanges = default;
		}
	}
}
