namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Reflection;
	using UnityEngine;
	using UnityEditor;

	internal sealed class ComponentInfo
	{
		public sealed class ObjectContext
		{
			public EFilterMode FilterMode;
			public FilterLabel ObjectFilter        = new FilterLabel();
			public IntLabel    ScriptCount         = new IntLabel();
			public IntLabel    AllObjectCount      = new IntLabel();
			public IntLabel    FilteredObjectCount = new IntLabel();
		}

		public readonly Type          Type;
		public readonly string        TypeName;
		public readonly string        TypeHierarchy;
		public readonly int           TypeLevel;
		public readonly MonoScript    Script;
		public readonly ComponentInfo Base;
		public readonly MethodInfo[]  RPCs;
		public readonly IntLabel      RPCCount;
		public readonly MemoryLabel   StateSize;
		public readonly IntLabel      ExecutionOrder;

		public readonly bool          HasSpawned;
		public readonly bool          HasDespawned;
		public readonly bool          HasFixedUpdateNetwork;
		public readonly bool          HasRender;

		public readonly bool          HasAfterSpawned;
		public readonly bool          HasAfterHostMigration;
		public readonly bool          HasStateAuthorityChanged;
		public readonly bool          HasInputAuthorityGained;
		public readonly bool          HasInputAuthorityLost;
		public readonly bool          HasSimulationEnter;
		public readonly bool          HasSimulationExit;
		public readonly bool          HasInterestEnter;
		public readonly bool          HasInterestExit;

		public readonly bool          HasBeforeUpdate;
		public readonly bool          HasBeforeCopyPreviousState;
		public readonly bool          HasBeforeClientPredictionReset;
		public readonly bool          HasAfterClientPredictionReset;
		public readonly bool          HasBeforeSimulation;
		public readonly bool          HasBeforeAllTicks;
		public readonly bool          HasBeforeTick;
		public readonly bool          HasAfterTick;
		public readonly bool          HasAfterAllTicks;
		public readonly bool          HasAfterRender;
		public readonly bool          HasAfterUpdate;

		public ObjectContext          Prefabs              = new ObjectContext();
		public ObjectContext          SceneObjects         = new ObjectContext();
		public MemoryLabel            TotalStateChanges    = new MemoryLabel(true);
		public MemoryLabel            SelectedStateChanges = new MemoryLabel(true);
		public MemoryPerSecondLabel   AverageStateChanges  = new MemoryPerSecondLabel();

		private MemoryTracker _stateTracker = new MemoryTracker();

		public ComponentInfo(Type type, MonoScript script, ComponentInfo baseComponent)
		{
			Type          = type;
			TypeName      = TypeUtility.GetTypeName(type);
			TypeHierarchy = TypeName;
			TypeLevel     = default;
			Script        = script;
			Base          = baseComponent;
			RPCs          = ReflectionUtility.GetRPCs(type);
			RPCCount      = new IntLabel(RPCs.Length);

			ComponentInfo hierarchyComponent = baseComponent;
			while (hierarchyComponent != null)
			{
				++TypeLevel;

				TypeHierarchy = $"{hierarchyComponent.TypeName}.{TypeHierarchy}";
				hierarchyComponent = hierarchyComponent.Base;
			}

			try
			{
				StateSize = new MemoryLabel(false, Mathf.Max(-1, NetworkBehaviourUtils.GetStaticWordCount(type) * 4));
			}
			catch
			{
				StateSize = new MemoryLabel(false, FusionInspector.STATE_SIZE_UNKNOWN, "---");
			}

			int executionOrder = TypeUtility.GetExecutionOrder(type, script);
			if (executionOrder == default && baseComponent != null)
			{
				executionOrder = baseComponent.ExecutionOrder.Value;
			}

			ExecutionOrder = new IntLabel(executionOrder);

			HasSpawned                     = ReflectionUtility.HasMethodOverride(type, nameof(NetworkBehaviour.Spawned));
			HasDespawned                   = ReflectionUtility.HasMethodOverride(type, nameof(NetworkBehaviour.Despawned));
			HasFixedUpdateNetwork          = ReflectionUtility.HasMethodOverride(type, nameof(NetworkBehaviour.FixedUpdateNetwork));
			HasRender                      = ReflectionUtility.HasMethodOverride(type, nameof(NetworkBehaviour.Render));

			HasAfterSpawned                = ReflectionUtility.HasInterfaceImplementation(type, typeof(IAfterSpawned));
			HasAfterHostMigration          = ReflectionUtility.HasInterfaceImplementation(type, typeof(IAfterHostMigration));
			HasStateAuthorityChanged       = ReflectionUtility.HasInterfaceImplementation(type, typeof(IStateAuthorityChanged));
			HasInputAuthorityGained        = ReflectionUtility.HasInterfaceImplementation(type, typeof(IInputAuthorityGained));
			HasInputAuthorityLost          = ReflectionUtility.HasInterfaceImplementation(type, typeof(IInputAuthorityLost));
			HasSimulationEnter             = ReflectionUtility.HasInterfaceImplementation(type, typeof(ISimulationEnter));
			HasSimulationExit              = ReflectionUtility.HasInterfaceImplementation(type, typeof(ISimulationExit));
			HasInterestEnter               = ReflectionUtility.HasInterfaceImplementation(type, typeof(IInterestEnter));
			HasInterestExit                = ReflectionUtility.HasInterfaceImplementation(type, typeof(IInterestExit));

			HasBeforeUpdate                = ReflectionUtility.HasInterfaceImplementation(type, typeof(IBeforeUpdate));
			HasBeforeCopyPreviousState     = ReflectionUtility.HasInterfaceImplementation(type, typeof(IBeforeCopyPreviousState));
			HasBeforeClientPredictionReset = ReflectionUtility.HasInterfaceImplementation(type, typeof(IBeforeClientPredictionReset));
			HasAfterClientPredictionReset  = ReflectionUtility.HasInterfaceImplementation(type, typeof(IAfterClientPredictionReset));
			HasBeforeSimulation            = ReflectionUtility.HasInterfaceImplementation(type, typeof(IBeforeSimulation));
			HasBeforeAllTicks              = ReflectionUtility.HasInterfaceImplementation(type, typeof(IBeforeAllTicks));
			HasBeforeTick                  = ReflectionUtility.HasInterfaceImplementation(type, typeof(IBeforeTick));
			HasAfterTick                   = ReflectionUtility.HasInterfaceImplementation(type, typeof(IAfterTick));
			HasAfterAllTicks               = ReflectionUtility.HasInterfaceImplementation(type, typeof(IAfterAllTicks));
			HasAfterRender                 = ReflectionUtility.HasInterfaceImplementation(type, typeof(IAfterRender));
			HasAfterUpdate                 = ReflectionUtility.HasInterfaceImplementation(type, typeof(IAfterUpdate));
		}

		public void AccumulateStateChange(int bitsChanged, bool isSelected)
		{
			_stateTracker.AccumulateChange(bitsChanged);

			if (isSelected == true)
			{
				SelectedStateChanges.Value += bitsChanged;
			}
		}

		public void ProcessAccumulatedStateChanges()
		{
			int totalBits = _stateTracker.ProcessAccumulatedChanges();

			TotalStateChanges.Value += totalBits;
			AverageStateChanges.Value = _stateTracker.BytesPerSecond;
		}

		public void ResetSession()
		{
			TotalStateChanges.Clear();
			SelectedStateChanges.Clear();
			AverageStateChanges.Clear();

			_stateTracker.Clear();
		}
	}
}
