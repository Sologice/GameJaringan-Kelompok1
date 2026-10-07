using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Server simulates the ball. Clients only receive its position through NetworkTransform.
/// Prefab needs: NetworkObject, NetworkTransform (server authoritative), Rigidbody2D (Dynamic, Gravity 0,
/// Freeze Rotation, Continuous collision), CircleCollider2D (Physics Material: Bounciness 1, Friction 0),
/// SpriteRenderer (optionally a TrailRenderer).
///
/// Card support: IBallModifier list (Curveball, Heavy, Invisibility, Fireball), Magnet Shield catch,
/// decoy mode (Multi-Ball) and portal teleporting.
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

    // Magnet Shield
    private float caughtTimer;

    // Decoy (Multi-Ball)
    private float decoyLife;

    // Card modifiers (server only)
    private readonly List<IBallModifier> modifiers = new();

    // Visuals synced to everybody
    private readonly NetworkVariable<Color> tint = new(Color.white);
    private readonly NetworkVariable<long> hiddenFrom = new(-1L); // client id that can't see the ball, -1 = nobody
    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private TrailRenderer[] trails;

    // ---------------------------------------------------------------- Public info for cards / GameManager

    public Collider2D Col { get; private set; }
    public bool IsDecoy { get; private set; }
    public bool IsLive => live;
    public PlayerController CaughtBy { get; private set; }
    public Vector2 Position => rb.position;

    /// <summary>Multiplier from Fireball. Reset to 1 when the modifier ends.</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>Time.time after which this ball may use a portal again.</summary>
    public float PortalReadyTime { get; set; }

    public float EffectiveSpeed => Mathf.Min(speed * SpeedMultiplier, CardBalance.FireballSpeedCap);

    public Vector2 Heading => rb.linearVelocity.sqrMagnitude > 0.01f
        ? rb.linearVelocity.normalized
        : new Vector2(dirX, 0f);

    /// <summary>True if the ball moves toward the side with this x sign (-1 left, +1 right).</summary>
    public bool MovingToward(float sideSign) => rb.linearVelocity.x * sideSign > 0f;

    public bool IsHeldBy(PlayerController player) => attached && CaughtBy != null && CaughtBy == player;

    // ---------------------------------------------------------------- Unity / Netcode

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Col = GetComponent<Collider2D>();

        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        // Clients don't simulate physics, they just follow NetworkTransform
        if (!IsServer) rb.simulated = false;

        tint.OnValueChanged += OnTintChanged;
        hiddenFrom.OnValueChanged += OnHiddenChanged;
        ApplyVisuals();
    }

    public override void OnNetworkDespawn()
    {
        tint.OnValueChanged -= OnTintChanged;
        hiddenFrom.OnValueChanged -= OnHiddenChanged;

        if (IsServer && IsDecoy && GameManager.Instance != null)
            GameManager.Instance.UnregisterDecoy(this);
    }

    private void OnTintChanged(Color previous, Color current) => ApplyVisuals();
    private void OnHiddenChanged(long previous, long current) => ApplyVisuals();

    private void ApplyVisuals()
    {
        bool hidden = hiddenFrom.Value >= 0
                      && NetworkManager != null
                      && (ulong)hiddenFrom.Value == NetworkManager.LocalClientId;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = !hidden;
            renderers[i].color = baseColors[i] * tint.Value;
        }

        foreach (var trail in trails) trail.enabled = !hidden;
    }

    // ---------------------------------------------------------------- Server API

    /// <summary>Stick the ball to a paddle (start of match / after a goal).</summary>
    public void AttachTo(Transform paddle)
    {
        if (!IsServer) return;

        ClearModifiers();

        holder = paddle;
        CaughtBy = null;
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
        CaughtBy = null;

        float rad = Mathf.Clamp(aim, -1f, 1f) * maxServeAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(dirX * Mathf.Cos(rad), Mathf.Sin(rad));
        rb.linearVelocity = dir * EffectiveSpeed;
    }

    /// <summary>Freeze the ball (game over / player left).</summary>
    public void Stop()
    {
        if (!IsServer) return;

        ClearModifiers();
        attached = false;
        live = false;
        holder = null;
        CaughtBy = null;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Turns this ball into a Multi-Ball decoy: bounces around, never scores, vanishes after lifetime.</summary>
    public void InitAsDecoy(Vector2 direction, float decoySpeed, float lifetime, Color color)
    {
        if (!IsServer) return;

        IsDecoy = true;
        speed = decoySpeed;
        decoyLife = lifetime;
        attached = false;
        live = true;
        tint.Value = color;
        rb.linearVelocity = direction.normalized * speed;
    }

    // ---------------------------------------------------------------- Card hooks

    public void AddModifier(IBallModifier modifier)
    {
        if (!IsServer || IsDecoy) return;

        modifiers.Add(modifier);
        modifier.OnAttach(this);
        RefreshTint();
    }

    public bool HasModifier<T>() where T : IBallModifier
    {
        foreach (var m in modifiers)
            if (m is T) return true;
        return false;
    }

    public void ClearModifiers()
    {
        if (!IsServer) return;

        for (int i = modifiers.Count - 1; i >= 0; i--)
            modifiers[i].OnRemoved(this);
        modifiers.Clear();

        SpeedMultiplier = 1f;
        RefreshTint();
    }

    private void RemoveExpiredModifiers()
    {
        bool changed = false;
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            if (!modifiers[i].Expired) continue;
            modifiers[i].OnRemoved(this);
            modifiers.RemoveAt(i);
            changed = true;
        }

        if (changed) RefreshTint();
    }

    private void RefreshTint()
    {
        Color c = Color.white;
        foreach (var m in modifiers)
            if (m.Tint != Color.white) c = m.Tint;
        tint.Value = c;
    }

    public void RotateVelocity(float degrees)
    {
        rb.linearVelocity = (Vector2)(Quaternion.Euler(0f, 0f, degrees) * (Vector3)rb.linearVelocity);
    }

    /// <summary>Hide the ball from one client (Invisibility Ball). -1 = visible to everyone.</summary>
    public void SetHiddenFrom(long clientId)
    {
        if (!IsServer) return;
        hiddenFrom.Value = clientId;
    }

    /// <summary>Portal Trap. Velocity is kept, so the ball leaves the other portal at the same angle.</summary>
    public void Teleport(Vector2 position)
    {
        if (!IsServer) return;

        rb.position = position;
        transform.position = position;

        if (TryGetComponent(out NetworkTransform nt))
            nt.Teleport(position, transform.rotation, transform.localScale);
    }

    /// <summary>Magnet Shield: the ball sticks to the paddle until the owner releases it (or the hold time runs out).</summary>
    private void Catch(PlayerController player)
    {
        holder = player.transform;
        CaughtBy = player;
        caughtTimer = CardBalance.MagnetHoldSeconds;
        dirX = -player.SideSign; // shoot away from the paddle
        attached = true;
        live = false;
        rb.linearVelocity = Vector2.zero;
        SnapToHolder();
    }

    // ---------------------------------------------------------------- Simulation

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (IsDecoy)
        {
            decoyLife -= Time.fixedDeltaTime;
            if (decoyLife <= 0f)
            {
                if (IsSpawned) NetworkObject.Despawn(true);
                return;
            }

            KeepSpeed(speed);
            return;
        }

        if (attached && holder != null)
        {
            if (CaughtBy != null)
            {
                caughtTimer -= Time.fixedDeltaTime;
                if (caughtTimer <= 0f)
                {
                    Launch(0f); // nobody released it -> straight shot
                    return;
                }
            }

            SnapToHolder();
            return;
        }

        if (!live) return;

        for (int i = modifiers.Count - 1; i >= 0; i--)
            modifiers[i].Tick(this, Time.fixedDeltaTime);
        RemoveExpiredModifiers();

        KeepSpeed(EffectiveSpeed);
    }

    // Keep a constant speed (wall bounces otherwise drift) and avoid a near-vertical loop
    private void KeepSpeed(float targetSpeed)
    {
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.0001f) return;

        float minX = targetSpeed * 0.25f;
        if (Mathf.Abs(v.x) < minX)
            v.x = minX * (v.x >= 0f ? 1f : -1f);

        rb.linearVelocity = v.normalized * targetSpeed;
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
        if (!collision.collider.TryGetComponent(out PlayerController player)) return;

        Bounds b = collision.collider.bounds;

        // Where on the paddle did we hit? -1 (bottom) .. 1 (top)
        float offset = Mathf.Clamp((rb.position.y - b.center.y) / b.extents.y, -1f, 1f);
        float side = Mathf.Sign(rb.position.x - b.center.x);
        Vector2 dir = new Vector2(side, offset * maxBounceSlope).normalized;

        // Decoys only bounce: no mana, no card effects
        if (IsDecoy)
        {
            rb.linearVelocity = dir * speed;
            return;
        }

        speed = Mathf.Min(speed + speedGainPerHit, maxSpeed);

        // Card hooks run before the new velocity is computed (Fireball multiplier, Heavy stun, ...)
        for (int i = modifiers.Count - 1; i >= 0; i--)
            modifiers[i].OnPaddleHit(this, player);
        RemoveExpiredModifiers();

        player.ServerAddMana(1f); // GDD: +1 mana per successful hit

        // Giant Paddle / Magnet Shield bookkeeping for the paddle that was hit
        bool magnet = player.Effects != null && player.Effects.ServerRegisterBallHit();
        if (magnet)
        {
            Catch(player);
            return;
        }

        rb.linearVelocity = dir * EffectiveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || !live) return;

        // Tag the two goal trigger colliders "GoalLeft" (host side) and "GoalRight"
        bool left = other.CompareTag("GoalLeft");
        bool right = other.CompareTag("GoalRight");
        if (!left && !right) return;

        if (IsDecoy)
        {
            if (IsSpawned) NetworkObject.Despawn(true); // decoys never score
            return;
        }

        Scored(left);
    }

    private void Scored(bool leftSide)
    {
        live = false; // prevents double-trigger before GameManager re-attaches the ball
        GameManager.Instance.OnGoal(leftSide);
    }
}
