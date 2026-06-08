using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterAnimator : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField] private float waveHeight    = 0.18f;
    [SerializeField] private float waveFrequency = 0.08f;  // how tightly packed the waves are
    [SerializeField] private float waveSpeed     = 0.6f;   // how fast waves move

    [Header("Second wave layer (cross-ripple)")]
    [SerializeField] private float wave2Height    = 0.09f;
    [SerializeField] private float wave2Frequency = 0.12f;
    [SerializeField] private float wave2Speed     = 0.4f;
    [SerializeField] private float wave2Angle     = 45f;   // degrees offset from first wave

    [Header("Colour shimmer")]
    [SerializeField] private Color waterDeep     = new Color(0.03f, 0.30f, 0.55f, 1f);
    [SerializeField] private Color waterShallow  = new Color(0.08f, 0.52f, 0.78f, 1f);
    [SerializeField] private float shimmerSpeed  = 0.35f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] animatedVertices;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices     = mesh.vertices;
        animatedVertices = new Vector3[baseVertices.Length];
        meshRenderer     = GetComponent<MeshRenderer>();
        propBlock        = new MaterialPropertyBlock();
    }

    private void Update()
    {
        float t = Time.time;
        float rad2 = wave2Angle * Mathf.Deg2Rad;
        float cos2 = Mathf.Cos(rad2);
        float sin2 = Mathf.Sin(rad2);

        for (int i = 0; i < baseVertices.Length; i++)
        {
            var v = baseVertices[i];

            // Wave 1 — travels along X axis
            float wave1 = Mathf.Sin(v.x * waveFrequency + t * waveSpeed) * waveHeight;

            // Wave 2 — travels at an angle for cross-ripple feel
            float dot2  = v.x * cos2 + v.z * sin2;
            float wave2 = Mathf.Sin(dot2 * wave2Frequency + t * wave2Speed) * wave2Height;

            animatedVertices[i] = new Vector3(v.x, v.y + wave1 + wave2, v.z);
        }

        mesh.vertices = animatedVertices;
        mesh.RecalculateNormals();

        // Shimmer between two colours based on time
        float shimmer = (Mathf.Sin(t * shimmerSpeed) + 1f) * 0.5f;
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", Color.Lerp(waterDeep, waterShallow, shimmer));
        propBlock.SetColor("_Color",     Color.Lerp(waterDeep, waterShallow, shimmer));
        meshRenderer.SetPropertyBlock(propBlock);
    }
}
