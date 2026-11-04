using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Service autentikasi (register + login) ke backend Express.
/// Bukan MonoBehaviour yang perlu di-drag ke scene — AuthSceneUI cukup pakai AuthManager.Instance.
/// Taruh di GameObject mana pun di AuthScene.
/// </summary>
public class AuthManager : MonoBehaviour
{
    [SerializeField] private string baseURL = "http://localhost:5000/api";

    public static AuthManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Daftar akun baru. onResult(success, message, userId).
    /// userId = -1 kalau gagal.
    /// </summary>
    public void Register(string username, string password, Action<bool, string, int> onResult)
    {
        StartCoroutine(RegisterRoutine(username, password, onResult));
    }

    /// <summary>
    /// Login. onResult(success, message, userId).
    /// Kalau sukses, PlayerSession otomatis diisi (userId, username, token).
    /// </summary>
    public void Login(string username, string password, Action<bool, string, int> onResult)
    {
        StartCoroutine(LoginRoutine(username, password, onResult));
    }

    // ---------------------------------------------------------------- routines

    private IEnumerator RegisterRoutine(string username, string password, Action<bool, string, int> onResult)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            onResult?.Invoke(false, "Username dan password wajib diisi.", -1);
            yield break;
        }

        var payload = new RegisterRequest { username = username, password = password };
        yield return PostJSON("/register", JsonUtility.ToJson(payload), (success, responseText) =>
        {
            if (!success)
            {
                onResult?.Invoke(false, ExtractMessage(responseText, "Registrasi gagal."), -1);
                return;
            }

            var resp = JsonUtility.FromJson<RegisterResponse>(responseText);
            if (resp != null && resp.status == "success")
                onResult?.Invoke(true, $"Akun {username} berhasil dibuat. Silakan login.", resp.userId);
            else
                onResult?.Invoke(false, ExtractMessage(responseText, "Registrasi gagal."), -1);
        });
    }

    private IEnumerator LoginRoutine(string username, string password, Action<bool, string, int> onResult)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            onResult?.Invoke(false, "Username dan password wajib diisi.", -1);
            yield break;
        }

        var payload = new LoginRequest { username = username, password = password };
        yield return PostJSON("/login", JsonUtility.ToJson(payload), (success, responseText) =>
        {
            if (!success)
            {
                onResult?.Invoke(false, ExtractMessage(responseText, "Login gagal."), -1);
                return;
            }

            var resp = JsonUtility.FromJson<LoginResponse>(responseText);
            if (resp != null && resp.status == "success" && resp.userId > 0)
            {
                PlayerSession.Save(resp.userId, username, resp.token ?? "");
                onResult?.Invoke(true, $"Selamat datang, {username}!", resp.userId);
            }
            else
            {
                onResult?.Invoke(false, ExtractMessage(responseText, "Login gagal."), -1);
            }
        });
    }

    private IEnumerator PostJSON(string path, string json, Action<bool, string> onDone)
    {
        using (var request = new UnityWebRequest(baseURL + path, "POST"))
        {
            request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            bool ok = request.result == UnityWebRequest.Result.Success;
            string body = request.downloadHandler != null ? request.downloadHandler.text : "";
            onDone?.Invoke(ok, body);
        }
    }

    /// <summary>Ambil field "message" dari response JSON, atau pakai fallback.</summary>
    private string ExtractMessage(string json, string fallback)
    {
        if (string.IsNullOrEmpty(json)) return fallback;
        try
        {
            var generic = JsonUtility.FromJson<RegisterResponse>(json); // bentuk { status, message }
            if (generic != null && !string.IsNullOrEmpty(generic.message)) return generic.message;
        }
        catch { /* ignore */ }
        return fallback;
    }
}