using System.Collections;
using UnityEngine;

/// <summary>
/// AI for enemy pirate ships.
///
/// - Floats on the water surface (with a gentle bob and roll) and never
///   leaves it — land is avoided with the same box-probe trick the raft uses.
/// - Patrols between random open-water waypoints around its spawn point.
/// - When Pippaloski is aboard the raft and inside aggro range, the ship
///   closes in, turns broadside, and fires cannonball volleys from whichever
///   side faces the raft.
///
/// Setup: run Tools > Pippaloski > Setup Pirate Ships (adds this component,
/// a hull collider, and spawns ships in open water).
/// </summary>
public class PirateShipAI : MonoBehaviour
{
    [Header("Water")]
    [Tooltip("World Y of the water surface. Match the raft's water level.")]
    public float waterLevel = 4f;
    [Tooltip("Pivot offset relative to the waterline so the hull sits in the water correctly.")]
    public float hullYOffset = 0f;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobFrequency = 0.5f;
    [SerializeField] private float rollAmplitude = 1.5f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 8f;
    [SerializeField] private float attackSpeed = 22f;
    [SerializeField] private float turnSpeed   = 35f;  // degrees per second

    [Header("Patrol")]
    [SerializeField] private float patrolRadius      = 90f;
    [SerializeField] private float waypointTolerance = 10f;
    [SerializeField] private float waypointTimeout   = 18f;

    [Header("Land Avoidance")]
    [Tooltip("Layer mask containing the islands / terrain.")]
    public LayerMask groundLayer = 1 << 8; // Island layer
    [SerializeField] private float probeDistance = 6f;

    [Header("Combat")]
    [SerializeField] private float aggroRange        = 130f;
    [SerializeField] private float fireRange         = 80f;
    [SerializeField] private float broadsideDistance = 45f;
    [SerializeField] private float fireCooldown      = 4.5f;
    [SerializeField] private int   volleySize        = 2;
    [SerializeField] private float volleyInterval    = 0.3f;
    [SerializeField] private float ballDamage        = 15f;
    [SerializeField] private float aimInaccuracy     = 5f;

    private float yaw;
    private float bobPhase;
    private Vector3 home;
    private Vector3 waypoint;
    private float waypointSetTime;
    private float nextFireTime;

    // Horizontal half extents of the hull, used for land probes and fire points
    private Vector3 hullHalf = new Vector3(4f, 5f, 10f);
    private float deckHeight = 4f;

    private RaftController raftCtrl;
    private RaftHealth raftHealth;
    private Vector3 lastRaftPos;
    private Vector3 raftVelocity;

    private void Awake()
    {
        yaw = transform.eulerAngles.y;
        bobPhase = Random.value * 100f;

        home = transform.position;
        home.y = waterLevel;

        MeasureHull();
        PickNewWaypoint();
    }

    private void Start()
    {
        raftCtrl = RaftController.Instance;
        if (raftCtrl == null)
            raftCtrl = FindFirstObjectByType<RaftController>();
        if (raftCtrl != null)
        {
            raftHealth  = raftCtrl.GetComponent<RaftHealth>();
            lastRaftPos = raftCtrl.transform.position;
        }
    }

    private void Update()
    {
        TrackRaftVelocity();

        bool aggro = IsAggro(out Vector3 toRaftFlat, out float raftDist);

        Vector3 steerTarget;
        float speed;

        if (aggro)
        {
            Vector3 raftPos = raftCtrl.transform.position;

            if (raftDist > fireRange * 0.95f)
            {
                // Close in toward a point broadsideDistance short of the raft
                steerTarget = raftPos - toRaftFlat * broadsideDistance;
                speed = attackSpeed;
            }
            else
            {
                // In range: circle the raft so a side stays toward it
                Vector3 tangent = Vector3.Cross(Vector3.up, toRaftFlat);
                if (Vector3.Dot(tangent, ForwardFlat()) < 0f) tangent = -tangent;

                // Drift in/out to hold the broadside ring
                Vector3 radial = toRaftFlat * Mathf.Clamp(raftDist - broadsideDistance, -1f, 1f);
                steerTarget = transform.position + (tangent + radial * 0.5f).normalized * 30f;
                speed = attackSpeed * 0.5f;
            }

            TryFire(toRaftFlat, raftDist);
        }
        else
        {
            if (FlatDistance(transform.position, waypoint) < waypointTolerance ||
                Time.time - waypointSetTime > waypointTimeout)
                PickNewWaypoint();

            steerTarget = waypoint;
            speed = patrolSpeed;
        }

        SteerAndMove(steerTarget, speed);
        ApplyFloating();
    }

    // ---------- helpers ----------

