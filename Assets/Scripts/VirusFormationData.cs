using UnityEngine;

// Stores the template for one formation — what each slot should look like (material, shape, scale, pulsation, position).
[CreateAssetMenu(fileName = "VirusFormationData", menuName = "Virus Hot Potato/Formation Data")]
public class VirusFormationData : ScriptableObject
{
    [System.Serializable]
    public class SlotConfig
    {
        [Range(0, 9)] public int materialIndex;
        [Range(0.05f, 3.0f)] public float scale = 1f;
        public bool isPulsating;
        public int shapeVariantIndex;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        [Tooltip("Fallback links when PlaceholderFormation.slotConnections is empty. Other slot indices (0-based). Line when both are correct.")]
        public int[] connectionIndices = System.Array.Empty<int>();
    }

    public SlotConfig[] slots = System.Array.Empty<SlotConfig>();
}
