// B. Ball modifiers
// Most of these just attach an IBallModifier to the real ball; the behaviour lives in BallModifiers.cs.

/// <summary>Curveball: ball bends while it travels through the opponent's half.</summary>
public sealed class CurveballEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) =>
        ctx.Ball != null && !ctx.Ball.HasModifier<CurveModifier>();

    public void Execute(CardContext ctx) => ctx.Ball.AddModifier(new CurveModifier(ctx.Caster));
}

/// <summary>Multi-Ball: 1 real ball + 2 decoys (decoys never score, never give mana) for 5 seconds.</summary>
public sealed class MultiBallEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) =>
        ctx.Ball != null && ctx.Ball.IsLive && !ctx.Game.HasDecoys;

    public void Execute(CardContext ctx) =>
        ctx.Game.ServerSpawnDecoys(CardBalance.DecoyCount, CardBalance.MultiBallDuration);
}

/// <summary>Heavy Ball: the opponent's paddle freezes for 0.5s when it hits this ball.</summary>
public sealed class HeavyBallEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) =>
        ctx.Ball != null && ctx.Opponent != null && !ctx.Ball.HasModifier<HeavyModifier>();

    public void Execute(CardContext ctx) => ctx.Ball.AddModifier(new HeavyModifier(ctx.Caster));
}

/// <summary>Invisibility Ball: ball vanishes for the opponent for 1 second mid-flight.</summary>
public sealed class InvisibilityBallEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) =>
        ctx.Ball != null && ctx.Opponent != null && !ctx.Ball.HasModifier<InvisibilityModifier>();

    public void Execute(CardContext ctx) =>
        ctx.Ball.AddModifier(new InvisibilityModifier(ctx.Caster, ctx.Opponent));
}

/// <summary>Fireball: ball speed doubles on each paddle bounce (for a few bounces).</summary>
public sealed class FireballEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) =>
        ctx.Ball != null && !ctx.Ball.HasModifier<FireballModifier>();

    public void Execute(CardContext ctx) => ctx.Ball.AddModifier(new FireballModifier());
}
