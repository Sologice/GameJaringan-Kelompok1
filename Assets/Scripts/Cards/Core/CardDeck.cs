using System;
using System.Collections.Generic;

/// <summary>
/// Clash-Royale style cycle, pure C# (no Unity / network code, easy to unit test).
/// 6 cards: 3 are in hand, 3 wait in a queue. Playing a hand slot puts the queue's front card
/// into that slot and sends the played card to the back of the queue.
/// </summary>
public sealed class CardDeck
{
    private readonly CardId[] hand = new CardId[CardBalance.HandSize];
    private readonly Queue<CardId> queue = new();

    public CardDeck(IReadOnlyList<CardId> cards, Random rng = null)
    {
        if (cards == null || cards.Count != CardBalance.DeckSize)
            throw new ArgumentException($"A deck needs exactly {CardBalance.DeckSize} cards.");

        var shuffled = new List<CardId>(cards);
        Shuffle(shuffled, rng ?? new Random());

        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i < CardBalance.HandSize) hand[i] = shuffled[i];
            else queue.Enqueue(shuffled[i]);
        }
    }

    public CardId GetHand(int slot) => hand[slot];

    /// <summary>The card that will enter the hand next (shown as "Next" in the UI).</summary>
    public CardId Next => queue.Peek();

    /// <summary>Plays a slot and cycles it. Returns the card that was played.</summary>
    public CardId Play(int slot)
    {
        CardId played = hand[slot];
        hand[slot] = queue.Dequeue();
        queue.Enqueue(played);
        return played;
    }

    public static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
