using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server simulates the ball. Clients only receive its position through NetworkTransform.
/// Prefab needs: NetworkObject, NetworkTransform, Rigidbody2D (Dynamic, Gravity 0, Freeze Rotation,
/// Continuous collision), CircleCollider2D (Physics Material: Bounciness 1, Friction 0).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Ball : NetworkBehaviour
{
    [Header("Speed")]
    [SerializeField] private float startSpeed = 7f;
    [SerializeField] private float speedGainPerHit = 0.3f;
    [SerializeField] private float maxSpeed = 15f;

    [Header("Serve / Bounce")]
    [SerializeField] private float attachOffset = 0.6f;      // distance in front of paddle while stuck
    [SerializeField] private float maxServeAngle = 45f;      // degrees, at full W/S input
    [SerializeField] private float maxBounceSlope = 0.75f;   // how steep a paddle-edge hit can get

    private Rigidbody2D rb;
    private Transform holder;   // paddle the ball is stuck to
    private float dirX = 1f;    // direction the held ball will be shot (+1 = right)
    private float speed;
    private bool attached;
    private bool live;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        // Clients don't simulate physics, they just follow NetworkTransform
        if (!IsServer) rb.simulated = false;
    }

    // ---------------------------------------------------------------- Server API

    /// <summary>Stick the ball to a paddle (start of match / after a goal).</summary>
    public void AttachTo(Transform paddle)
    {
        if (!IsServer) return;

        holder = paddle;
        dirX = paddle.position.x < 0f ? 1f : -1f; // host paddle is on the left -> shoots right
        speed = startSpeed;
        attached = true;
        live = false;
        rb.linearVelocity = Vector2.zero;
        SnapToHolder();
    }

    /// <param name="aim">-1..1 (paddle move input at the moment of release). + = up.</param>
    public void Launch(float aim)
    {
        if (!IsServer || !attached) return;

        attached = false;
        live = true;

        float rad = Mathf.Clamp(aim, -1f, 1f) * maxServeAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(dirX * Mathf.Cos(rad), Mathf.Sin(rad));
        rb.linearVelocity = dir * speed;
    }

    /// <summary>Freeze the ball (game over / player left).</summary>
    public void Stop()
    {
        if (!IsServer) return;
        attached = false;
        live = false;
        holder = null;
        rb.linearVelocity = Vector2.zero;
    }

    // ---------------------------------------------------------------- Simulation

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (attached && holder != null)
        {
            SnapToHolder();
            return;
        }

        if (!live) return;

        // Keep a constant speed (wall bounces otherwise drift) and avoid a near-vertical loop
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.0001f) return;

        float minX = speed * 0.25f;
        if (Mathf.Abs(v.x) < minX)
            v.x = minX * (v.x >= 0f ? 1f : -1f);

        rb.linearVelocity = v.normalized * speed;
    }

    private void SnapToHolder()
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 target = (Vector2)holder.position + Vector2.right * dirX * attachOffset;
        rb.position = target;
        transform.position = target;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer || !live) return;

        if (collision.collider.TryGetComponent(out PlayerController player))
        {
            Bounds b = collision.collider.bounds;

            // Where on the paddle did we hit? -1 (bottom) .. 1 (top)
            float offset = Mathf.Clamp((rb.position.y - b.center.y) / b.extents.y, -1f, 1f);
            float side = Mathf.Sign(rb.position.x - b.center.x);

            speed = Mathf.Min(speed + speedGainPerHit, maxSpeed);
            Vector2 dir = new Vector2(side, offset * maxBounceSlope).normalized;
            rb.linearVelocity = dir * speed;

            player.ServerAddMana(1f); // GDD: +1 mana per successful hit
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || !live) return;

        // Tag the two goal trigger colliders "GoalLeft" (host side) and "GoalRight"
        if (other.CompareTag("GoalLeft")) Scored(true);
        else if (other.CompareTag("GoalRight")) Scored(false);
    }

    private void Scored(bool leftSide)
    {
        live = false; // prevents double-trigger before GameManager re-attaches the ball
        GameManager.Instance.OnGoal(leftSide);
    }
}