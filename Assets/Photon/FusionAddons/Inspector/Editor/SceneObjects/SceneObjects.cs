namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	[Serializable]
	internal sealed class SceneObjects
	{
		public List<SceneObjectInfo> All = new List<SceneObjectInfo>(512);
		public List<SceneObjectInfo> Filtered = new List<SceneObjectInfo>(512);
		public HashSet<Type>         ComponentFilterInclude = new HashSet<Type>();
		public HashSet<Type>         ComponentFilterExclude = new HashSet<Type>();
		public string                ObjectNameFilter;
		public EFilterMode           IsInSimulationFilter;
		public EFilterMode           HasStateAuthorityFilter;
		public EFilterMode           HasInputAuthorityFilter;
		public EFilterMode           IsMasterClientObjectFilter;
		public EFilterMode           AllowStateAuthorityOverrideFilter;
		public EFilterMode           DestroyWhenStateAuthorityLeavesFilter;
		public ESceneObjectSortMode  SortMode = ESceneObjectSortMode.NameAscending;
		public ESceneObjectField     VisibleFields;
		public bool                  VisibleFieldsFoldout;
		public bool                  ComponentFilterFoldout;

		private SceneObjectsContext _context = new SceneObjectsContext();
		private HashSet<ComponentInfo> _tempComponents = new HashSet<ComponentInfo>();
		private Dictionary<NetworkObject, SceneObjectInfo> _allByNO = new Dictionary<NetworkObject, SceneObjectInfo>();

		public void GetObjects(List<SceneObjectInfo> objects, Type type)
		{
			objects.Clear();

			foreach (SceneObjectInfo sceneObject in All)
			{
				foreach (ComponentInfo component in sceneObject.Components)
				{
					if (type.IsAssignableFrom(component.Type) == true)
					{
						objects.Add(sceneObject);
						break;
					}
				}
			}
		}

		public void Synchronize(Components components, List<NetworkObject> networkObjects, PlayerRef localPlayer, Vector3 distanceOrigin, bool hasDistanceOrigin, bool processStateChanges)
		{
			_context.Components          = components;
			_context.LocalPlayer         = localPlayer;
			_context.DistanceOrigin      = distanceOrigin;
			_context.HasDistanceOrigin   = hasDistanceOrigin;
			_context.ProcessStateChanges = processStateChanges;

			foreach (SceneObjectInfo sceneObject in All)
			{
				sceneObject.IsValid = false;
			}

			foreach (NetworkObject networkObject in networkObjects)
			{
				if (_allByNO.TryGetValue(networkObject, out SceneObjectInfo sceneObject) == false)
				{
					sceneObject = new SceneObjectInfo(_context, networkObject);

					_allByNO[networkObject] = sceneObject;
					All.Add(sceneObject);
				}

				sceneObject.Update(_context);
			}

			for (int i = All.Count - 1; i >= 0; --i)
			{
				SceneObjectInfo sceneObject = All[i];
				if (sceneObject.IsValid == false || sceneObject.NetworkObject == null)
				{
					_allByNO.Remove(sceneObject.NetworkObject);
					All.RemoveAt(i);
				}
			}
		}

		public void Refresh(Components components)
		{
			Filtered.Clear();

			foreach (ComponentInfo component in components.All)
			{
				component.SceneObjects.ScriptCount.Value         = default;
				component.SceneObjects.AllObjectCount.Value      = default;
				component.SceneObjects.FilteredObjectCount.Value = default;
			}

			if (All.Count <= 0)
				return;

			Sort(All, SortMode);

			bool hasNameFilter                            = string.IsNullOrWhiteSpace(ObjectNameFilter) == false;
			bool hasIsInSimulationFilter                  = IsInSimulationFilter                  != EFilterMode.None;
			bool hasHasStateAuthorityFilter               = HasStateAuthorityFilter               != EFilterMode.None;
			bool hasHasInputAuthorityFilter               = HasInputAuthorityFilter               != EFilterMode.None;
			bool hasIsMasterClientObjectFilter            = IsMasterClientObjectFilter            != EFilterMode.None;
			bool hasAllowStateAuthorityOverrideFilter     = AllowStateAuthorityOverrideFilter     != EFilterMode.None;
			bool hasDestroyWhenStateAuthorityLeavesFilter = DestroyWhenStateAuthorityLeavesFilter != EFilterMode.None;
			bool hasComponentFilterInclude                = ComponentFilterInclude.Count > 0;
			bool hasComponentFilterExclude                = ComponentFilterExclude.Count > 0;

			bool hasAnyFilter  = false;
			hasAnyFilter |= hasNameFilter;
			hasAnyFilter |= hasIsInSimulationFilter;
			hasAnyFilter |= hasHasStateAuthorityFilter;
			hasAnyFilter |= hasHasInputAuthorityFilter;
			hasAnyFilter |= hasIsMasterClientObjectFilter;
			hasAnyFilter |= hasAllowStateAuthorityOverrideFilter;
			hasAnyFilter |= hasDestroyWhenStateAuthorityLeavesFilter;
			hasAnyFilter |= hasComponentFilterInclude;
			hasAnyFilter |= hasComponentFilterExclude;

			foreach (SceneObjectInfo sceneObject in All)
			{
				bool isFiltered = true;

				if (hasAnyFilter == true)
				{
					if (hasNameFilter                            == true) isFiltered &= sceneObject.Name.Contains(ObjectNameFilter, StringComparison.OrdinalIgnoreCase);
					if (hasIsInSimulationFilter                  == true) isFiltered &= (IsInSimulationFilter                  == EFilterMode.Include && sceneObject.IsInSimulation                  == true) || (IsInSimulationFilter                  == EFilterMode.Exclude && sceneObject.IsInSimulation                  == false);
					if (hasHasStateAuthorityFilter               == true) isFiltered &= (HasStateAuthorityFilter               == EFilterMode.Include && sceneObject.HasStateAuthority               == true) || (HasStateAuthorityFilter               == EFilterMode.Exclude && sceneObject.HasStateAuthority               == false);
					if (hasHasInputAuthorityFilter               == true) isFiltered &= (HasInputAuthorityFilter               == EFilterMode.Include && sceneObject.HasInputAuthority               == true) || (HasStateAuthorityFilter               == EFilterMode.Exclude && sceneObject.HasStateAuthority               == false);
					if (hasIsMasterClientObjectFilter            == true) isFiltered &= (IsMasterClientObjectFilter            == EFilterMode.Include && sceneObject.IsMasterClientObject            == true) || (IsMasterClientObjectFilter            == EFilterMode.Exclude && sceneObject.IsMasterClientObject            == false);
					if (hasAllowStateAuthorityOverrideFilter     == true) isFiltered &= (AllowStateAuthorityOverrideFilter     == EFilterMode.Include && sceneObject.AllowStateAuthorityOverride     == true) || (AllowStateAuthorityOverrideFilter     == EFilterMode.Exclude && sceneObject.AllowStateAuthorityOverride     == false);
					if (hasDestroyWhenStateAuthorityLeavesFilter == true) isFiltered &= (DestroyWhenStateAuthorityLeavesFilter == EFilterMode.Include && sceneObject.DestroyWhenStateAuthorityLeaves == true) || (DestroyWhenStateAuthorityLeavesFilter == EFilterMode.Exclude && sceneObject.DestroyWhenStateAuthorityLeaves == false);
					if (hasComponentFilterInclude                == true) isFiltered &= sceneObject.HasAnyComponentRecursive(ComponentFilterInclude) == true;
					if (hasComponentFilterExclude                == true) isFiltered &= sceneObject.HasAnyComponentRecursive(ComponentFilterExclude) == false;
				}

				if (isFiltered == true)
				{
					Filtered.Add(sceneObject);
				}
				else
				{
					sceneObject.IsSelected = false;
				}

				_tempComponents.Clear();

				foreach (ComponentInfo component in sceneObject.Components)
				{
					if (_tempComponents.Add(component) == true)
					{
						++component.SceneObjects.ScriptCount.Value;
						++component.SceneObjects.AllObjectCount.Value;

						if (isFiltered == true)
						{
							++component.SceneObjects.FilteredObjectCount.Value;
						}

						ComponentInfo baseComponent = component.Base;
						while (baseComponent != null && _tempComponents.Add(baseComponent) == true)
						{
							++baseComponent.SceneObjects.AllObjectCount.Value;

							if (isFiltered == true)
							{
								++baseComponent.SceneObjects.FilteredObjectCount.Value;
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
			ESceneObjectField defaultFields = ESceneObjectField.Object;
			defaultFields |= ESceneObjectField.InterestMode;
			defaultFields |= ESceneObjectField.IsInSimulation;
			defaultFields |= ESceneObjectField.HasStateAuthority;
			defaultFields |= ESceneObjectField.NetworkId;
			defaultFields |= ESceneObjectField.StateAuthority;
			defaultFields |= ESceneObjectField.TotalStateChanges;
			defaultFields |= ESceneObjectField.StateSize;
			defaultFields |= ESceneObjectField.Distance;
			defaultFields |= ESceneObjectField.Components;
			VisibleFields = (ESceneObjectField)EditorPrefs.GetInt($"{nameof(FusionInspector)}.{nameof(SceneObjects)}.{nameof(VisibleFields)}", (int)defaultFields);
		}

		public void ToggleVisibleField(ESceneObjectField field)
		{
			VisibleFields ^= field;
			EditorPrefs.SetInt($"{nameof(FusionInspector)}.{nameof(SceneObjects)}.{nameof(VisibleFields)}", (int)VisibleFields);
		}

		public static void Sort(List<SceneObjectInfo> objects, ESceneObjectSortMode sortMode)
		{
			switch (sortMode)
			{
				case ESceneObjectSortMode.None:
					break;

				case ESceneObjectSortMode.NameAscending:                 { ListUtility.InsertionSort(objects, (o1, o2) => string.CompareOrdinal(o1.Name, o2.Name)); break; }
				case ESceneObjectSortMode.NameDescending:                { ListUtility.InsertionSort(objects, (o1, o2) => string.CompareOrdinal(o2.Name, o1.Name)); break; }
				case ESceneObjectSortMode.InterestModeAscending:         { ListUtility.InsertionSort(objects, (o1, o2) => o1.InterestMode.CompareTo(o2.InterestMode)); break; }
				case ESceneObjectSortMode.InterestModeDescending:        { ListUtility.InsertionSort(objects, (o1, o2) => o2.InterestMode.CompareTo(o1.InterestMode)); break; }
				case ESceneObjectSortMode.NetworkIdAscending:            { ListUtility.InsertionSort(objects, (o1, o2) => o1.NetworkId.Value.Raw.CompareTo(o2.NetworkId.Value.Raw)); break; }
				case ESceneObjectSortMode.NetworkIdDescending:           { ListUtility.InsertionSort(objects, (o1, o2) => o2.NetworkId.Value.Raw.CompareTo(o1.NetworkId.Value.Raw)); break; }
				case ESceneObjectSortMode.StateAuthorityAscending:       { ListUtility.InsertionSort(objects, (o1, o2) => o1.StateAuthoritySortKey.CompareTo(o2.StateAuthoritySortKey)); break; }
				case ESceneObjectSortMode.StateAuthorityDescending:      { ListUtility.InsertionSort(objects, (o1, o2) => o2.StateAuthoritySortKey.CompareTo(o1.StateAuthoritySortKey)); break; }
				case ESceneObjectSortMode.InputAuthorityAscending:       { ListUtility.InsertionSort(objects, (o1, o2) => o1.InputAuthoritySortKey.CompareTo(o2.InputAuthoritySortKey)); break; }
				case ESceneObjectSortMode.InputAuthorityDescending:      { ListUtility.InsertionSort(objects, (o1, o2) => o2.InputAuthoritySortKey.CompareTo(o1.InputAuthoritySortKey)); break; }
				case ESceneObjectSortMode.TotalStateChangesAscending:    { ListUtility.InsertionSort(objects, (o1, o2) => o1.TotalStateChanges.Value.CompareTo(o2.TotalStateChanges.Value)); break; }
				case ESceneObjectSortMode.TotalStateChangesDescending:   { ListUtility.InsertionSort(objects, (o1, o2) => o2.TotalStateChanges.Value.CompareTo(o1.TotalStateChanges.Value)); break; }
				case ESceneObjectSortMode.AverageStateChangesAscending:  { ListUtility.InsertionSort(objects, (o1, o2) => o1.AverageStateChanges.Value.CompareTo(o2.AverageStateChanges.Value)); break; }
				case ESceneObjectSortMode.AverageStateChangesDescending: { ListUtility.InsertionSort(objects, (o1, o2) => o2.AverageStateChanges.Value.CompareTo(o1.AverageStateChanges.Value)); break; }
				case ESceneObjectSortMode.StateSizeAscending:            { ListUtility.InsertionSort(objects, (o1, o2) => o1.StateSize.Value.CompareTo(o2.StateSize.Value)); break; }
				case ESceneObjectSortMode.StateSizeDescending:           { ListUtility.InsertionSort(objects, (o1, o2) => o2.StateSize.Value.CompareTo(o1.StateSize.Value)); break; }
				case ESceneObjectSortMode.DistanceAscending:             { ListUtility.InsertionSort(objects, (o1, o2) => o1.Distance.Value.CompareTo(o2.Distance.Value)); break; }
				case ESceneObjectSortMode.DistanceDescending:            { ListUtility.InsertionSort(objects, (o1, o2) => o2.Distance.Value.CompareTo(o1.Distance.Value)); break; }
				case ESceneObjectSortMode.ComponentsAscending:           { ListUtility.InsertionSort(objects, (o1, o2) => o1.ComponentCount.Value.CompareTo(o2.ComponentCount.Value)); break; }
				case ESceneObjectSortMode.ComponentsDescending:          { ListUtility.InsertionSort(objects, (o1, o2) => o2.ComponentCount.Value.CompareTo(o1.ComponentCount.Value)); break; }

				default:
					throw new NotSupportedException(sortMode.ToString());
			}
		}
	}
}
