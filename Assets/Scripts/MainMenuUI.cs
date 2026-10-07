using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Title screen shown when the scene starts and again whenever the connection ends
/// (Main Menu button on the game over screen, host quit, connection lost).
/// Builds itself in code like the other UIs: put it on any GameObject in MainScene, no wiring needed.
///
/// Flow: Main menu -> Play -> deck builder -> Host / Client buttons -> match -> game over -> back here.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameTitle = "ULTIMATE PONG";

    private Canvas canvas;
    private DeckBuilderUI deckBuilder;
    private NetworkManagerUI networkUI;
    private bool subscribed;

    private void Awake()
    {
        BuildUI();
    }

    private void Start()
    {
        deckBuilder = FindFirstObjectByType<DeckBuilderUI>();
        networkUI = FindFirstObjectByType<NetworkManagerUI>();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped += OnNetworkStopped;
            subscribed = true;
        }
    }

    private void OnDestroy()
    {
        if (subscribed && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientStopped -= OnNetworkStopped;
    }

    private void BuildUI()
    {
        // 200 = above the deck builder (100) and the card hand (50)
        canvas = UIFactory.CreateCanvas("MainMenuCanvas", 200);

        var background = UIFactory.MakePanel(canvas.transform, "Background", new Color(0.05f, 0.06f, 0.1f, 1f));
        UIFactory.Stretch(background);

        var title = UIFactory.MakeText(background, gameTitle, 120, Color.white, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1500, 160), new Vector2(0, 220));

        var subtitle = UIFactory.MakeText(background, "2-player arcade pong with power-up cards", 36,
            new Color(0.75f, 0.78f, 0.9f), TextAlignmentOptions.Center);
        UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1200, 50), new Vector2(0, 120));

        MakeMenuButton(background, "Play", new Color(0.2f, 0.7f, 0.35f), new Vector2(0, -20), OnPlay);

#if !UNITY_WEBGL
        MakeMenuButton(background, "Quit", new Color(0.55f, 0.2f, 0.2f), new Vector2(0, -140), OnQuit);
#endif
    }

    private static void MakeMenuButton(Transform parent, string text, Color color, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var button = UIFactory.MakeButton(parent, text + "Button", color, onClick);
        UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420, 90), position);

        var label = UIFactory.MakeText(button.transform, text, 44, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(label.rectTransform);
    }

    // ---------------------------------------------------------------- Buttons

    private void OnPlay()
    {
        canvas.gameObject.SetActive(false);

        // Pick / confirm a deck, then NetworkManagerUI reveals Host / Client
        if (deckBuilder != null) deckBuilder.Show();
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------------------------------------------------------------- Returning to the menu

    /// <summary>Fires on the host and on clients when the connection ends for any reason.</summary>
    private void OnNetworkStopped(bool wasHost)
    {
        ShowMenu();
    }

    public void ShowMenu()
    {
        if (networkUI != null) networkUI.ResetToStart();
        canvas.gameObject.SetActive(true);
    }
}