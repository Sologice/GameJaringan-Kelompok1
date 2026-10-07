// C. Arena modifiers

/// <summary>Bouncer Net: a solid net on the center line for 2 seconds that bounces the ball back.</summary>
public sealed class BouncerNetEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) => ctx.Game != null && !ArenaEffects.ServerNetActive;

    public void Execute(CardContext ctx) => ctx.Game.ServerSpawnNet();
}

/// <summary>Portal Trap: 2 random portals. Ball entering one exits the other with the same direction.</summary>
public sealed class PortalTrapEffect : ICardEffect
{
    public bool CanPlay(CardContext ctx) => ctx.Game != null && !ArenaEffects.ServerPortalsActive;

    public void Execute(CardContext ctx) => ctx.Game.ServerSpawnPortals();
}
