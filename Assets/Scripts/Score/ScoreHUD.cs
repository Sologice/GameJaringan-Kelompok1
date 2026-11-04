using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// =====================================================================
//  ScoreHUD — tampilkan skor live pemain lokal di tengah-atas layar.
//  Otomatis sembunyi saat belum masuk match.
//  Prasyarat: PlayerController harus punya `public NetworkVariable<int> score`.
// =====================================================================
public class ScoreHUD : MonoBehaviour
{
    [SerializeField] private bool hideWhenNotPlaying = true;
    [SerializeField] private int sortingOrder = 40;

    private Canvas canvas;
    private TextMeshProUGUI label;
    private PlayerController localPlayer;

    private void Awake()
    {
        canvas = UIFactory.CreateCanvas("ScoreHudCanvas", sortingOrder);

        label = UIFactory.MakeText(canvas.transform, "SCORE 0", 34, Color.white, TextAlignmentOptions.Center);
        UIFactory.Place(label.rectTransform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(600, 50), new Vector2(0, -20));

        // Outline supaya kontras dengan background apa pun
        var outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private void Update()
    {
        var net = NetworkManager.Singleton;

        // Belum connect / belum spawn player lokal
        if (net == null || net.LocalClient == null || net.LocalClient.PlayerObject == null)
        {
            localPlayer = null;
            if (hideWhenNotPlaying) canvas.gameObject.SetActive(false);
            return;
        }

        if (localPlayer == null)
        {
            localPlayer = net.LocalClient.PlayerObject.GetComponent<PlayerController>();
            if (localPlayer == null) return;
        }

        if (hideWhenNotPlaying)
        {
            var game = GameManager.Instance;
            bool show = game != null
                        && game.IsSpawned
                        && game.State.Value != GameState.WaitingForPlayers;
            canvas.gameObject.SetActive(show);
        }

        label.text = $"SCORE {localPlayer.score.Value}";
    }
}