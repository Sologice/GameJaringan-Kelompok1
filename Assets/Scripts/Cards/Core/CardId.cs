/// <summary>One entry per power-up card (GDD: 10 cards total). Values are sent over the network as ints.</summary>
public enum CardId
{
    // Platform modifiers
    GiantPaddle = 0,
    MagnetShield = 1,
    StickyIce = 2,

    // Ball modifiers
    Curveball = 3,
    MultiBall = 4,
    HeavyBall = 5,
    InvisibilityBall = 6,
    Fireball = 7,

    // Arena modifiers
    BouncerNet = 8,
    PortalTrap = 9
}

public enum CardCategory
{
    Paddle,
    Ball,
    Arena
}
