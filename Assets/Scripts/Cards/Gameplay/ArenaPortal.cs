using UnityEngine;

/// <summary>Server-side portal trigger. Created in code by ArenaEffects, no prefab needed.</summary>
public class ArenaPortal : MonoBehaviour
{
    public ArenaPortal Partner;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Partner == null) return;
        if (!other.TryGetComponent(out Ball ball)) return;
        if (!ball.IsLive || Time.time < ball.PortalReadyTime) return;

        ball.PortalReadyTime = Time.time + CardBalance.PortalCooldown;
        ball.Teleport(Partner.transform.position); // velocity is untouched -> same angle out of portal B
    }
}
