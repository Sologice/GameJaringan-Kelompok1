using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// =====================================================================
//  ScoreManager — satu-satunya jembatan Unity ↔ endpoint skor backend.
//  Auto-create (tidak perlu drag ke scene), persist antar scene.
//  Dipakai oleh GameOverUI (Module 11) dan script lain yang perlu kirim
//  atau baca skor.
// =====================================================================
public class ScoreManager : MonoBehaviour
{
    public const string DefaultBaseURL = "http://localhost:5000/api";

    [SerializeField] private string baseURL = DefaultBaseURL;
    [SerializeField] private float timeoutSeconds = 10f;

    private static ScoreManager instance;

    public static ScoreManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Awake di ScoreManager akan set `instance` & DontDestroyOnLoad
                var go = new GameObject(nameof(ScoreManager));
                go.AddComponent<ScoreManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ----------------------------------------------------------------
    //  API PUBLIK
    // ----------------------------------------------------------------

    /// <summary>
    /// Kirim skor akhir match ke POST /api/score/save.
    /// onComplete SELALU dipanggil (sukses maupun gagal) supaya caller bisa lanjut
    /// ke langkah berikutnya tanpa nyangkut.
    /// </summary>
    public void SaveCurrentScore(int finalScore, Action onComplete = null)
    {
        int userId = PlayerSession.UserId;

        if (userId <= 0)
        {
            Debug.LogWarning("[ScoreManager] Tidak ada active user session. Skor tidak dikirim.");
            onComplete?.Invoke();
            return;
        }

        finalScore = Mathf.Max(0, finalScore);
        StartCoroutine(SaveScoreRoutine(userId, finalScore, onComplete));
    }

    /// <summary>
    /// Ambil rekor skor tertinggi pemain dari GET /api/score/highscore/:userId.
    /// Callback menerima 0 kalau belum ada data / gagal network.
    /// </summary>
    public void GetPersonalHighScore(Action<int> onCallback)
    {
        int userId = PlayerSession.UserId;

        if (userId <= 0)
        {
            Debug.LogWarning("[ScoreManager] Tidak ada active user session. High score default 0.");
            onCallback?.Invoke(0);
            return;
        }

        StartCoroutine(FetchHighScoreRoutine(userId, onCallback));
    }

    // ----------------------------------------------------------------
    //  IMPLEMENTASI
    // ----------------------------------------------------------------

    private IEnumerator SaveScoreRoutine(int userId, int score, Action onComplete)
    {
        var payload = new SaveScoreRequest { userId = userId, score = score };
        string json = JsonUtility.ToJson(payload);

        Debug.Log($"[ScoreManager] POST /score/save → {json}");

        using (var request = new UnityWebRequest(baseURL + "/score/save", "POST"))
        {
            request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.RoundToInt(timeoutSeconds);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var resp = JsonUtility.FromJson<SaveScoreResponse>(request.downloadHandler.text);
                    if (resp != null && resp.status == "success")
                    {
                        Debug.Log($"[ScoreManager] Skor tersimpan. scoreId={resp.scoreId}, score={score}, userId={userId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ScoreManager] Backend menolak skor: {resp?.message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ScoreManager] Parse SaveScoreResponse gagal: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[ScoreManager] Gagal simpan skor: HTTP {request.responseCode} — {request.error}");
            }
        }

        onComplete?.Invoke();
    }

    private IEnumerator FetchHighScoreRoutine(int userId, Action<int> onCallback)
    {
        string url = $"{baseURL}/score/highscore/{userId}";
        Debug.Log($"[ScoreManager] GET {url}");

        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = Mathf.RoundToInt(timeoutSeconds);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success
                && request.downloadHandler != null
                && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                int hs = 0;
                try
                {
                    var resp = JsonUtility.FromJson<HighScoreResponse>(request.downloadHandler.text);
                    hs = resp != null ? resp.highScore : 0;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ScoreManager] Parse HighScoreResponse gagal: {e.Message}");
                }

                Debug.Log($"[ScoreManager] High score user {userId} = {hs}");
                onCallback?.Invoke(hs);
            }
            else
            {
                Debug.LogError($"[ScoreManager] Gagal ambil high score: HTTP {request.responseCode} — {request.error}");
                onCallback?.Invoke(0);
            }
        }
    }
}