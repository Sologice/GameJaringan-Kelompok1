/// <summary>
/// One implementation per card. Runs on the SERVER only.
/// CanPlay is checked BEFORE mana is spent, so a rejected card costs nothing and is not cycled.
/// </summary>
public interface ICardEffect
{
    bool CanPlay(CardContext ctx);
    void Execute(CardContext ctx);
}
