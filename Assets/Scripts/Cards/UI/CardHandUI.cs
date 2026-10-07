using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match hand: 3 playable cards (J / K / L, or click) + the "Next" card of the cycle, mana readout and a
/// small message line. Builds itself in code; put it on any GameObject in the scene.
/// Only shows up once the local player has spawned.
/// </summary>
public class CardHandUI : MonoBehaviour
{
    private class Slot
    {
        public UnityEngine.UI.Button Button;
        public UnityEngine.UI.Image Background;
        public UnityEngine.UI.Image Icon;
        public TextMeshProUGUI Label;
    }

    private static readonly string[] KeyLabels = { "J", "K", "L" };

    private readonly Slot[] slots = new Slot[CardBalance.HandSize];
    private GameObject root;
    private UnityEngine.UI.Image nextBackground;
    private UnityEngine.UI.Image nextIcon;
    private TextMeshProUGUI nextLabel;
    private TextMeshProUGUI manaLabel;
    private TextMeshProUGUI toastLabel;

    private PlayerCards local;
    private float toastTimer;

    private void Awake()
    {
        BuildUI();
        root.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerCards.CardPlayed += OnCardPlayed;
    }

    private void OnDisable()
    {
        PlayerCards.CardPlayed -= OnCardPlayed;
        Unbind();
    }

    private void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("CardHandCanvas", 50);

        // Bottom-center container (800 x 210)
        var container = UIFactory.MakeGroup(canvas.transform, "Hand");
        UIFactory.Place(container, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(800, 210), new Vector2(0, 20));
        root = container.gameObject;

        // "Next" card (small, left)
        var nextPanel = UIFactory.MakePanel(container, "Next", new Color(0.2f, 0.2f, 0.25f, 0.9f));
        UIFactory.Place(nextPanel, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(120, 110), new Vector2(70, 85));
        nextBackground = nextPanel.GetComponent<UnityEngine.UI.Image>();

        // 1:1 Next Card Icon
        nextIcon = UIFactory.MakeImageIcon(nextPanel, "NextIcon", new Vector2(40, 40));

        nextLabel = UIFactory.MakeText(nextPanel, "", 18, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(nextLabel.rectTransform, 4);

        // 3 hand slots
        for (int i = 0; i < CardBalance.HandSize; i++)
        {
            int slotIndex = i;
            var button = UIFactory.MakeButton(container, $"Slot{i + 1}", Color.gray, () => OnSlotClicked(slotIndex));
            UIFactory.Place((RectTransform)button.transform, Vector2.zero, new Vector2(0.5f, 0.5f),
                new Vector2(200, 150), new Vector2(270 + i * 220, 85));

            // 1:1 Card Icon
            var icon = UIFactory.MakeImageIcon(button.transform, "CardIcon", new Vector2(50, 50));

            var label = UIFactory.MakeText(button.transform, "", 24, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(label.rectTransform, 6);

            slots[i] = new Slot
            {
                Button = button,
                Background = button.GetComponent<UnityEngine.UI.Image>(),
                Icon = icon,
                Label = label
            };
        }

        manaLabel = UIFactory.MakeText(container, "", 28, Color.white, TextAlignmentOptions.Center);
        UIFactory.Place(manaLabel.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(400, 36), new Vector2(400, 190));

        // Message line (top-center of the screen)
        toastLabel = UIFactory.MakeText(canvas.transform, "", 34, Color.white, TextAlignmentOptions.Center);
        UIFactory.Place(toastLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1000, 50), new Vector2(0, -90));
        toastLabel.alpha = 0f;
    }

    private void Update()
    {
        // Bind to / unbind from the local player's cards as the connection comes and goes
        if (local == null)
        {
            if (PlayerCards.Local == null)
            {
                root.SetActive(false);
                TickToast();
                return;
            }

            Bind(PlayerCards.Local);
        }
        else if (PlayerCards.Local != local)
        {
            Unbind();
            root.SetActive(false);
            return;
        }

        root.SetActive(true);
        Refresh();
        TickToast();
    }

    private void Bind(PlayerCards cards)
    {
        local = cards;
        local.MessageReceived += ShowToast;
    }

    private void Unbind()
    {
        if (local == null) return;
        local.MessageReceived -= ShowToast;
        local = null;
    }

    private void Refresh()
    {
        float mana = local.Controller.mana.Value;
        var game = GameManager.Instance;
        bool playable = game != null && game.CardsPlayable;

        manaLabel.text = $"Mana {mana:0.0}";

        if (!local.HasHand) return;

        for (int i = 0; i < slots.Length; i++)
        {
            CardDefinition def = CardCatalog.Get(local.Hand[i]);
            bool affordable = mana + 0.0001f >= def.ManaCost;

            Color color = UIFactory.CategoryColor(def.Category);
            color.a = (playable && affordable) ? 1f : 0.4f;
            slots[i].Background.color = color;

            // Set icon sprite if assigned
            if (def.Icon != null)
            {
                slots[i].Icon.sprite = def.Icon;
                slots[i].Icon.gameObject.SetActive(true);
            }
            else
            {
                slots[i].Icon.gameObject.SetActive(false);
            }

            slots[i].Label.text =
                $"<size=20>[{KeyLabels[i]}]</size>\n<b>{def.Name}</b>\n<size=22>Mana {def.ManaCost}</size>";
        }

        CardDefinition next = CardCatalog.Get(local.Next);
        nextBackground.color = UIFactory.CategoryColor(next.Category) * 0.6f;

        if (next.Icon != null)
        {
            nextIcon.sprite = next.Icon;
            nextIcon.gameObject.SetActive(true);
        }
        else
        {
            nextIcon.gameObject.SetActive(false);
        }

        nextLabel.text = $"<size=16>Next</size>\n<b><size=18>{next.Name}</size></b>\n<size=16>Mana {next.ManaCost}</size>";
    }

    private void OnSlotClicked(int slot)
    {
        if (local != null) local.RequestPlay(slot);
    }

    private void OnCardPlayed(ulong clientId, CardId id)
    {
        bool mine = NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId;
        string name = CardCatalog.Get(id).Name;
        ShowToast(mine ? $"You played {name}" : $"Opponent played {name}");
    }

    private void ShowToast(string message)
    {
        toastLabel.text = message;
        toastTimer = 2f;
    }

    private void TickToast()
    {
        if (toastTimer <= 0f) return;

        toastTimer -= Time.deltaTime;
        toastLabel.alpha = Mathf.Clamp01(toastTimer);
    }
}