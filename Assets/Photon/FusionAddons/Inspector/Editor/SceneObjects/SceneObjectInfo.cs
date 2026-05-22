namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	internal sealed class SceneObjectInfo
	{
		public readonly Transform      Transform;
		public readonly GameObject     GameObject;
		public readonly NetworkObject  NetworkObject;
		public readonly NetworkIdLabel NetworkId;

		public string               Name;
		public bool                 IsValid;
		public bool                 IsActive;
		public bool                 IsSelected;
		public ComponentInfo[]      Components;
		public IntLabel             ComponentCount;
		public int                  InterestMode;
		public bool                 IsInSimulation;
		public bool                 HasStateAuthority;
		public bool                 HasInputAuthority;
		public PlayerRefLabel       StateAuthority        = new PlayerRefLabel();
		public int                  StateAuthoritySortKey = int.MaxValue;
		public PlayerRefLabel       InputAuthority        = new PlayerRefLabel();
		public int                  InputAuthoritySortKey = int.MaxValue;
		public MemoryLabel          TotalStateChanges     = new MemoryLabel(true);
		public MemoryPerSecondLabel AverageStateChanges   = new MemoryPerSecondLabel();
		public MemoryLabel          StateSize             = new MemoryLabel(false, default, "---");
		public DistanceLabel        Distance              = new DistanceLabel();

		public bool IsMasterClientObject            => (NetworkObject.Flags & NetworkObjectFlags.MasterClientObject)              == NetworkObjectFlags.MasterClientObject;
		public bool AllowStateAuthorityOverride     => (NetworkObject.Flags & NetworkObjectFlags.AllowStateAuthorityOverride)     == NetworkObjectFlags.AllowStateAuthorityOverride;
		public bool DestroyWhenStateAuthorityLeaves => (NetworkObject.Flags & NetworkObjectFlags.DestroyWhenStateAuthorityLeaves) == NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;

		private NetworkBehaviour[]    _behaviours;
		private NetworkStateTracker[] _stateTrackers;
		private MemoryTracker         _stateTracker = new MemoryTracker();

		public SceneObjectInfo(SceneObjectsContext context, NetworkObject networkObject)
		{
			Name          = networkObject.name;
			Transform     = networkObject.transform;
			GameObject    = networkObject.gameObject;
			NetworkObject = networkObject;
			NetworkId     = new NetworkIdLabel(networkObject.Id);

			UpdateComponents(context);
		}

		public bool HasAnyComponentRecursive(HashSet<Type> types)
		{
			foreach (ComponentInfo component in Components)
			{
				if (types.Contains(component.Type) == true)
					return true;

				ComponentInfo baseComponent = component.Base;
				while (baseComponent != null)
				{
					if (types.Contains(baseComponent.Type) == true)
						return true;

					baseComponent = baseComponent.Base;
				}
			}

			return false;
		}

		public void Update(SceneObjectsContext context)
		{
			IsValid = true;

			if (Application.isPlaying == false)
			{
				Name = GameObject.name;
			}

			IsActive              = GameObject.activeSelf;
			InterestMode          = NetworkObjectUtility.GetObjectInterest(NetworkObject);
			IsInSimulation        = NetworkObject.IsInSimulation;
			HasStateAuthority     = NetworkObject.HasStateAuthority;
			HasInputAuthority     = NetworkObject.HasInputAuthority;
			StateAuthority.Value  = NetworkObject.StateAuthority;
			StateAuthoritySortKey = GetAuthoritySortKey(StateAuthority.Value, context.LocalPlayer);
			InputAuthority.Value  = NetworkObject.InputAuthority;
			InputAuthoritySortKey = GetAuthoritySortKey(InputAuthority.Value, context.LocalPlayer);

			int behaviourCount = NetworkObject.NetworkedBehaviours != null ? NetworkObject.NetworkedBehaviours.Length : default;
			if (behaviourCount != _behaviours.Length)
			{
				UpdateComponents(context);
			}
			else if (behaviourCount > 0)
			{
				for (int i = 0; i < behaviourCount; ++i)
				{
					if (object.ReferenceEquals(_behaviours[i], NetworkObject.NetworkedBehaviours[i]) == false)
					{
						UpdateComponents(context);
						break;
					}
				}
			}

			if (context.HasDistanceOrigin == true)
			{
				Distance.Value = Vector3.Distance(Transform.position, context.DistanceOrigin);
			}
			else
			{
				Distance.Clear();
			}

			if (context.ProcessStateChanges == true)
			{
				ProcessStateChanges();
			}
		}

		public void ResetSession(bool refresh)
		{
			if (refresh == true)
			{
				Name = GameObject.name;
			}

			TotalStateChanges.Clear();
			AverageStateChanges.Clear();

			_stateTracker.Clear();
		}

		private void UpdateComponents(SceneObjectsContext context)
		{
			int count = NetworkObject.NetworkedBehaviours != null ? NetworkObject.NetworkedBehaviours.Length : default;

			_behaviours = new NetworkBehaviour[count];
			Components = new ComponentInfo[count];
			ComponentCount = new IntLabel(count);

			for (int i = 0; i < count; ++i)
			{
				NetworkBehaviour networkBehaviour = NetworkObject.NetworkedBehaviours[i];

				_behaviours[i] = networkBehaviour;
				Components[i] = context.Components.GetComponent(networkBehaviour.GetType());
			}

			try
			{
				StateSize = new MemoryLabel(false, Mathf.Max(-1, NetworkObject.GetWordCount(NetworkObject) * 4));
			}
			catch
			{
				StateSize = new MemoryLabel(false, FusionInspector.STATE_SIZE_UNKNOWN, "---");
			}
		}

		private static int GetAuthoritySortKey(PlayerRef authority, PlayerRef localPlayer)
		{
			if (authority.IsRealPlayer == true)
				return authority == localPlayer ? int.MinValue : authority.RawEncoded;

			if (authority.IsMasterClient == true)
				return int.MinValue + 1;

			return int.MaxValue;
		}

		private void ProcessStateChanges()
		{
			int componentCount    = NetworkObject.NetworkedBehaviours != null ? NetworkObject.NetworkedBehaviours.Length : default;
			int changedObjectBits = default;

			if (_stateTrackers == null || _stateTrackers.Length != componentCount)
			{
				_stateTrackers = new NetworkStateTracker[componentCount];

				for (int i = 0; i < componentCount; ++i)
				{
					_stateTrackers[i] = new NetworkStateTracker(NetworkObject.NetworkedBehaviours[i]);
				}
			}
			else
			{
				for (int i = 0; i < componentCount; ++i)
				{
					int changedComponentBits = _stateTrackers[i].ExchangeState(NetworkObject.NetworkedBehaviours[i]);
					changedObjectBits += changedComponentBits;
					Components[i].AccumulateStateChange(changedComponentBits, IsSelected);
				}
			}

			_stateTracker.AccumulateChange(changedObjectBits);

			int totalBits = _stateTracker.ProcessAccumulatedChanges();

			TotalStateChanges.Value += totalBits;
			AverageStateChanges.Value = _stateTracker.BytesPerSecond;
		}
	}
}
