using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory pattern: turns a CardId into its ICardEffect. The rest of the game (PlayerCards, UI)
/// never references a concrete effect class, so adding a card = enum value + catalog entry
/// + effect class + one line in the table below.
/// </summary>
public static class CardEffectFactory
{
    private static readonly Dictionary<CardId, Func<ICardEffect>> creators = new()
    {
        // Platform
        { CardId.GiantPaddle,      () => new GiantPaddleEffect() },
        { CardId.MagnetShield,     () => new MagnetShieldEffect() },
        { CardId.StickyIce,        () => new StickyIceEffect() },

        // Ball
        { CardId.Curveball,        () => new CurveballEffect() },
        { CardId.MultiBall,        () => new MultiBallEffect() },
        { CardId.HeavyBall,        () => new HeavyBallEffect() },
        { CardId.InvisibilityBall, () => new InvisibilityBallEffect() },
        { CardId.Fireball,         () => new FireballEffect() },

        // Arena
        { CardId.BouncerNet,       () => new BouncerNetEffect() },
        { CardId.PortalTrap,       () => new PortalTrapEffect() },
    };

    public static ICardEffect Create(CardId id)
    {
        if (creators.TryGetValue(id, out var create)) return create();
        throw new ArgumentOutOfRangeException(nameof(id), id, "No effect registered for this card.");
    }

    /// <summary>Optional: lets other code (tests, mods) add or replace an effect.</summary>
    public static void Register(CardId id, Func<ICardEffect> creator) => creators[id] = creator;

    /// <summary>Logs an error for every catalog card that has no effect. Called once when the match starts.</summary>
    public static void ValidateAll()
    {
        foreach (var def in CardCatalog.All)
            if (!creators.ContainsKey(def.Id))
                Debug.LogError($"[CardEffectFactory] Card '{def.Name}' has no effect registered!");
    }
}
