namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;

	[Serializable]
	internal sealed class Components
	{
		public List<ComponentInfo> All      = new List<ComponentInfo>(512);
		public List<ComponentInfo> Filtered = new List<ComponentInfo>(512);
		public EComponentSortMode  SortMode = EComponentSortMode.TypeHierarchyAscending;
		public string              TypeNameFilter;
		public EComponentField     VisibleFields;
		public bool                VisibleFieldsFoldout;

		public EFilterMode         HasSpawnedFilter;
		public EFilterMode         HasDespawnedFilter;
		public EFilterMode         HasFixedUpdateNetworkFilter;
		public EFilterMode         HasRenderFilter;

		public EFilterMode         HasAfterSpawnedFilter;
		public EFilterMode         HasAfterHostMigrationFilter;
		public EFilterMode         HasStateAuthorityChangedFilter;
		public EFilterMode         HasInputAuthorityGainedFilter;
		public EFilterMode         HasInputAuthorityLostFilter;
		public EFilterMode         HasSimulationEnterFilter;
		public EFilterMode         HasSimulationExitFilter;
		public EFilterMode         HasInterestEnterFilter;
		public EFilterMode         HasInterestExitFilter;

		public EFilterMode         HasBeforeUpdateFilter;
		public EFilterMode         HasBeforeCopyPreviousStateFilter;
		public EFilterMode         HasBeforeClientPredictionResetFilter;
		public EFilterMode         HasAfterClientPredictionResetFilter;
		public EFilterMode         HasBeforeSimulationFilter;
		public EFilterMode         HasBeforeAllTicksFilter;
		public EFilterMode         HasBeforeTickFilter;
		public EFilterMode         HasAfterTickFilter;
		public EFilterMode         HasAfterAllTicksFilter;
		public EFilterMode         HasAfterRenderFilter;
		public EFilterMode         HasAfterUpdateFilter;

		private Dictionary<Type, ComponentInfo> _allByType = new Dictionary<Type, ComponentInfo>();

		public ComponentInfo GetComponent(Type type)
		{
			return _allByType[type];
		}

		public bool TryGetComponent(Type type, out ComponentInfo component)
		{
			return _allByType.TryGetValue(type, out component);
		}

		public void Synchronize()
		{
			if (All.Count > 0)
				return;

			Dictionary<Type, MonoScript> monoScripts = new Dictionary<Type, MonoScript>();

			foreach(MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
			{
				Type scriptClass = monoScript.GetClass();
				if (scriptClass != null)
				{
					monoScripts[scriptClass] = monoScript;
				}
			}

			TypeCache.TypeCollection typeCollection = TypeCache.GetTypesDerivedFrom<NetworkBehaviour>();
			for (int i = 0; i < typeCollection.Count; ++i)
			{
				Type type = typeCollection[i];
				if (type.IsAbstract == true || type.IsGenericType == true)
					continue;

				GetOrAddComponent(type);
			}

			return;

			ComponentInfo GetOrAddComponent(Type type)
			{
				if (type == null)
					return null;
				if (type == typeof(NetworkBehaviour))
					return null;
				if (_allByType.TryGetValue(type, out ComponentInfo component) == true)
					return component;

				ComponentInfo baseComponent = GetOrAddComponent(type.BaseType);

				monoScripts.TryGetValue(type, out MonoScript monoScript);
				component = new ComponentInfo(type, monoScript, baseComponent);

				_allByType.Add(type, component);
				All.Add(component);

				return component;
			}
		}

		public void ProcessAccumulatedStateChanges()
		{
			foreach (ComponentInfo component in All)
			{
				component.ProcessAccumulatedStateChanges();
			}
		}

		public void ClearSelectedStateChanges()
		{
			foreach (ComponentInfo component in All)
			{
				component.SelectedStateChanges.Clear();
			}
		}

		public void Refresh()
		{
			Filtered.Clear();

			Sort(All, SortMode);

			bool hasTypeNameFilter                    = string.IsNullOrWhiteSpace(TypeNameFilter) == false;
			bool hasSpawnedFilter                     = HasSpawnedFilter                     != EFilterMode.None;
			bool hasDespawnedFilter                   = HasDespawnedFilter                   != EFilterMode.None;
			bool hasFixedUpdateNetworkFilter          = HasFixedUpdateNetworkFilter          != EFilterMode.None;
			bool hasRenderFilter                      = HasRenderFilter                      != EFilterMode.None;
			bool hasAfterSpawnedFilter                = HasAfterSpawnedFilter                != EFilterMode.None;
			bool hasAfterHostMigrationFilter          = HasAfterHostMigrationFilter          != EFilterMode.None;
			bool hasStateAuthorityChangedFilter       = HasStateAuthorityChangedFilter       != EFilterMode.None;
			bool hasInputAuthorityGainedFilter        = HasInputAuthorityGainedFilter        != EFilterMode.None;
			bool hasInputAuthorityLostFilter          = HasInputAuthorityLostFilter          != EFilterMode.None;
			bool hasSimulationEnterFilter             = HasSimulationEnterFilter             != EFilterMode.None;
			bool hasSimulationExitFilter              = HasSimulationExitFilter              != EFilterMode.None;
			bool hasInterestEnterFilter               = HasInterestEnterFilter               != EFilterMode.None;
			bool hasInterestExitFilter                = HasInterestExitFilter                != EFilterMode.None;
			bool hasBeforeUpdateFilter                = HasBeforeUpdateFilter                != EFilterMode.None;
			bool hasBeforeCopyPreviousStateFilter     = HasBeforeCopyPreviousStateFilter     != EFilterMode.None;
			bool hasBeforeClientPredictionResetFilter = HasBeforeClientPredictionResetFilter != EFilterMode.None;
			bool hasAfterClientPredictionResetFilter  = HasAfterClientPredictionResetFilter  != EFilterMode.None;
			bool hasBeforeSimulationFilter            = HasBeforeSimulationFilter            != EFilterMode.None;
			bool hasBeforeAllTicksFilter              = HasBeforeAllTicksFilter              != EFilterMode.None;
			bool hasBeforeTickFilter                  = HasBeforeTickFilter                  != EFilterMode.None;
			bool hasAfterTickFilter                   = HasAfterTickFilter                   != EFilterMode.None;
			bool hasAfterAllTicksFilter               = HasAfterAllTicksFilter               != EFilterMode.None;
			bool hasAfterRenderFilter                 = HasAfterRenderFilter                 != EFilterMode.None;
			bool hasAfterUpdateFilter                 = HasAfterUpdateFilter                 != EFilterMode.None;

			bool hasAnyFilter  = false;
			hasAnyFilter |= hasTypeNameFilter;
			hasAnyFilter |= hasSpawnedFilter;
			hasAnyFilter |= hasDespawnedFilter;
			hasAnyFilter |= hasFixedUpdateNetworkFilter;
			hasAnyFilter |= hasRenderFilter;
			hasAnyFilter |= hasAfterSpawnedFilter;
			hasAnyFilter |= hasAfterHostMigrationFilter;
			hasAnyFilter |= hasStateAuthorityChangedFilter;
			hasAnyFilter |= hasInputAuthorityGainedFilter;
			hasAnyFilter |= hasInputAuthorityLostFilter;
			hasAnyFilter |= hasSimulationEnterFilter;
			hasAnyFilter |= hasSimulationExitFilter;
			hasAnyFilter |= hasInterestEnterFilter;
			hasAnyFilter |= hasInterestExitFilter;
			hasAnyFilter |= hasBeforeUpdateFilter;
			hasAnyFilter |= hasBeforeCopyPreviousStateFilter;
			hasAnyFilter |= hasBeforeClientPredictionResetFilter;
			hasAnyFilter |= hasAfterClientPredictionResetFilter;
			hasAnyFilter |= hasBeforeSimulationFilter;
			hasAnyFilter |= hasBeforeAllTicksFilter;
			hasAnyFilter |= hasBeforeTickFilter;
			hasAnyFilter |= hasAfterTickFilter;
			hasAnyFilter |= hasAfterAllTicksFilter;
			hasAnyFilter |= hasAfterRenderFilter;
			hasAnyFilter |= hasAfterUpdateFilter;

			foreach (ComponentInfo component in All)
			{
				bool isFiltered = true;

				if (hasAnyFilter == true)
				{
					if (hasTypeNameFilter                    == true) isFiltered &= component.TypeName.Contains(TypeNameFilter, StringComparison.OrdinalIgnoreCase);
					if (hasSpawnedFilter                     == true) isFiltered &= (HasSpawnedFilter                     == EFilterMode.Include && component.HasSpawned                     == true) || (HasSpawnedFilter                     == EFilterMode.Exclude && component.HasSpawned                     == false);
					if (hasDespawnedFilter                   == true) isFiltered &= (HasDespawnedFilter                   == EFilterMode.Include && component.HasDespawned                   == true) || (HasDespawnedFilter                   == EFilterMode.Exclude && component.HasDespawned                   == false);
					if (hasFixedUpdateNetworkFilter          == true) isFiltered &= (HasFixedUpdateNetworkFilter          == EFilterMode.Include && component.HasFixedUpdateNetwork          == true) || (HasFixedUpdateNetworkFilter          == EFilterMode.Exclude && component.HasFixedUpdateNetwork          == false);
					if (hasRenderFilter                      == true) isFiltered &= (HasRenderFilter                      == EFilterMode.Include && component.HasRender                      == true) || (HasRenderFilter                      == EFilterMode.Exclude && component.HasRender                      == false);
					if (hasAfterSpawnedFilter                == true) isFiltered &= (HasAfterSpawnedFilter                == EFilterMode.Include && component.HasAfterSpawned                == true) || (HasAfterSpawnedFilter                == EFilterMode.Exclude && component.HasAfterSpawned                == false);
					if (hasAfterHostMigrationFilter          == true) isFiltered &= (HasAfterHostMigrationFilter          == EFilterMode.Include && component.HasAfterHostMigration          == true) || (HasAfterHostMigrationFilter          == EFilterMode.Exclude && component.HasAfterHostMigration          == false);
					if (hasStateAuthorityChangedFilter       == true) isFiltered &= (HasStateAuthorityChangedFilter       == EFilterMode.Include && component.HasStateAuthorityChanged       == true) || (HasStateAuthorityChangedFilter       == EFilterMode.Exclude && component.HasStateAuthorityChanged       == false);
					if (hasInputAuthorityGainedFilter        == true) isFiltered &= (HasInputAuthorityGainedFilter        == EFilterMode.Include && component.HasInputAuthorityGained        == true) || (HasInputAuthorityGainedFilter        == EFilterMode.Exclude && component.HasInputAuthorityGained        == false);
					if (hasInputAuthorityLostFilter          == true) isFiltered &= (HasInputAuthorityLostFilter          == EFilterMode.Include && component.HasInputAuthorityLost          == true) || (HasInputAuthorityLostFilter          == EFilterMode.Exclude && component.HasInputAuthorityLost          == false);
					if (hasSimulationEnterFilter             == true) isFiltered &= (HasSimulationEnterFilter             == EFilterMode.Include && component.HasSimulationEnter             == true) || (HasSimulationEnterFilter             == EFilterMode.Exclude && component.HasSimulationEnter             == false);
					if (hasSimulationExitFilter              == true) isFiltered &= (HasSimulationExitFilter              == EFilterMode.Include && component.HasSimulationExit              == true) || (HasSimulationExitFilter              == EFilterMode.Exclude && component.HasSimulationExit              == false);
					if (hasInterestEnterFilter               == true) isFiltered &= (HasInterestEnterFilter               == EFilterMode.Include && component.HasInterestEnter               == true) || (HasInterestEnterFilter               == EFilterMode.Exclude && component.HasInterestEnter               == false);
					if (hasInterestExitFilter                == true) isFiltered &= (HasInterestExitFilter                == EFilterMode.Include && component.HasInterestExit                == true) || (HasInterestExitFilter                == EFilterMode.Exclude && component.HasInterestExit                == false);
					if (hasBeforeUpdateFilter                == true) isFiltered &= (HasBeforeUpdateFilter                == EFilterMode.Include && component.HasBeforeUpdate                == true) || (HasBeforeUpdateFilter                == EFilterMode.Exclude && component.HasBeforeUpdate                == false);
					if (hasBeforeCopyPreviousStateFilter     == true) isFiltered &= (HasBeforeCopyPreviousStateFilter     == EFilterMode.Include && component.HasBeforeCopyPreviousState     == true) || (HasBeforeCopyPreviousStateFilter     == EFilterMode.Exclude && component.HasBeforeCopyPreviousState     == false);
					if (hasBeforeClientPredictionResetFilter == true) isFiltered &= (HasBeforeClientPredictionResetFilter == EFilterMode.Include && component.HasBeforeClientPredictionReset == true) || (HasBeforeClientPredictionResetFilter == EFilterMode.Exclude && component.HasBeforeClientPredictionReset == false);
					if (hasAfterClientPredictionResetFilter  == true) isFiltered &= (HasAfterClientPredictionResetFilter  == EFilterMode.Include && component.HasAfterClientPredictionReset  == true) || (HasAfterClientPredictionResetFilter  == EFilterMode.Exclude && component.HasAfterClientPredictionReset  == false);
					if (hasBeforeSimulationFilter            == true) isFiltered &= (HasBeforeSimulationFilter            == EFilterMode.Include && component.HasBeforeSimulation            == true) || (HasBeforeSimulationFilter            == EFilterMode.Exclude && component.HasBeforeSimulation            == false);
					if (hasBeforeAllTicksFilter              == true) isFiltered &= (HasBeforeAllTicksFilter              == EFilterMode.Include && component.HasBeforeAllTicks              == true) || (HasBeforeAllTicksFilter              == EFilterMode.Exclude && component.HasBeforeAllTicks              == false);
					if (hasBeforeTickFilter                  == true) isFiltered &= (HasBeforeTickFilter                  == EFilterMode.Include && component.HasBeforeTick                  == true) || (HasBeforeTickFilter                  == EFilterMode.Exclude && component.HasBeforeTick                  == false);
					if (hasAfterTickFilter                   == true) isFiltered &= (HasAfterTickFilter                   == EFilterMode.Include && component.HasAfterTick                   == true) || (HasAfterTickFilter                   == EFilterMode.Exclude && component.HasAfterTick                   == false);
					if (hasAfterAllTicksFilter               == true) isFiltered &= (HasAfterAllTicksFilter               == EFilterMode.Include && component.HasAfterAllTicks               == true) || (HasAfterAllTicksFilter               == EFilterMode.Exclude && component.HasAfterAllTicks               == false);
					if (hasAfterRenderFilter                 == true) isFiltered &= (HasAfterRenderFilter                 == EFilterMode.Include && component.HasAfterRender                 == true) || (HasAfterRenderFilter                 == EFilterMode.Exclude && component.HasAfterRender                 == false);
					if (hasAfterUpdateFilter                 == true) isFiltered &= (HasAfterUpdateFilter                 == EFilterMode.Include && component.HasAfterUpdate                 == true) || (HasAfterUpdateFilter                 == EFilterMode.Exclude && component.HasAfterUpdate                 == false);
				}

				if (isFiltered == true)
				{
					Filtered.Add(component);
				}
			}
		}

		public void Clear()
		{
			All.Clear();
			Filtered.Clear();

			_allByType.Clear();
		}

		public void LoadEditorPrefs()
		{
			EComponentField defaultFields = EComponentField.Type;
			defaultFields |= EComponentField.HasSpawned;
			defaultFields |= EComponentField.HasDespawned;
			defaultFields |= EComponentField.HasFixedUpdateNetwork;
			defaultFields |= EComponentField.HasRender;
			defaultFields |= EComponentField.SceneObjects;
			defaultFields |= EComponentField.Prefabs;
			defaultFields |= EComponentField.RPCs;
			defaultFields |= EComponentField.StateSize;
			defaultFields |= EComponentField.ExecutionOrder;
			defaultFields |= EComponentField.TotalStateChanges;
			VisibleFields = (EComponentField)EditorPrefs.GetInt($"{nameof(FusionInspector)}.{nameof(Components)}.{nameof(VisibleFields)}", (int)defaultFields);
		}

		public void ToggleVisibleField(EComponentField field)
		{
			VisibleFields ^= field;
			EditorPrefs.SetInt($"{nameof(FusionInspector)}.{nameof(Components)}.{nameof(VisibleFields)}", (int)VisibleFields);
		}

		public static void Sort(List<ComponentInfo> components, EComponentSortMode sortMode)
		{
			switch (sortMode)
			{
				case EComponentSortMode.None:
					break;

				case EComponentSortMode.TypeNameAscending:             { ListUtility.InsertionSort(components, (c1, c2) => c2.TypeName.StartsWith(c1.TypeName, StringComparison.Ordinal) ? -1 : string.CompareOrdinal(c1.TypeName, c2.TypeName)); break; }
				case EComponentSortMode.TypeNameDescending:            { ListUtility.InsertionSort(components, (c1, c2) => c2.TypeName.StartsWith(c1.TypeName, StringComparison.Ordinal) ? -1 : string.CompareOrdinal(c2.TypeName, c1.TypeName)); break; }
				case EComponentSortMode.TypeHierarchyAscending:        { ListUtility.InsertionSort(components, (c1, c2) => c2.TypeHierarchy.StartsWith(c1.TypeHierarchy, StringComparison.Ordinal) ? -1 : string.CompareOrdinal(c1.TypeHierarchy, c2.TypeHierarchy)); break; }
				case EComponentSortMode.TypeHierarchyDescending:       { ListUtility.InsertionSort(components, (c1, c2) => c2.TypeHierarchy.StartsWith(c1.TypeHierarchy, StringComparison.Ordinal) ? -1 : string.CompareOrdinal(c2.TypeHierarchy, c1.TypeHierarchy)); break; }
				case EComponentSortMode.SceneObjectsAscending:         { ListUtility.InsertionSort(components, (c1, c2) => c1.SceneObjects.ScriptCount.Value.CompareTo(c2.SceneObjects.ScriptCount.Value)); break; }
				case EComponentSortMode.SceneObjectsDescending:        { ListUtility.InsertionSort(components, (c1, c2) => c2.SceneObjects.ScriptCount.Value.CompareTo(c1.SceneObjects.ScriptCount.Value)); break; }
				case EComponentSortMode.PrefabsAscending:              { ListUtility.InsertionSort(components, (c1, c2) => c1.Prefabs.ScriptCount.Value.CompareTo(c2.Prefabs.ScriptCount.Value)); break; }
				case EComponentSortMode.PrefabsDescending:             { ListUtility.InsertionSort(components, (c1, c2) => c2.Prefabs.ScriptCount.Value.CompareTo(c1.Prefabs.ScriptCount.Value)); break; }
				case EComponentSortMode.RPCsAscending:                 { ListUtility.InsertionSort(components, (c1, c2) => c1.RPCs.Length.CompareTo(c2.RPCs.Length)); break; }
				case EComponentSortMode.RPCsDescending:                { ListUtility.InsertionSort(components, (c1, c2) => c2.RPCs.Length.CompareTo(c1.RPCs.Length)); break; }
				case EComponentSortMode.StateSizeAscending:            { ListUtility.InsertionSort(components, (c1, c2) => c1.StateSize.Value.CompareTo(c2.StateSize.Value)); break; }
				case EComponentSortMode.StateSizeDescending:           { ListUtility.InsertionSort(components, (c1, c2) => c2.StateSize.Value.CompareTo(c1.StateSize.Value)); break; }
				case EComponentSortMode.ExecutionOrderAscending:       { ListUtility.InsertionSort(components, (c1, c2) => c1.ExecutionOrder.Value.CompareTo(c2.ExecutionOrder.Value)); break; }
				case EComponentSortMode.ExecutionOrderDescending:      { ListUtility.InsertionSort(components, (c1, c2) => c2.ExecutionOrder.Value.CompareTo(c1.ExecutionOrder.Value)); break; }
				case EComponentSortMode.TotalStateChangesAscending:    { ListUtility.InsertionSort(components, (c1, c2) => c1.TotalStateChanges.Value.CompareTo(c2.TotalStateChanges.Value)); break; }
				case EComponentSortMode.TotalStateChangesDescending:   { ListUtility.InsertionSort(components, (c1, c2) => c2.TotalStateChanges.Value.CompareTo(c1.TotalStateChanges.Value)); break; }
				case EComponentSortMode.AverageStateChangesAscending:  { ListUtility.InsertionSort(components, (c1, c2) => c1.AverageStateChanges.Value.CompareTo(c2.AverageStateChanges.Value)); break; }
				case EComponentSortMode.AverageStateChangesDescending: { ListUtility.InsertionSort(components, (c1, c2) => c2.AverageStateChanges.Value.CompareTo(c1.AverageStateChanges.Value)); break; }

				default:
					throw new NotSupportedException(sortMode.ToString());
			}
		}
	}
}
