namespace Fusion.Addons.Inspector.Editor
{
	using System;

	[Flags]
	internal enum EPrefabField
	{
		None                            = 0,
		Object                          = 1 << 0,
		IsSpawnable                     = 1 << 1,
		IsMasterClientObject            = 1 << 2,
		AllowStateAuthorityOverride     = 1 << 3,
		DestroyWhenStateAuthorityLeaves = 1 << 4,
		InterestMode                    = 1 << 5,
		StateSize                       = 1 << 6,
		Components                      = 1 << 7,
	}

	internal static class PrefabFieldExtensions
	{
		public static bool Has(this EPrefabField fields, EPrefabField field)
		{
			return (fields & field) == field;
		}
	}
}
