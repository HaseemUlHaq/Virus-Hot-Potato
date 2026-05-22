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
        for (int i = 0; i < shapeVariants.Length; i++)
        {
            if (shapeVariants[i] != null)
                shapeVariants[i].SetActive(i == index);
        }
    }
}
