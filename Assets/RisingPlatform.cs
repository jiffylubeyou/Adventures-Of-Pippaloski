using UnityEngine;

/// <summary>
/// Attach to any platform. When the player steps on it the platform rises to
/// (startY + riseHeight) at riseSpeed units/second, then holds there.
/// When the player leaves it descends back to startY at the same speed.
///
/// Uses Physics.OverlapBox each frame to sense the player — this works with
/// CharacterController players that don't generate OnCollision events on the
/// objects they stand on.
/// </summary>
public class RisingPlatform : MonoBehaviour
{
    [Tooltip("Units per second the platform moves up or down.")]
    [SerializeField] private float riseSpeed  = 3f;

    [Tooltip("How many units above the start position the platform rises before stopping.")]
    [SerializeField] private float riseHeight = 5f;

    [Tooltip("How far above the platform surface to check for the player. " +
             "Increase if the player is tall or the platform is thin.")]
    [SerializeField] private float sensorHeight = 1.5f;

    [Tooltip("Sensor width (X axis). 0 = auto-fit from mesh bounds.")]
    [SerializeField] private float sensorSizeX = 0f;

    [Tooltip("Sensor depth (Z axis). 0 = auto-fit from mesh bounds.")]
    [SerializeField] private float sensorSizeZ = 0f;

    // ── state ─────────────────────────────────────────────────────────────────
    private float startY;
    private bool  playerOnPlatform;

    // cached so we don't allocate every frame
    private Collider[] sensorResults = new Collider[8];
    private Transform  playerTransform;
    private Vector3    autoHalfExtents;   // computed once in Start

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        startY = transform.position.y;

        // Find the player once
        var playerObj = GameObject.FindGameObjectWithTag("Player")
                     ?? GameObject.Find("Pippaloski")
                     ?? GameObject.Find("Player Border Collie");
        if (playerObj != null) playerTransform = playerObj.transform;

        // Auto-compute sensor half-extents from the mesh/collider bounds
        var col = GetComponent<Collider>();
        var ren = GetComponentInChildren<Renderer>();
        Vector3 size;
        if (col != null)
            size = col.bounds.size;
        else if (ren != null)
            size = ren.bounds.size;
        else
            size = Vector3.one;

        autoHalfExtents = new Vector3(size.x * 0.45f, sensorHeight * 0.5f, size.z * 0.45f);
    }

    private void Update()
    {
        DetectPlayer();

        float targetY = playerOnPlatform ? startY + riseHeight : startY;
        float newY    = Mathf.MoveTowards(transform.position.y, targetY, riseSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void DetectPlayer()
    {
        if (playerTransform == null) { playerOnPlatform = false; return; }

        // Centre the sensor box just above the platform's top surface
        Collider col    = GetComponent<Collider>();
        float    topY   = col != null ? col.bounds.max.y : transform.position.y + 0.1f;
        Vector3  centre = new Vector3(transform.position.x,
                                      topY + sensorHeight * 0.5f,
                                      transform.position.z);

        float hx = sensorSizeX > 0f ? sensorSizeX : autoHalfExtents.x;
        float hz = sensorSizeZ > 0f ? sensorSizeZ : autoHalfExtents.z;
        Vector3 half = new Vector3(hx, sensorHeight * 0.5f, hz);

        int hits = Physics.OverlapBoxNonAlloc(centre, half, sensorResults,
                                              transform.rotation, ~0,
                                              QueryTriggerInteraction.Ignore);
        playerOnPlatform = false;
        for (int i = 0; i < hits; i++)
        {
            if (IsPlayer(sensorResults[i].gameObject))
            {
                playerOnPlatform = true;
                break;
            }
        }
    }

    private static bool IsPlayer(GameObject go)
    {
        return go.CompareTag("Player")
            || go.GetComponent<PlayerDogController>() != null
            || go.name == "Pippaloski";
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        float y0 = Application.isPlaying ? startY : transform.position.y;

        // Rise range line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(transform.position.x, y0, transform.position.z),
                        new Vector3(transform.position.x, y0 + riseHeight, transform.position.z));
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, y0 + riseHeight, transform.position.z), 0.2f);

        // Sensor box
        Collider col  = GetComponent<Collider>();
        float    topY = col != null ? col.bounds.max.y : transform.position.y + 0.1f;
        Vector3  ctr  = new Vector3(transform.position.x, topY + sensorHeight * 0.5f, transform.position.z);

        Vector3 size;
        if (sensorSizeX > 0f || sensorSizeZ > 0f)
        {
            var ren2 = GetComponentInChildren<Renderer>();
            Vector3 b2 = col != null ? col.bounds.size : (ren2 != null ? ren2.bounds.size : Vector3.one);
            float gx = sensorSizeX > 0f ? sensorSizeX * 2f : b2.x * 0.9f;
            float gz = sensorSizeZ > 0f ? sensorSizeZ * 2f : b2.z * 0.9f;
            size = new Vector3(gx, sensorHeight, gz);
        }
        else
        {
            var ren = GetComponentInChildren<Renderer>();
            Vector3 b = col != null ? col.bounds.size : (ren != null ? ren.bounds.size : Vector3.one);
            size = new Vector3(b.x * 0.9f, sensorHeight, b.z * 0.9f);
        }

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireCube(ctr, size);
    }
}
