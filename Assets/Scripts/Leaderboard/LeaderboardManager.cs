using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Fetches GET /api/leaderboard and rebuilds the ScrollView content dynamically.
/// Put this on the LeaderboardPanel GameObject.
/// Base URL matches ScoreManager / AuthManager ("http://localhost:5000/api").
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    [Header("API Config")]
    [SerializeField] private string baseURL = "http://localhost:5000/api";

    [Header("UI References")]
    [SerializeField] private Transform  leaderboardContainer;   // ScrollView/Viewport/Content
    [SerializeField] private GameObject leaderboardPrefab;      // the prefab from B.4
    [SerializeField] private GameObject loadingLabel;           // optional "Loading..." text
    [SerializeField] private GameObject emptyLabel;             // optional "No scores yet"
    [SerializeField] private Button     refreshButton;

    private void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(FetchLeaderboard);
    }

    private void OnEnable()
    {
        FetchLeaderboard();
    }

    public void FetchLeaderboard()
    {
        StartCoroutine(GetLeaderboardRoutine());
    }

    private IEnumerator GetLeaderboardRoutine()
    {
        // Guard: refs must be wired in the Inspector
        if (leaderboardContainer == null || leaderboardPrefab == null)
        {
            Debug.LogError("[LeaderboardManager] leaderboardContainer / leaderboardPrefab not assigned!");
            yield break;
        }

        if (loadingLabel != null) loadingLabel.SetActive(true);
        if (emptyLabel   != null) emptyLabel.SetActive(false);

        using (UnityWebRequest request = UnityWebRequest.Get(baseURL + "/leaderboard"))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LeaderboardManager] Gagal memuat leaderboard: " +
                               $"HTTP {request.responseCode} — {request.error}");
                if (loadingLabel != null) loadingLabel.SetActive(false);
                yield break;
            }

            LeaderboardResponse response = null;
            try
            {
                response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LeaderboardManager] Parse gagal: {e.Message}");
            }

            if (loadingLabel != null) loadingLabel.SetActive(false);

            if (response != null && response.status == "success" && response.data != null)
            {
                PopulateScrollView(response.data);
            }
            else
            {
                Debug.LogWarning("[LeaderboardManager] Response kosong / status bukan success.");
                if (emptyLabel != null) emptyLabel.SetActive(true);
            }
        }
    }

    private void PopulateScrollView(List<LeaderboardEntry> entries)
    {
        // 1. Destroy old rows (from previous refresh)
        foreach (Transform child in leaderboardContainer)
            Destroy(child.gameObject);

        // 2. Instantiate one row per entry
        for (int i = 0; i < entries.Count; i++)
        {
            GameObject row = Instantiate(leaderboardPrefab, leaderboardContainer);
            var itemUI = row.GetComponent<LeaderboardItemUI>();
            if (itemUI != null)
                itemUI.SetData(i + 1, entries[i].username, entries[i].highScore);
        }

        Debug.Log($"[LeaderboardManager] Berhasil memuat {entries.Count} data leaderboard.");
    }
}