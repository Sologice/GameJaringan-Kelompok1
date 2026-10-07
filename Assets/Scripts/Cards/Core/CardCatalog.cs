using System.Collections.Generic;
using System.Linq;

/// <summary>The 10 cards from the GDD. Mana costs match the GDD tables.</summary>
public static class CardCatalog
{
    private static readonly List<CardDefinition> all = new()
    {
        // A. Platform modifiers
        new CardDefinition(CardId.GiantPaddle, "Giant Paddle", 3, CardCategory.Paddle,
            "Paddle +50% longer\nfor your next 3 hits."),
        new CardDefinition(CardId.MagnetShield, "Magnet Shield", 4, CardCategory.Paddle,
            "Ball sticks to your paddle on\nnext 2 hits. Aim, press Space."),
        new CardDefinition(CardId.StickyIce, "Sticky Ice", 5, CardCategory.Paddle,
            "Opponent paddle is 40%\nslower for 3 seconds."),

        // B. Ball modifiers
        new CardDefinition(CardId.Curveball, "Curveball", 3, CardCategory.Ball,
            "Ball curves as it nears\nthe opponent's side."),
        new CardDefinition(CardId.MultiBall, "Multi-Ball", 6, CardCategory.Ball,
            "Splits into 3 balls: 1 real,\n2 decoys for 5 seconds."),
        new CardDefinition(CardId.HeavyBall, "Heavy Ball", 2, CardCategory.Ball,
            "Opponent paddle freezes\n0.5s when it hits the ball."),
        new CardDefinition(CardId.InvisibilityBall, "Invisibility Ball", 7, CardCategory.Ball,
            "Ball vanishes for the\nopponent for 1 second."),
        new CardDefinition(CardId.Fireball, "Fireball", 4, CardCategory.Ball,
            "Ball speed doubles on\neach bounce (3 bounces)."),

        // C. Arena modifiers
        new CardDefinition(CardId.BouncerNet, "Bouncer Net", 5, CardCategory.Arena,
            "Net on the center line\nbounces the ball back (2s)."),
        new CardDefinition(CardId.PortalTrap, "Portal Trap", 3, CardCategory.Arena,
            "2 random portals: enter A,\nexit B, same direction."),
    };

    private static readonly Dictionary<CardId, CardDefinition> byId = all.ToDictionary(d => d.Id);

    public static IReadOnlyList<CardDefinition> All => all;

    public static bool Has(CardId id) => byId.ContainsKey(id);

    public static CardDefinition Get(CardId id) => byId[id];
}