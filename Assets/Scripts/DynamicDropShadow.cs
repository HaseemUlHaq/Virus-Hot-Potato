using UnityEngine;

// Raycast-based fake drop shadow for MR depth perception.
// Attach to the Virus root. Assign shadowQuad (child Quad with a blurry soft-circle transparent mat).
// The shadow scales down AND fades out as the object rises away from the surface.
// surfaceLayer should contain the table collider layer.
public class DynamicDropShadow : MonoBehaviour
{
    [SerializeField] private Transform shadowQuad;
    [SerializeField] private LayerMask surfaceLayer;
    [SerializeField] private float maxShadowDistance = 2.0f;
    [SerializeField] private float maxShadowSize = 0.25f;
    [SerializeField] private float maxAlpha = 0.6f;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (shadowQuad != null)
            _renderer = shadowQuad.GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (shadowQuad == null || _renderer == null) return;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxShadowDistance, surfaceLayer))
        {
            shadowQuad.gameObject.SetActive(true);
            shadowQuad.position = hit.point + hit.normal * 0.005f;
            shadowQuad.rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90f, 0f, 0f);

            float t = 1f - (hit.distance / maxShadowDistance);
            shadowQuad.localScale = Vector3.one * (t * maxShadowSize);

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorProp, new Color(0f, 0f, 0f, t * maxAlpha));
            _renderer.SetPropertyBlock(_mpb);
        }
        else
        {
            shadowQuad.gameObject.SetActive(false);
        }
    }
}
