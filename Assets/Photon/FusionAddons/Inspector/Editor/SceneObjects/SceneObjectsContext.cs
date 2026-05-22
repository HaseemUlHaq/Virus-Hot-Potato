namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;

	internal sealed class SceneObjectsContext
	{
		public Components Components;
		public PlayerRef  LocalPlayer;
		public Vector3    DistanceOrigin;
		public bool       HasDistanceOrigin;
		public bool       ProcessStateChanges;
	}
}