    private Vector3 ForwardFlat()
    {
        return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ---------- perception ----------

    private bool IsAggro(out Vector3 toRaftFlat, out float raftDist)
    {
        toRaftFlat = Vector3.forward;
        raftDist = float.MaxValue;

        if (raftCtrl == null || !raftCtrl.PlayerAboard) return false;
        if (raftHealth != null && raftHealth.IsSinking) return false;

        Vector3 delta = raftCtrl.transform.position - transform.position;
        delta.y = 0f;
        raftDist = delta.magnitude;
        if (raftDist < 0.01f || raftDist > aggroRange) return false;

        toRaftFlat = delta / raftDist;
        return true;
    }

    private void TrackRaftVelocity()
    {
        if (raftCtrl == null) return;
        Vector3 pos = raftCtrl.transform.position;
        if (Time.deltaTime > 0f)
        {
            Vector3 instant = (pos - lastRaftPos) / Time.deltaTime;
            raftVelocity = Vector3.Lerp(raftVelocity, instant, 5f * Time.deltaTime);
        }
        lastRaftPos = pos;
    }

    // ---------- movement ----------

    private void SteerAndMove(Vector3 target, float speed)
    {
        Vector3 desired = target - transform.position;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.01f) return;

        float desiredYaw = Mathf.Atan2(desired.x, desired.z) * Mathf.Rad2Deg;

        // Find the clear heading closest to where we want to go
        float chosenYaw = desiredYaw;
        bool foundClear = false;
        float[] offsets = { 0f, 25f, -25f, 50f, -50f, 80f, -80f, 115f, -115f, 150f, -150f, 180f };
        foreach (float offset in offsets)
        {
            if (IsHeadingClear(desiredYaw + offset))
            {
                chosenYaw = desiredYaw + offset;
                foundClear = true;
                break;
            }
        }

        yaw = Mathf.MoveTowardsAngle(yaw, chosenYaw, turnSpeed * Time.deltaTime);

        // Only advance while the bow is actually clear — otherwise turn in place
        Vector3 forward = ForwardFlat();
        if (foundClear && IsHeadingClear(yaw))
            transform.position += forward * speed * Time.deltaTime;
    }

    private bool IsHeadingClear(float headingYaw)
    {
        Quaternion rot = Quaternion.Euler(0f, headingYaw, 0f);
        Vector3 dir = rot * Vector3.forward;
        Vector3 boxCenter = transform.position + dir * (hullHalf.z + probeDistance);
        boxCenter.y = waterLevel;

        // Tall box so islands are caught no matter their height
        Vector3 half = new Vector3(hullHalf.x * 1.2f, 15f, hullHalf.z * 0.6f + probeDistance * 0.5f);

        return !Physics.CheckBox(boxCenter, half, rot, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void ApplyFloating()
    {
        float t = Time.time * bobFrequency * Mathf.PI * 2f + bobPhase;
        float bob   = Mathf.Sin(t) * bobAmplitude;
        float roll  = Mathf.Sin(t * 0.8f) * rollAmplitude;
        float pitch = Mathf.Sin(t * 0.6f + 1.3f) * rollAmplitude * 0.5f;

        Vector3 pos = transform.position;
        pos.y = waterLevel + hullYOffset + bob;
        transform.position = pos;
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }

    private void PickNewWaypoint()
    {
        waypointSetTime = Time.time;

        // Sample random points around home, keep the first one in open water
        for (int i = 0; i < 12; i++)
        {
            Vector2 circle = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = home + new Vector3(circle.x, 0f, circle.y);

            bool onLand = Physics.CheckBox(
                new Vector3(candidate.x, waterLevel, candidate.z),
                new Vector3(hullHalf.x * 1.5f, 15f, hullHalf.x * 1.5f),
                Quaternion.identity, groundLayer, QueryTriggerInteraction.Ignore);

            if (!onLand)
            {
                waypoint = candidate;
                return;
            }
        }

        waypoint = home; // everything sampled was land — head back home
    }

    // ---------- combat ----------

    private void TryFire(Vector3 toRaftFlat, float raftDist)
    {
        if (Time.time < nextFireTime || raftDist > fireRange) return;

        // Only fire when the target is roughly abeam (off the side)
        Vector3 rightFlat = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        float side = Vector3.Dot(rightFlat, toRaftFlat);
        if (Mathf.Abs(side) < 0.55f) return;

        nextFireTime = Time.time + fireCooldown * Random.Range(0.85f, 1.25f);
        StartCoroutine(FireVolley(side > 0f));
    }

    private IEnumerator FireVolley(bool starboard)
    {
        for (int i = 0; i < volleySize; i++)
        {
            FireOne(starboard, i);
            yield return new WaitForSeconds(volleyInterval);
        }
    }

    private void FireOne(bool starboard, int index)
    {
        if (raftCtrl == null) return;

        Quaternion shipRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 forward = shipRot * Vector3.forward;
        Vector3 sideDir = shipRot * (starboard ? Vector3.right : Vector3.left);

        // Stagger fire points along the deck
        float alongDeck = Mathf.Lerp(-0.35f, 0.35f, volleySize <= 1 ? 0.5f : (float)index / (volleySize - 1));
        Vector3 muzzle = transform.position
                       + forward * (alongDeck * hullHalf.z * 2f)
                       + sideDir * (hullHalf.x + 1f)
                       + Vector3.up * deckHeight;

        // Lead the raft, then add some scatter so it's dodgeable
        float flightTime = Mathf.Clamp(Vector3.Distance(muzzle, raftCtrl.transform.position) / 35f, 1f, 2.2f);
        Vector2 scatter = Random.insideUnitCircle * aimInaccuracy;
        Vector3 target = raftCtrl.transform.position
                       + raftVelocity * flightTime * 0.8f
                       + new Vector3(scatter.x, 0f, scatter.y);
        target.y = waterLevel;

        CannonBall.Launch(muzzle, target, flightTime, ballDamage, transform);
    }

    // ---------- hull measurement ----------

    private void MeasureHull()
    {
        // Prefer the hull BoxCollider added by the setup tool
        var box = GetComponent<BoxCollider>();
        Bounds bounds;
        if (box != null)
        {
            bounds = box.bounds;
        }
        else
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        }

        hullHalf = bounds.extents;
        deckHeight = bounds.max.y - transform.position.y + 0.5f;
    }

    // ---------- debug ----------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, fireRange);
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        Gizmos.DrawLine(transform.position, Application.isPlaying ? waypoint : transform.position);
    }
}
