using UnityEngine;

// Enables one shape child GameObject at a time, hides the rest. Driven by ShapeVariantIndex on NetworkGrabbableVirus.
public class VirusShapeCycler : MonoBehaviour
{
    [SerializeField] private GameObject[] shapeVariants;

    public int ShapeCount => shapeVariants != null ? shapeVariants.Length : 0;

    private void Awake()
    {
        SetShapeIndex(0);
    }

    public void SetShapeIndex(int index)
    {
        if (shapeVariants == null) return;
        if (index >= shapeVariants.Length)
        {
            Debug.LogWarning($"[VirusShapeCycler] Index {index} is out of range — shapeVariants has {shapeVariants.Length} entries. Add the new shape to the array in the Inspector.", this);
            return;
        }
        for (int i = 0; i < shapeVariants.Length; i++)
        {
            if (shapeVariants[i] != null)
                shapeVariants[i].SetActive(i == index);
        }
    }
}
