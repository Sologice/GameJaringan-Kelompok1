// A. Platform modifiers

/// <summary>Giant Paddle: +50% paddle length for the next 3 bounces.</summary>
public sealed class GiantPaddleEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) => ctx.Caster != null && ctx.Caster.Effects != null;

    public void Execute(CardContext ctx) => ctx.Caster.Effects.ServerApplyGiant();
}

/// <summary>Magnet Shield: the next 2 ball hits stick to the paddle so the owner can aim (Space to release).</summary>
public sealed class MagnetShieldEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) => ctx.Caster != null && ctx.Caster.Effects != null;

    public void Execute(CardContext ctx) => ctx.Caster.Effects.ServerApplyMagnet();
}

/// <summary>Sticky Ice: opponent paddle moves 40% slower for 3 seconds.</summary>
public sealed class StickyIceEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) => ctx.Opponent != null && ctx.Opponent.Effects != null;

    public void Execute(CardContext ctx) =>
        ctx.Opponent.Effects.ServerApplySlow(CardBalance.StickySpeedMultiplier, CardBalance.StickyDuration);
}
