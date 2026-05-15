namespace Fusion.Addons.Inspector.Editor
{
	using System;

	[Flags]
	internal enum EComponentField
	{
		None                           = 0,
		Type                           = 1 <<  0,

		HasSpawned                     = 1 <<  1,
		HasDespawned                   = 1 <<  2,
		HasFixedUpdateNetwork          = 1 <<  3,
		HasRender                      = 1 <<  4,

		HasAfterSpawned                = 1 <<  5,
		HasAfterHostMigration          = 1 <<  6,
		HasStateAuthorityChanged       = 1 <<  7,
		HasInputAuthorityGained        = 1 <<  8,
		HasInputAuthorityLost          = 1 <<  9,
		HasSimulationEnter             = 1 << 10,
		HasSimulationExit              = 1 << 11,
		HasInterestEnter               = 1 << 12,
		HasInterestExit                = 1 << 13,

		HasBeforeUpdate                = 1 << 14,
		HasBeforeCopyPreviousState     = 1 << 15,
		HasBeforeClientPredictionReset = 1 << 16,
		HasAfterClientPredictionReset  = 1 << 17,
		HasBeforeSimulation            = 1 << 18,
		HasBeforeAllTicks              = 1 << 19,
		HasBeforeTick                  = 1 << 20,
		HasAfterTick                   = 1 << 21,
		HasAfterAllTicks               = 1 << 22,
		HasAfterRender                 = 1 << 23,
		HasAfterUpdate                 = 1 << 24,

		SceneObjects                   = 1 << 25,
		Prefabs                        = 1 << 26,
		RPCs                           = 1 << 27,
		StateSize                      = 1 << 28,
		ExecutionOrder                 = 1 << 29,
		TotalStateChanges              = 1 << 30,
		AverageStateChanges            = 1 << 31,
	}

	internal static class ComponentFieldExtensions
	{
		public static bool Has(this EComponentField fields, EComponentField field)
		{
			return (fields & field) == field;
		}
	}
}
