using UnityEngine;

/// <summary>Static description of a card (name, cost, text). Behaviour lives in an ICardEffect, not here.</summary>
public sealed class CardDefinition
{
    public CardId Id { get; }
    public string Name { get; }
    public int ManaCost { get; }
    public CardCategory Category { get; }
    public string Description { get; }
    
    // Dynamically fetches the icon from IconManager
    public Sprite Icon => IconManager.Instance != null ? IconManager.Instance.GetIcon(Id) : null;

    public CardDefinition(CardId id, string name, int manaCost, CardCategory category, string description)
    {
        Id = id;
        Name = name;
        ManaCost = manaCost;
        Category = category;
        Description = description;
    }
}