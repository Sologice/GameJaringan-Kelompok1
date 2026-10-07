using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Per-player paddle state changed by cards: length (Giant Paddle), speed (Sticky Ice),
/// stun (Heavy Ball) and the Magnet Shield / Giant Paddle bounce counters.
/// The server owns the state; clients read the NetworkVariables.
/// Add this component to the Player prefab. Turn OFF "Sync Scale" on the paddle's NetworkTransform,
/// the scale is synced here instead.
/// </summary>
public class PaddleEffects : NetworkBehaviour
{
    public NetworkVariable<float> LengthMultiplier = new(1f);
    public NetworkVariable<float> SpeedMultiplier = new(1f);
    public NetworkVariable<bool> Stunned = new(false);

    private Vector3 baseScale;

    // Server only
    private int giantHitsLeft;
    private int magnetHitsLeft;
    private float slowEndTime;
    private float stunEndTime;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public override void OnNetworkSpawn()
    {
        LengthMultiplier.OnValueChanged += OnLengthChanged;
        ApplyLength(LengthMultiplier.Value);
    }

    public override void OnNetworkDespawn()
    {
        LengthMultiplier.OnValueChanged -= OnLengthChanged;
    }

    private void OnLengthChanged(float previous, float current) => ApplyLength(current);

    // The paddle is long on its local Y axis (it moves up/down). The collider scales with the transform.
    private void ApplyLength(float multiplier)
    {
        transform.localScale = new Vector3(baseScale.x, baseScale.y * multiplier, baseScale.z);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (SpeedMultiplier.Value != 1f && Time.time >= slowEndTime) SpeedMultiplier.Value = 1f;
        if (Stunned.Value && Time.time >= stunEndTime) Stunned.Value = false;
    }

    // ---------------------------------------------------------------- Server API (called by card effects)

    public void ServerApplyGiant()
    {
        if (!IsServer) return;
        giantHitsLeft = CardBalance.GiantHits;
        LengthMultiplier.Value = CardBalance.GiantLengthMultiplier;
    }

    public void ServerApplyMagnet()
    {
        if (!IsServer) return;
        magnetHitsLeft = CardBalance.MagnetHits;
    }

    public void ServerApplySlow(float multiplier, float duration)
    {
        if (!IsServer) return;
        slowEndTime = Time.time + duration;
        SpeedMultiplier.Value = multiplier;
    }

    public void ServerStun(float duration)
    {
        if (!IsServer) return;
        stunEndTime = Time.time + duration;
        Stunned.Value = true;
    }

    /// <summary>
    /// Called by the Ball every time the real ball hits this paddle. Counts down Giant Paddle / Magnet Shield.
    /// Returns true if the Magnet Shield should catch the ball on this hit.
    /// </summary>
    public bool ServerRegisterBallHit()
    {
        if (!IsServer) return false;

        if (giantHitsLeft > 0)
        {
            giantHitsLeft--;
            if (giantHitsLeft == 0) LengthMultiplier.Value = 1f;
        }

        if (magnetHitsLeft > 0)
        {
            magnetHitsLeft--;
            return true;
        }

        return false;
    }

    /// <summary>Slow and stun do not carry over into the next serve.</summary>
    public void ServerClearTimedDebuffs()
    {
        if (!IsServer) return;
        SpeedMultiplier.Value = 1f;
        Stunned.Value = false;
    }

    /// <summary>Fresh state for a new match.</summary>
    public void ServerResetAll()
    {
        if (!IsServer) return;
        giantHitsLeft = 0;
        magnetHitsLeft = 0;
        LengthMultiplier.Value = 1f;
        ServerClearTimedDebuffs();
    }
}
