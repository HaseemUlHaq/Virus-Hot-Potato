namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	internal sealed class PrefabInfo
	{
		public readonly Transform     Transform;
		public readonly GameObject    GameObject;
		public readonly NetworkObject NetworkObject;

		public string          Name;
		public bool            IsValid;
		public bool            IsActive;
		public bool            IsSelected;
		public ComponentInfo[] Components;
		public IntLabel        ComponentCount;
		public int             InterestMode;
		public MemoryLabel     StateSize = new MemoryLabel(false, default, "---");

		public bool IsSpawnable                     => NetworkObject.IsSpawnable;
		public bool IsMasterClientObject            => (NetworkObject.Flags & NetworkObjectFlags.MasterClientObject)              == NetworkObjectFlags.MasterClientObject;
		public bool AllowStateAuthorityOverride     => (NetworkObject.Flags & NetworkObjectFlags.AllowStateAuthorityOverride)     == NetworkObjectFlags.AllowStateAuthorityOverride;
		public bool DestroyWhenStateAuthorityLeaves => (NetworkObject.Flags & NetworkObjectFlags.DestroyWhenStateAuthorityLeaves) == NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;

		private NetworkBehaviour[] _behaviours;

		public PrefabInfo(Components components, NetworkObject networkObject)
		{
			Name          = networkObject.name;
			Transform     = networkObject.transform;
			GameObject    = networkObject.gameObject;
			NetworkObject = networkObject;

			UpdateComponents(components);
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

		public void Update(PrefabsContext context)
		{
			IsValid = true;

			if (Application.isPlaying == false)
			{
				Name = GameObject.name;
			}

			IsActive     = GameObject.activeSelf;
			InterestMode = NetworkObjectUtility.GetObjectInterest(NetworkObject);

			int behaviourCount = NetworkObject.NetworkedBehaviours != null ? NetworkObject.NetworkedBehaviours.Length : default;
			if (behaviourCount != _behaviours.Length)
			{
				UpdateComponents(context.Components);
			}
			else if (behaviourCount > 0)
			{
				for (int i = 0; i < behaviourCount; ++i)
				{
					if (object.ReferenceEquals(_behaviours[i], NetworkObject.NetworkedBehaviours[i]) == false)
					{
						UpdateComponents(context.Components);
						break;
					}
				}
			}
		}

		private void UpdateComponents(Components components)
		{
			int count = NetworkObject.NetworkedBehaviours != null ? NetworkObject.NetworkedBehaviours.Length : default;

			_behaviours = new NetworkBehaviour[count];
			Components = new ComponentInfo[count];
			ComponentCount = new IntLabel(count);

			for (int i = 0; i < count; ++i)
			{
				NetworkBehaviour networkBehaviour = NetworkObject.NetworkedBehaviours[i];

				_behaviours[i] = networkBehaviour;
				Components[i] = components.GetComponent(networkBehaviour.GetType());
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
	}
}
