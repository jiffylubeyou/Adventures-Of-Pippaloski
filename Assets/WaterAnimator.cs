using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterAnimator : MonoBehaviour
{
    [Header("Waves")]
    [Tooltip("How high each peak rises above the rest position")]
    [SerializeField] private float waveHeight = 0.35f;
    [Tooltip("Higher = shorter distance between peaks and troughs. 0.5 makes neighbouring vertices clearly opposite each other")]
    [SerializeField] private float waveFrequency = 0.5f;
    [SerializeField] private float waveSpeed = 0.7f;

    [Header("Second wave layer (cross direction)")]
    [SerializeField] private float wave2Height    = 0.2f;
    [SerializeField] private float wave2Frequency = 0.45f;
    [SerializeField] private float wave2Speed     = 0.5f;

    [Header("Colour shimmer")]
    [SerializeField] private Color waterDeep    = new Color(0.03f, 0.30f, 0.55f, 1f);
    [SerializeField] private Color waterShallow = new Color(0.08f, 0.52f, 0.78f, 1f);
    [SerializeField] private float shimmerSpeed = 0.35f;

    [Header("Grid Resolution")]
    [Tooltip("Vertices per side — higher = more detail but more CPU cost. 80 is a good balance.")]
    [SerializeField] private int gridResolution = 80;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] animatedVertices;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock    = new MaterialPropertyBlock();

        // Replace the sparse Unity plane mesh with a high-resolution grid
        mesh = BuildGridMesh(gridResolution);
        GetComponent<MeshFilter>().mesh = mesh;

        baseVertices     = mesh.vertices;
        animatedVertices = new Vector3[baseVertices.Length];
    }

    private static Mesh BuildGridMesh(int resolution)
    {
        int vCount = resolution + 1;
        var vertices  = new Vector3[vCount * vCount];
        var uvs       = new Vector2[vertices.Length];
        var triangles = new int[resolution * resolution * 6];

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int idx = z * vCount + x;
                // Span -0.5 to 0.5 in local space so the mesh matches Unity's default plane size
                float fx = (x / (float)resolution) - 0.5f;
                float fz = (z / (float)resolution) - 0.5f;
                vertices[idx] = new Vector3(fx * 10f, 0f, fz * 10f);
                uvs[idx]      = new Vector2(fx + 0.5f, fz + 0.5f);
            }
        }

        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int bl = z * vCount + x;
                int br = bl + 1;
                int tl = bl + vCount;
                int tr = tl + 1;
                triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = tr;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = br;
            }
        }

        var m = new Mesh();
        m.name = "Water Grid";
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices  = vertices;
        m.uv        = uvs;
        m.triangles = triangles;
        m.RecalculateNormals();
        return m;
    }

    private void Update()
    {
        float t = Time.time;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            var v = baseVertices[i];

            // Wave 1 travels diagonally so both X and Z neighbours differ in height
            float w1 = Mathf.Sin((v.x + v.z) * waveFrequency + t * waveSpeed) * waveHeight;

            // Wave 2 travels the opposite diagonal — creates the grid checkerboard effect
            // where some points are up while their neighbours are down
            float w2 = Mathf.Sin((v.x - v.z) * wave2Frequency + t * wave2Speed) * wave2Height;

            animatedVertices[i] = new Vector3(v.x, v.y + w1 + w2, v.z);
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
