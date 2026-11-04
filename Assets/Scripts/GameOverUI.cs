using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    private Canvas canvas;
    private TextMeshProUGUI resultLabel;
    private TextMeshProUGUI detailLabel;
    private TextMeshProUGUI matchScoreLabel;     // NEW
    private TextMeshProUGUI highScoreLabel;      // NEW
    private Button rematchButton;
    private TextMeshProUGUI rematchLabel;

    private bool rematchRequested;
    private bool scoreSubmitted;                 // NEW: guard 1x submit per match

    private void Awake()
    {
        BuildUI();
        canvas.gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("GameOverCanvas", 150);

        var background = UIFactory.MakePanel(canvas.transform, "Background", new Color(0f, 0f, 0f, 0.8f));
        UIFactory.Stretch(background);

        resultLabel = UIFactory.MakeText(background, "", 130, Color.white, TextAlignmentOptions.Center);
        resultLabel.fontStyle = FontStyles.Bold;
        UIFactory.Place(resultLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(1500, 170), new Vector2(0, 260));

        detailLabel = UIFactory.MakeText(background, "", 38, new Color(0.8f, 0.82f, 0.9f), TextAlignmentOptions.Center);
        UIFactory.Place(detailLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(1200, 60), new Vector2(0, 155));

        // ---- NEW: skor match ----
        matchScoreLabel = UIFactory.MakeText(background, "", 46, Color.white, TextAlignmentOptions.Center);
        UIFactory.Place(matchScoreLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(1200, 60), new Vector2(0, 80));

        // ---- NEW: personal high score ----
        highScoreLabel = UIFactory.MakeText(background, "", 40, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center);
        UIFactory.Place(highScoreLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(1200, 60), new Vector2(0, 20));

        rematchButton = UIFactory.MakeButton(background, "RematchButton", new Color(0.2f, 0.7f, 0.35f), OnRematch);
        UIFactory.Place((RectTransform)rematchButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(560, 90), new Vector2(0, -100));
        rematchLabel = UIFactory.MakeText(rematchButton.transform, "Rematch", 40, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(rematchLabel.rectTransform);

        var menuButton = UIFactory.MakeButton(background, "MainMenuButton", new Color(0.35f, 0.35f, 0.45f), OnMainMenu);
        UIFactory.Place((RectTransform)menuButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(560, 90), new Vector2(0, -220));
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
                rematchRequested = false;
                scoreSubmitted = false;                       // match berikutnya boleh submit lagi
                matchScoreLabel.text = "";
                highScoreLabel.text = "";
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

        // ---- Skor ----
        var localPlayer = GetLocalPlayer();
        int matchScore = localPlayer != null ? localPlayer.score.Value : 0;
        matchScoreLabel.text = $"Match Score: {matchScore}";

        if (!scoreSubmitted)
        {
            scoreSubmitted = true;
            highScoreLabel.text = "Personal High Score: ...";
            StartCoroutine(SaveThenFetchRoutine());
        }
    }

    /// <summary>Simpan skor match ini, lalu ambil rekor tertinggi (berurutan, sesuai Modul 11).</summary>
    private IEnumerator SaveThenFetchRoutine()
    {
        // Beri waktu NetworkVariable skor (termasuk bonus menang) tersinkron ke client
        yield return new WaitForSeconds(0.75f);

        var player = GetLocalPlayer();
        int finalScore = player != null ? player.score.Value : 0;
        matchScoreLabel.text = $"Match Score: {finalScore}";

        // 1) POST /api/score/save
        bool saved = false;
        ScoreManager.Instance.SaveCurrentScore(finalScore, () => saved = true);
        while (!saved) yield return null;

        // 2) GET /api/score/highscore/:userId
        int highScore = 0;
        bool fetched = false;
        ScoreManager.Instance.GetPersonalHighScore(hs => { highScore = hs; fetched = true; });
        while (!fetched) yield return null;

        // 3) UI binding
        bool newRecord = finalScore >= highScore;
        highScoreLabel.text = newRecord
            ? $"Personal High Score: {highScore}  <color=#7CFF8A>(NEW RECORD!)</color>"
            : $"Personal High Score: {highScore}";
    }

    private PlayerController GetLocalPlayer()
    {
        var net = NetworkManager.Singleton;
        if (net == null || net.LocalClient == null || net.LocalClient.PlayerObject == null) return null;
        return net.LocalClient.PlayerObject.GetComponent<PlayerController>();
    }

    private void OnRematch()
    {
        if (GameManager.Instance == null) return;
        rematchRequested = true;
        GameManager.Instance.RequestRematchServerRpc();
    }

    private void OnMainMenu()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
    }
}