using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The 6 cards the local player picked in the deck builder (saved in PlayerPrefs).
/// Sent to the server when the player spawns; the server validates it with TryParse.
/// </summary>
public static class DeckSelection
{
    private const string PrefKey = "UltimatePong.Deck";

    private static List<CardId> current;
    private static bool loaded;

    /// <summary>The saved / chosen deck, or null if the player never picked one.</summary>
    public static IReadOnlyList<CardId> Current
    {
        get { Load(); return current; }
    }

    public static bool Set(IEnumerable<CardId> cards)
    {
        var list = cards.ToList();
        if (!IsValid(list)) return false;

        current = list;
        loaded = true;
        PlayerPrefs.SetString(PrefKey, string.Join(",", list.Select(c => (int)c)));
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Deck to send to the server. Falls back to a random deck if nothing was picked.</summary>
    public static int[] ToIntArray()
    {
        Load();
        var deck = current ?? RandomDeck();
        return deck.Select(c => (int)c).ToArray();
    }

    /// <summary>Server-side validation of what a client sent (right size, known cards, no duplicates).</summary>
    public static bool TryParse(int[] ids, out List<CardId> deck)
    {
        deck = null;
        if (ids == null || ids.Length != CardBalance.DeckSize) return false;

        var list = new List<CardId>();
        foreach (int id in ids)
        {
            if (!System.Enum.IsDefined(typeof(CardId), id)) return false;
            list.Add((CardId)id);
        }

        if (!IsValid(list)) return false;
        deck = list;
        return true;
    }

    public static List<CardId> RandomDeck()
    {
        var pool = CardCatalog.All.Select(d => d.Id).ToList();
        CardDeck.Shuffle(pool, new System.Random());
        return pool.Take(CardBalance.DeckSize).ToList();
    }

    private static bool IsValid(List<CardId> list)
    {
        return list.Count == CardBalance.DeckSize
            && list.Distinct().Count() == list.Count
            && list.All(CardCatalog.Has);
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;

        string saved = PlayerPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(saved)) return;

        var parsed = new List<int>();
        foreach (string part in saved.Split(','))
            if (int.TryParse(part, out int value)) parsed.Add(value);

        if (TryParse(parsed.ToArray(), out var deck)) current = deck;
    }
}
