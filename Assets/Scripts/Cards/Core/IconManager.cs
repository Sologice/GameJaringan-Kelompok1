using System;
using System.Collections.Generic;
using UnityEngine;

public class IconManager : MonoBehaviour
{
    public static IconManager Instance { get; private set; }

    [System.Serializable]
    public struct CardIconMapping
    {
        public CardId id;
        public Sprite icon;
    }

    [Header("Drag and Drop Card Icons Here")]
    [SerializeField] private List<CardIconMapping> cardIcons = new();

    private readonly Dictionary<CardId, Sprite> iconDictionary = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Build quick lookup dictionary
        foreach (var mapping in cardIcons)
        {
            if (mapping.icon != null && !iconDictionary.ContainsKey(mapping.id))
            {
                iconDictionary.Add(mapping.id, mapping.icon);
            }
        }
    }

    public Sprite GetIcon(CardId id)
    {
        return iconDictionary.TryGetValue(id, out var sprite) ? sprite : null;
    }
}