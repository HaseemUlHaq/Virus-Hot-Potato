namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;

	[Serializable]
	internal sealed class Prefabs
	{
		public List<PrefabInfo> All = new List<PrefabInfo>(512);
		public List<PrefabInfo> Filtered = new List<PrefabInfo>(512);
		public HashSet<Type>    ComponentFilterInclude = new HashSet<Type>();
		public HashSet<Type>    ComponentFilterExclude = new HashSet<Type>();
		public bool             ComponentFilterFoldout;
		public string           ObjectNameFilter;
		public EFilterMode      IsSpawnableFilter;
		public EFilterMode      IsMasterClientObjectFilter;
		public EFilterMode      AllowStateAuthorityOverrideFilter;
		public EFilterMode      DestroyWhenStateAuthorityLeavesFilter;
		public EPrefabSortMode  SortMode = EPrefabSortMode.NameAscending;
		public EPrefabField     VisibleFields;
		public bool             VisibleFieldsFoldout;

		private PrefabsContext _context = new PrefabsContext();
		private HashSet<ComponentInfo> _tempComponents = new HashSet<ComponentInfo>();
		private Dictionary<NetworkObject, PrefabInfo> _allByNO = new Dictionary<NetworkObject, PrefabInfo>();

		public void GetObjects(List<PrefabInfo> objects, Type type)
		{
			objects.Clear();

			foreach (PrefabInfo prefab in All)
			{
				foreach (ComponentInfo component in prefab.Components)
				{
					if (type.IsAssignableFrom(component.Type) == true)
					{
						objects.Add(prefab);
						break;
					}
				}
			}
		}

		public void Synchronize(Components components, List<NetworkObject> networkObjects)
		{
			_context.Components = components;

			foreach (PrefabInfo prefab in All)
			{
				prefab.IsValid = false;
			}

			foreach (NetworkObject networkObject in networkObjects)
			{
				if (_allByNO.TryGetValue(networkObject, out PrefabInfo prefab) == false)
				{
					prefab = new PrefabInfo(components, networkObject);

					_allByNO[networkObject] = prefab;
					All.Add(prefab);
				}

				prefab.Update(_context);
			}

			for (int i = All.Count - 1; i >= 0; --i)
			{
				PrefabInfo networkObjectInfo = All[i];
				if (networkObjectInfo.IsValid == false || networkObjectInfo.NetworkObject == null)
				{
					_allByNO.Remove(networkObjectInfo.NetworkObject);
					All.RemoveAt(i);
				}
			}
		}

		public void Refresh(Components components)
		{
			Filtered.Clear();

			foreach (ComponentInfo component in components.All)
			{
				component.Prefabs.ScriptCount.Value         = default;
				component.Prefabs.AllObjectCount.Value      = default;
				component.Prefabs.FilteredObjectCount.Value = default;
			}

			if (All.Count <= 0)
				return;

			Sort(All, SortMode);

			bool hasNameFilter                            = string.IsNullOrWhiteSpace(ObjectNameFilter) == false;
			bool hasIsSpawnableFilter                     = IsSpawnableFilter                     != EFilterMode.None;
			bool hasIsMasterClientObjectFilter            = IsMasterClientObjectFilter            != EFilterMode.None;
			bool hasAllowStateAuthorityOverrideFilter     = AllowStateAuthorityOverrideFilter     != EFilterMode.None;
			bool hasDestroyWhenStateAuthorityLeavesFilter = DestroyWhenStateAuthorityLeavesFilter != EFilterMode.None;
			bool hasComponentFilterInclude                = ComponentFilterInclude.Count > 0;
			bool hasComponentFilterExclude                = ComponentFilterExclude.Count > 0;

			bool hasAnyFilter  = false;
			hasAnyFilter |= hasNameFilter;
			hasAnyFilter |= hasIsSpawnableFilter;
			hasAnyFilter |= hasIsMasterClientObjectFilter;
			hasAnyFilter |= hasAllowStateAuthorityOverrideFilter;
			hasAnyFilter |= hasDestroyWhenStateAuthorityLeavesFilter;
			hasAnyFilter |= hasComponentFilterInclude;
			hasAnyFilter |= hasComponentFilterExclude;

			foreach (PrefabInfo prefab in All)
			{
				bool isFiltered = true;

				if (hasAnyFilter == true)
				{
					if (hasNameFilter                            == true) isFiltered &= prefab.Name.Contains(ObjectNameFilter, StringComparison.OrdinalIgnoreCase);
					if (hasIsSpawnableFilter                     == true) isFiltered &= (IsSpawnableFilter                     == EFilterMode.Include && prefab.IsSpawnable                     == true) || (IsSpawnableFilter                     == EFilterMode.Exclude && prefab.IsSpawnable                     == false);
					if (hasIsMasterClientObjectFilter            == true) isFiltered &= (IsMasterClientObjectFilter            == EFilterMode.Include && prefab.IsMasterClientObject            == true) || (IsMasterClientObjectFilter            == EFilterMode.Exclude && prefab.IsMasterClientObject            == false);
					if (hasAllowStateAuthorityOverrideFilter     == true) isFiltered &= (AllowStateAuthorityOverrideFilter     == EFilterMode.Include && prefab.AllowStateAuthorityOverride     == true) || (AllowStateAuthorityOverrideFilter     == EFilterMode.Exclude && prefab.AllowStateAuthorityOverride     == false);
					if (hasDestroyWhenStateAuthorityLeavesFilter == true) isFiltered &= (DestroyWhenStateAuthorityLeavesFilter == EFilterMode.Include && prefab.DestroyWhenStateAuthorityLeaves == true) || (DestroyWhenStateAuthorityLeavesFilter == EFilterMode.Exclude && prefab.DestroyWhenStateAuthorityLeaves == false);
					if (hasComponentFilterInclude                == true) isFiltered &= prefab.HasAnyComponentRecursive(ComponentFilterInclude) == true;
					if (hasComponentFilterExclude                == true) isFiltered &= prefab.HasAnyComponentRecursive(ComponentFilterExclude) == false;
				}

				if (isFiltered == true)
				{
					Filtered.Add(prefab);
				}
				else
				{
					prefab.IsSelected = false;
				}

				_tempComponents.Clear();

				foreach (ComponentInfo component in prefab.Components)
				{
					if (_tempComponents.Add(component) == true)
					{
						++component.Prefabs.ScriptCount.Value;
						++component.Prefabs.AllObjectCount.Value;

						if (isFiltered == true)
						{
							++component.Prefabs.FilteredObjectCount.Value;
						}

						ComponentInfo baseComponent = component.Base;
						while (baseComponent != null && _tempComponents.Add(baseComponent) == true)
						{
							++baseComponent.Prefabs.AllObjectCount.Value;

							if (isFiltered == true)
							{
								++baseComponent.Prefabs.FilteredObjectCount.Value;
							}

							baseComponent = baseComponent.Base;
						}
					}
				}
			}
		}

		public void Clear()
		{
			All.Clear();
			Filtered.Clear();

			_allByNO.Clear();
		}

		public void LoadEditorPrefs()
		{
			EPrefabField defaultFields = EPrefabField.Object;
			defaultFields |= EPrefabField.IsMasterClientObject;
			defaultFields |= EPrefabField.AllowStateAuthorityOverride;
			defaultFields |= EPrefabField.DestroyWhenStateAuthorityLeaves;
			defaultFields |= EPrefabField.InterestMode;
			defaultFields |= EPrefabField.StateSize;
			defaultFields |= EPrefabField.Components;
			VisibleFields = (EPrefabField)EditorPrefs.GetInt($"{nameof(FusionInspector)}.{nameof(Prefabs)}.{nameof(VisibleFields)}", (int)defaultFields);
		}

		public void ToggleVisibleField(EPrefabField field)
		{
			VisibleFields ^= field;
			EditorPrefs.SetInt($"{nameof(FusionInspector)}.{nameof(Prefabs)}.{nameof(VisibleFields)}", (int)VisibleFields);
		}

		public static void Sort(List<PrefabInfo> prefabs, EPrefabSortMode sortMode)
		{
			switch (sortMode)
			{
				case EPrefabSortMode.None:
					break;

				case EPrefabSortMode.NameAscending:          { ListUtility.InsertionSort(prefabs, (o1, o2) => string.CompareOrdinal(o1.Name, o2.Name)); break; }
				case EPrefabSortMode.NameDescending:         { ListUtility.InsertionSort(prefabs, (o1, o2) => string.CompareOrdinal(o2.Name, o1.Name)); break; }
				case EPrefabSortMode.InterestModeAscending:  { ListUtility.InsertionSort(prefabs, (o1, o2) => o1.InterestMode.CompareTo(o2.InterestMode)); break; }
				case EPrefabSortMode.InterestModeDescending: { ListUtility.InsertionSort(prefabs, (o1, o2) => o2.InterestMode.CompareTo(o1.InterestMode)); break; }
				case EPrefabSortMode.StateSizeAscending:     { ListUtility.InsertionSort(prefabs, (o1, o2) => o1.StateSize.Value.CompareTo(o2.StateSize.Value)); break; }
				case EPrefabSortMode.StateSizeDescending:    { ListUtility.InsertionSort(prefabs, (o1, o2) => o2.StateSize.Value.CompareTo(o1.StateSize.Value)); break; }
				case EPrefabSortMode.ComponentsAscending:    { ListUtility.InsertionSort(prefabs, (o1, o2) => o1.ComponentCount.Value.CompareTo(o2.ComponentCount.Value)); break; }
				case EPrefabSortMode.ComponentsDescending:   { ListUtility.InsertionSort(prefabs, (o1, o2) => o2.ComponentCount.Value.CompareTo(o1.ComponentCount.Value)); break; }

				default:
					throw new NotSupportedException(sortMode.ToString());
			}
		}
	}
}
