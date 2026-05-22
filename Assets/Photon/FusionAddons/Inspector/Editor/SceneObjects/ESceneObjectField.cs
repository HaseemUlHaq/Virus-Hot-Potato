namespace Fusion.Addons.Inspector.Editor
{
	using System;

	[Flags]
	internal enum ESceneObjectField
	{
		None                            = 0,
		Object                          = 1 <<  0,
		IsMasterClientObject            = 1 <<  1,
		AllowStateAuthorityOverride     = 1 <<  2,
		DestroyWhenStateAuthorityLeaves = 1 <<  3,
		InterestMode                    = 1 <<  4,
		IsInSimulation                  = 1 <<  5,
		HasStateAuthority               = 1 <<  6,
		HasInputAuthority               = 1 <<  7,
		NetworkId                       = 1 <<  8,
		StateAuthority                  = 1 <<  9,
		InputAuthority                  = 1 << 10,
		TotalStateChanges               = 1 << 11,
		AverageStateChanges             = 1 << 12,
		StateSize                       = 1 << 13,
		Distance                        = 1 << 14,
		Components                      = 1 << 15,
	}

	internal static class SceneObjectFieldExtensions
	{
		public static bool Has(this ESceneObjectField fields, ESceneObjectField field)
		{
			return (fields & field) == field;
		}
	}
}
