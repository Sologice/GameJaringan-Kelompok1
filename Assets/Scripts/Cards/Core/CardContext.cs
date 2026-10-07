/// <summary>Everything a card effect may need. Built on the server right before a card is played.</summary>
public readonly struct CardContext
{
    public readonly GameManager Game;
    public readonly PlayerController Caster;
    public readonly PlayerController Opponent;
    public readonly Ball Ball;   // the real ball (never a decoy)

    public CardContext(GameManager game, PlayerController caster, PlayerController opponent, Ball ball)
    {
        Game = game;
        Caster = caster;
        Opponent = opponent;
        Ball = ball;
    }
}
