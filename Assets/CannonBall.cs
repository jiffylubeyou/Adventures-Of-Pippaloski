using UnityEngine;

/// <summary>
/// A ballistic cannonball fired by pirate ships (and later, player boats).
/// Created entirely from code via Launch() — no prefab required.
/// Damages anything in the hit hierarchy that has a RaftHealth.
/// </summary>
public class CannonBall : MonoBehaviour
{
    private float damage;

    /// <param name="from">Muzzle position.</param>
    /// <param name="target">World point the ball should land on.</param>
    /// <param name="flightTime">Seconds the arc should take to arrive.</param>
    /// <param name="damage">Damage dealt to a RaftHealth on impact.</param>
    /// <param name="owner">Firing ship — its colliders are ignored so the ball clears the deck.</param>
    public static CannonBall Launch(Vector3 from, Vector3 target, float flightTime,
        float damage, Transform owner)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "CannonBall";
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.7f;

        var renderer = go.GetComponent<Renderer>();
        renderer.material.color = new Color(0.12f, 0.12f, 0.13f);
        if (renderer.material.HasProperty("_BaseColor"))
            renderer.material.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.13f));

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var myCollider = go.GetComponent<Collider>();
        if (owner != null)
        {
            foreach (var col in owner.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(myCollider, col);
        }

        // Solve the initial velocity for a gravity arc that lands on the
        // target after flightTime: p = p0 + v0*t + g*t²/2
        Vector3 delta = target - from;
        rb.linearVelocity = (delta - 0.5f * Physics.gravity * flightTime * flightTime) / flightTime;

        var ball = go.AddComponent<CannonBall>();
        ball.damage = damage;

        Destroy(go, 12f); // safety net if it never hits anything
        return ball;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var raftHealth = collision.collider.GetComponentInParent<RaftHealth>();
        if (raftHealth != null)
            raftHealth.ApplyDamage(damage);

        Destroy(gameObject);
    }
}
