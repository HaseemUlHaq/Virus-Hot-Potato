using UnityEngine;

// Visual-only virus in the example formation. No networking, no grabbing — just applies material, shape, scale, and pulsation locally.
public class LockedVirusDisplay : MonoBehaviour
{
    [SerializeField] private VirusSwipeCycler materialCycler;
    [SerializeField] private VirusShapeCycler shapeCycler;

    public void ApplyConfig(int materialIndex, float scale, bool isPulsating, int shapeVariantIndex)
    {
        transform.localScale = Vector3.one * scale;

        if (materialCycler != null)
        {
            materialCycler.SetMaterialIndex(materialIndex);
            materialCycler.SetStandalonePulsating(isPulsating);
        }

        if (shapeCycler != null)
            shapeCycler.SetShapeIndex(shapeVariantIndex);
    }


}
