using UnityEngine;

// Visual-only virus in the example formation. No networking, no grabbing — just applies material, shape, scale, and pulsation locally.
public class LockedVirusDisplay : MonoBehaviour
{
    [SerializeField] private VirusSwipeCycler materialCycler;
    [SerializeField] private VirusShapeCycler shapeCycler;

    private float _baseScale = 1f;
    private bool _isPulsating;
    private float _pulsateTime;

    private const float PULSATE_SPEED = 2f;
    private const float PULSATE_AMOUNT = 0.2f;

    public void ApplyConfig(int materialIndex, float scale, bool isPulsating, int shapeVariantIndex)
    {
        _baseScale = scale;
        _isPulsating = isPulsating;
        _pulsateTime = 0f;
        transform.localScale = Vector3.one * scale;

        if (materialCycler != null)
        {
            materialCycler.SetMaterialIndex(materialIndex);
            materialCycler.SetStandalonePulsating(isPulsating);
        }

        if (shapeCycler != null)
            shapeCycler.SetShapeIndex(shapeVariantIndex);
    }

    private void Update()
    {
        if (!_isPulsating) return;
        _pulsateTime += Time.deltaTime * PULSATE_SPEED;
        transform.localScale = Vector3.one * _baseScale * (1f + Mathf.Sin(_pulsateTime) * PULSATE_AMOUNT);
    }
}
