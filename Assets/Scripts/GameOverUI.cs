using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game over screen. Shows itself while GameManager.State == GameOver, with the result for the local player,
/// a Rematch button (restarts once BOTH players pressed it) and a Main Menu button (disconnects).
/// Builds itself in code: put it on any GameObject in MainScene, no wiring needed.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private Canvas canvas;
    private TextMeshProUGUI resultLabel;
    private TextMeshProUGUI detailLabel;
    private Button rematchButton;
    private TextMeshProUGUI rematchLabel;

    private bool rematchRequested;

    private void Awake()
    {
        BuildUI();
        canvas.gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        // 150 = above the card hand (50) and deck builder (100), below the main menu (200)
        canvas = UIFactory.CreateCanvas("GameOverCanvas", 150);

        var background = UIFactory.MakePanel(canvas.transform, "Background", new Color(0f, 0f, 0f, 0.8f));
        UIFactory.Stretch(background);

        resultLabel = UIFactory.MakeText(background, "", 130, Color.white, TextAlignmentOptions.Center);
        resultLabel.fontStyle = FontStyles.Bold;
        UIFactory.Place(resultLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1500, 170), new Vector2(0, 160));

        detailLabel = UIFactory.MakeText(background, "", 38, new Color(0.8f, 0.82f, 0.9f), TextAlignmentOptions.Center);
        UIFactory.Place(detailLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1200, 60), new Vector2(0, 50));

        rematchButton = UIFactory.MakeButton(background, "RematchButton", new Color(0.2f, 0.7f, 0.35f), OnRematch);
        UIFactory.Place((RectTransform)rematchButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 90), new Vector2(0, -70));
        rematchLabel = UIFactory.MakeText(rematchButton.transform, "Rematch", 40, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(rematchLabel.rectTransform);

        var menuButton = UIFactory.MakeButton(background, "MainMenuButton", new Color(0.35f, 0.35f, 0.45f), OnMainMenu);
        UIFactory.Place((RectTransform)menuButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 90), new Vector2(0, -190));
        var menuLabel = UIFactory.MakeText(menuButton.transform, "Main Menu", 40, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(menuLabel.rectTransform);
    }

    private void Update()
    {
        var game = GameManager.Instance;
        var net = NetworkManager.Singleton;

        bool show = game != null && game.IsSpawned && net != null && net.IsListening
                    && game.State.Value == GameState.GameOver;

        if (!show)
        {
            if (canvas.gameObject.activeSelf)
            {
                canvas.gameObject.SetActive(false);
                rematchRequested = false; // next game over starts fresh
            }
            return;
        }

        if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        Refresh(game, net);
    }

    private void Refresh(GameManager game, NetworkManager net)
    {
        bool won = game.WinnerClientId.Value == net.LocalClientId;
        resultLabel.text = won ? "YOU WIN!" : "YOU LOSE";
        resultLabel.color = won ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.45f);
        detailLabel.text = won ? "Your opponent ran out of health" : "You ran out of health";

        // Both paddles exist on both machines, so counting them tells us whether the opponent is still here
        bool opponentHere = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length >= 2;

        if (!opponentHere)
        {
            detailLabel.text = "Your opponent left the match";
            rematchLabel.text = "Rematch";
            rematchButton.interactable = false;
        }
        else if (rematchRequested)
        {
            rematchLabel.text = $"Waiting for opponent ({game.RematchVotes.Value}/2)";
            rematchButton.interactable = false;
        }
        else
        {
            rematchLabel.text = game.RematchVotes.Value > 0 ? "Rematch (opponent is ready!)" : "Rematch";
            rematchButton.interactable = true;
        }
    }

    private void OnRematch()
    {
        if (GameManager.Instance == null) return;

        rematchRequested = true;
        GameManager.Instance.RequestRematchServerRpc();
    }

    private void OnMainMenu()
    {
        // MainMenuUI listens for the shutdown and brings the title screen back
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
    }
}