/// <summary>
/// Every tunable number of the card system in one place (values come from the GDD where it gives one).
/// </summary>
public static class CardBalance
{
    // Deck / hand (Clash-Royale style cycle)
    public const int DeckSize = 6;      // cards the player brings into the match
    public const int HandSize = 3;      // J / K / L
    public const int PoolSize = 10;     // cards the player picks from

    // Giant Paddle
    public const float GiantLengthMultiplier = 1.5f;   // +50%
    public const int GiantHits = 3;                    // 3 bounces

    // Magnet Shield
    public const int MagnetHits = 2;                   // next 2 bounces stick to the paddle
    public const float MagnetHoldSeconds = 3f;         // auto-release if the player does nothing

    // Sticky Ice
    public const float StickySpeedMultiplier = 0.6f;   // -40% paddle speed
    public const float StickyDuration = 3f;

    // Curveball
    public const float CurveDegreesPerSecond = 110f;
    public const float CurveMaxTurnDegrees = 50f;      // the curve never turns the ball more than this

    // Multi-Ball
    public const int DecoyCount = 2;                   // 1 real + 2 decoys
    public const float MultiBallDuration = 5f;
    public const float DecoySpreadDegrees = 20f;

    // Heavy Ball
    public const float HeavyStunSeconds = 0.5f;

    // Invisibility Ball
    public const float InvisibilitySeconds = 7f;

    // Fireball
    public const float FireballMultiplier = 2f;        // x2 on every paddle bounce
    public const int FireballBounces = 3;              // how many bounces it keeps doubling
    public const float FireballSpeedCap = 40f;

    // Bouncer Net
    public const float NetDuration = 2f;

    // Portal Trap
    public const float PortalDuration = 6f;            // GDD gives no duration
    public const float PortalRadius = 0.6f;
    public const float PortalCooldown = 0.4f;          // stops a ball from instantly re-entering
}
