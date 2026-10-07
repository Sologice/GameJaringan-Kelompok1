using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Pick 6 of the 10 power-ups" screen shown before Host / Client. Builds itself in code.
/// Put it on any GameObject in the scene and drag it into NetworkManagerUI's "Deck Builder" slot.
/// The choice is saved (PlayerPrefs) and sent to the server when the player spawns.
/// </summary>
public class DeckBuilderUI : MonoBehaviour
{
    public event Action Confirmed;
    public bool IsConfirmed { get; private set; }

    private class CardView
    {
        public CardDefinition Def;
        public UnityEngine.UI.Image Background;
        public Outline Outline;
        public TextMeshProUGUI Label;
    }

    private readonly HashSet<CardId> selected = new();
    private readonly List<CardView> views = new();

    private Canvas canvas;
    private TextMeshProUGUI counterLabel;
    private UnityEngine.UI.Button confirmButton;

    private void Awake()
    {
        // start from the saved deck, if any
        if (DeckSelection.Current != null)
            foreach (var id in DeckSelection.Current) selected.Add(id);

        BuildUI();
        Refresh();
    }

    /// <summary>Re-open the builder (for example from a future "Edit deck" button).</summary>
    public void Show()
    {
        IsConfirmed = false;
        canvas.gameObject.SetActive(true);
        Refresh();
    }

    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("DeckBuilderCanvas", 100);

        var background = UIFactory.MakePanel(canvas.transform, "Background", new Color(0.05f, 0.06f, 0.1f, 0.96f));
        UIFactory.Stretch(background);

        var title = UIFactory.MakeText(background, $"Choose {CardBalance.DeckSize} of {CardBalance.PoolSize} power-ups",
            56, Color.white, TextAlignmentOptions.Center);
        UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1200, 80), new Vector2(0, -50));

        // Grid of cards: 5 columns x 2 rows
        var grid = UIFactory.MakeGroup(background, "Grid");
        UIFactory.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1356, 354), new Vector2(0, 10));

        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(260, 170);
        layout.spacing = new Vector2(14, 14);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        layout.childAlignment = TextAnchor.MiddleCenter;

        foreach (var def in CardCatalog.All)
        {
            CardDefinition captured = def;
            var button = UIFactory.MakeButton(grid, def.Name, Color.white, () => Toggle(captured.Id));

            var view = new CardView
            {
                Def = def,
                Background = button.GetComponent<UnityEngine.UI.Image>(),
                Outline = button.gameObject.AddComponent<Outline>(),
                Label = UIFactory.MakeText(button.transform, "", 20, Color.white, TextAlignmentOptions.Center),
            };
            view.Outline.effectColor = Color.white;
            view.Outline.effectDistance = new Vector2(5, -5);
            UIFactory.Stretch(view.Label.rectTransform, 8);
            views.Add(view);
        }

        counterLabel = UIFactory.MakeText(background, "", 36, Color.white, TextAlignmentOptions.Center);
        UIFactory.Place(counterLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(600, 50), new Vector2(0, 170));

        confirmButton = UIFactory.MakeButton(background, "ConfirmButton", new Color(0.2f, 0.7f, 0.35f), OnConfirm);
        UIFactory.Place((RectTransform)confirmButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420, 80), new Vector2(0, 70));
        var confirmLabel = UIFactory.MakeText(confirmButton.transform, "Confirm Deck", 36, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(confirmLabel.rectTransform);
    }

    private void Toggle(CardId id)
    {
        if (!selected.Remove(id) && selected.Count < CardBalance.DeckSize)
            selected.Add(id);

        Refresh();
    }

    private void Refresh()
    {
        foreach (var view in views)
        {
            bool isSelected = selected.Contains(view.Def.Id);
            Color baseColor = UIFactory.CategoryColor(view.Def.Category);

            view.Background.color = isSelected ? baseColor : baseColor * 0.4f;
            view.Background.color = new Color(view.Background.color.r, view.Background.color.g, view.Background.color.b, 1f);
            view.Outline.enabled = isSelected;

            string tag = isSelected ? "  <color=#FFFF66>[SELECTED]</color>" : "";
            view.Label.text =
                $"<b><size=26>{view.Def.Name}</size></b>{tag}\n" +
                $"<size=18><color=#DDDDDD>Mana {view.Def.ManaCost}  |  {UIFactory.CategoryName(view.Def.Category)}</color></size>\n" +
                $"<size=17>{view.Def.Description}</size>";
        }

        counterLabel.text = $"Selected {selected.Count} / {CardBalance.DeckSize}";
        confirmButton.interactable = selected.Count == CardBalance.DeckSize;
    }

    private void OnConfirm()
    {
        if (!DeckSelection.Set(selected)) return;

        IsConfirmed = true;
        canvas.gameObject.SetActive(false);
        Confirmed?.Invoke();
    }
}
