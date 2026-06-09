using UnityEngine;

/// <summary>
/// Attach to any platform. When the player steps on it the platform rises to
/// (startY + riseHeight) at riseSpeed units/second, then holds there.
/// When the player leaves it descends back to startY at the same speed.
///
/// Works with a CharacterController player — the platform moves in Update and
/// the player naturally rides it because they stand on top.
/// </summary>
public class RisingPlatform : MonoBehaviour
{
    [Tooltip("Units per second the platform moves up or down.")]
    [SerializeField] private float riseSpeed  = 3f;

    [Tooltip("How many units above the start position the platform rises before stopping.")]
    [SerializeField] private float riseHeight = 5f;

    // ── state ─────────────────────────────────────────────────────────────────
    private float   startY;
    private float   targetY;
    private bool    playerOnPlatform = false;

    // ── how we detect the player ──────────────────────────────────────────────
    // Works whether the platform has a Collider set to trigger=false (normal
    // collision) or trigger=true.  OnCollisionStay / OnTriggerStay both work.
    private int     contactCount = 0;   // incremented by Enter, decremented by Exit

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        startY  = transform.position.y;
        targetY = startY;
    }

    private void Update()
    {
        // Update target based on whether the player is on the platform.
        targetY = playerOnPlatform ? startY + riseHeight : startY;

        // Smoothly move toward target.
        float newY = Mathf.MoveTowards(transform.position.y, targetY, riseSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // ── Non-trigger (solid) collider ──────────────────────────────────────────

    private void OnCollisionEnter(Collision col)
    {
        if (IsPlayer(col.gameObject)) { contactCount++; RefreshState(); }
    }

    private void OnCollisionExit(Collision col)
    {
        if (IsPlayer(col.gameObject)) { contactCount = Mathf.Max(0, contactCount - 1); RefreshState(); }
    }

    // ── Trigger collider ──────────────────────────────────────────────────────
    // CharacterControllers fire OnTrigger* rather than OnCollision*.

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other.gameObject)) { contactCount++; RefreshState(); }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other.gameObject)) { contactCount = Mathf.Max(0, contactCount - 1); RefreshState(); }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void RefreshState()
    {
        playerOnPlatform = contactCount > 0;
        Debug.Log($"[RisingPlatform] {gameObject.name} — player on: {playerOnPlatform}");
    }

    private static bool IsPlayer(GameObject go)
    {
        return go.CompareTag("Player")
            || go.GetComponent<PlayerDogController>() != null
            || go.name == "Pippaloski";
    }

    // ── Gizmos: show rise range in the Scene view ─────────────────────────────

    private void OnDrawGizmosSelected()
    {
        float y0 = Application.isPlaying ? startY : transform.position.y;
        Vector3 bottom = new Vector3(transform.position.x, y0,               transform.position.z);
        Vector3 top    = new Vector3(transform.position.x, y0 + riseHeight,  transform.position.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(bottom, top);
        Gizmos.DrawWireSphere(top, 0.25f);

        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        // Draw a faint box at the top position to show where the platform ends up
        Gizmos.DrawWireCube(top, Vector3.one * 0.5f);
    }
}
