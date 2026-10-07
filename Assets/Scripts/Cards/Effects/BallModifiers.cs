using UnityEngine;

/// <summary>
/// A temporary behaviour attached to the ball (server only). The Ball calls these hooks, so the Ball
/// class never needs to know which card caused them. Set Expired = true and the Ball removes it.
/// </summary>
public interface IBallModifier
{
    Color Tint { get; }      // ball colour while active (Color.white = no tint)
    bool Expired { get; }
    void OnAttach(Ball ball);
    void Tick(Ball ball, float dt);                       // every FixedUpdate while the ball is live
    void OnPaddleHit(Ball ball, PlayerController hitter); // before the bounce velocity is computed
    void OnRemoved(Ball ball);                            // always called once, clean up here
}

public abstract class BallModifier : IBallModifier
{
    public virtual Color Tint => Color.white;
    public bool Expired { get; protected set; }

    public virtual void OnAttach(Ball ball) { }
    public virtual void Tick(Ball ball, float dt) { }
    public virtual void OnPaddleHit(Ball ball, PlayerController hitter) { }
    public virtual void OnRemoved(Ball ball) { }
}

/// <summary>Curveball: bends the ball while it travels through the opponent's half, until the opponent hits it.</summary>
public sealed class CurveModifier : BallModifier
{
    private readonly PlayerController owner;
    private readonly float sign = Random.value < 0.5f ? -1f : 1f;
    private float turned;

    public CurveModifier(PlayerController owner) { this.owner = owner; }

    public override Color Tint => new Color(0.75f, 0.45f, 1f);

    public override void Tick(Ball ball, float dt)
    {
        float oppSide = -owner.SideSign;

        // only curve in the opponent's half, and only while heading to them
        if (ball.Position.x * oppSide <= 0f || !ball.MovingToward(oppSide)) return;
        if (turned >= CardBalance.CurveMaxTurnDegrees) return;

        float step = CardBalance.CurveDegreesPerSecond * dt;
        turned += step;
        ball.RotateVelocity(sign * step);
    }

    public override void OnPaddleHit(Ball ball, PlayerController hitter)
    {
        if (hitter != owner) Expired = true;
    }
}

/// <summary>Heavy Ball: when the OPPONENT's paddle hits the ball, that paddle is frozen for 0.5s.</summary>
public sealed class HeavyModifier : BallModifier
{
    private readonly PlayerController owner;

    public HeavyModifier(PlayerController owner) { this.owner = owner; }

    public override Color Tint => new Color(0.55f, 0.6f, 0.7f);

    public override void OnPaddleHit(Ball ball, PlayerController hitter)
    {
        if (hitter == owner) return;

        if (hitter.Effects != null) hitter.Effects.ServerStun(CardBalance.HeavyStunSeconds);
        Expired = true;
    }
}

/// <summary>
/// Invisibility Ball: once the ball crosses the middle toward the opponent it disappears for the
/// OPPONENT only (the caster still sees it) for 1 second.
/// </summary>
public sealed class InvisibilityModifier : BallModifier
{
    private enum Phase { Waiting, Hidden }

    private readonly PlayerController owner;
    private readonly PlayerController opponent;
    private Phase phase = Phase.Waiting;
    private float timer;

    public InvisibilityModifier(PlayerController owner, PlayerController opponent)
    {
        this.owner = owner;
        this.opponent = opponent;
    }

    public override void Tick(Ball ball, float dt)
    {
        float oppSide = -owner.SideSign;

        if (phase == Phase.Waiting)
        {
            if (ball.MovingToward(oppSide) && ball.Position.x * oppSide >= 0f)
            {
                phase = Phase.Hidden;
                timer = CardBalance.InvisibilitySeconds;
                ball.SetHiddenFrom((long)opponent.OwnerClientId);
            }
            return;
        }

        timer -= dt;
        if (timer <= 0f) Expired = true;
    }

    public override void OnPaddleHit(Ball ball, PlayerController hitter)
    {
        // // the opponent reached the ball (or it came back): the trick is over
        // if (hitter == opponent || phase == Phase.Hidden) Expired = true;
    }

    public override void OnRemoved(Ball ball) => ball.SetHiddenFrom(-1);
}

/// <summary>Fireball: ball speed doubles on every paddle bounce for a few bounces, then goes back to normal.</summary>
public sealed class FireballModifier : BallModifier
{
    private int bouncesLeft = CardBalance.FireballBounces;

    public override Color Tint => new Color(1f, 0.45f, 0.1f);

    public override void OnPaddleHit(Ball ball, PlayerController hitter)
    {
        if (bouncesLeft <= 0)
        {
            Expired = true; // OnRemoved resets the multiplier before the bounce velocity is computed
            return;
        }

        bouncesLeft--;
        ball.SpeedMultiplier *= CardBalance.FireballMultiplier;
    }

    public override void OnRemoved(Ball ball) => ball.SpeedMultiplier = 1f;
}
