using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI Login + Register di AuthScene. Auto-build (pakai UIFactory), taruh di GameObject mana pun.
/// Alur: login sukses → PlayerSession terisi → LoadScene(mainSceneName).
/// </summary>
public class AuthSceneUI : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private string mainSceneName = "MainScene";
    [Tooltip("Centang untuk menampilkan tombol 'Skip Login (Dev)' — hanya untuk testing cepat.")]
    [SerializeField] private bool allowSkipLogin = false;

    private Canvas canvas;
    private TMP_InputField usernameInput;
    private TMP_InputField passwordInput;
    private TextMeshProUGUI statusLabel;
    private Button loginButton;
    private Button registerButton;
    private Button playButton;      // muncul setelah login sukses
    private Button skipButton;
    private bool busy;

    private void Awake()
    {
        BuildUI();
        Debug.Log($"[AuthScene] Awake — IsLoggedIn = {PlayerSession.IsLoggedIn}, UserId = {PlayerSession.UserId}, Username = {PlayerSession.Username}");
        
        // Kalau sudah login sebelumnya (PlayerPrefs persist), langsung ke MainScene
        if (PlayerSession.IsLoggedIn)
        {
            Debug.Log($"[AuthScene] Sudah login (user {PlayerSession.UserId}). Langsung ke {mainSceneName}.");
            SceneManager.LoadScene(mainSceneName);
        }
    }

    // ---------------------------------------------------------------- UI

    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("AuthCanvas", 10);

        var bg = UIFactory.MakePanel(canvas.transform, "Background", new Color(0.05f, 0.06f, 0.1f, 1f));
        UIFactory.Stretch(bg);

        // Judul
        var title = UIFactory.MakeText(bg, "ULTIMATE PONG", 96, Color.white, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(1200, 120), new Vector2(0, 320));

        var subtitle = UIFactory.MakeText(bg, "Login untuk melanjutkan", 32, new Color(0.75f, 0.78f, 0.9f),
            TextAlignmentOptions.Center);
        UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(1200, 50), new Vector2(0, 220));

        // Input username
        usernameInput = UIFactory.MakeInputField(bg, "UsernameInput", "Username", isPassword: false);
        UIFactory.Place((RectTransform)usernameInput.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(560, 70), new Vector2(0, 100));

        // Input password
        passwordInput = UIFactory.MakeInputField(bg, "PasswordInput", "Password", isPassword: true);
        UIFactory.Place((RectTransform)passwordInput.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(560, 70), new Vector2(0, 10));

        // Tombol Login
        loginButton = UIFactory.MakeButton(bg, "LoginButton", new Color(0.2f, 0.6f, 0.9f), OnLoginClicked);
        UIFactory.Place((RectTransform)loginButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(270, 70), new Vector2(-140, -100));
        var loginLabel = UIFactory.MakeText(loginButton.transform, "LOGIN", 34, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(loginLabel.rectTransform);

        // Tombol Register
        registerButton = UIFactory.MakeButton(bg, "RegisterButton", new Color(0.35f, 0.35f, 0.45f), OnRegisterClicked);
        UIFactory.Place((RectTransform)registerButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(270, 70), new Vector2(140, -100));
        var regLabel = UIFactory.MakeText(registerButton.transform, "REGISTER", 34, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(regLabel.rectTransform);

        // Tombol Play (disabled, muncul setelah login sukses)
        playButton = UIFactory.MakeButton(bg, "PlayButton", new Color(0.2f, 0.7f, 0.35f), OnPlayClicked);
        UIFactory.Place((RectTransform)playButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(420, 80), new Vector2(0, -220));
        var playLabel = UIFactory.MakeText(playButton.transform, "LANJUTKAN →", 38, Color.white, TextAlignmentOptions.Center);
        UIFactory.Stretch(playLabel.rectTransform);
        playButton.gameObject.SetActive(false);

        // Status label (feedback ke user)
        statusLabel = UIFactory.MakeText(bg, "", 28, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Center);
        UIFactory.Place(statusLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(1200, 60), new Vector2(0, 60));

        // Tombol skip (dev)
        if (allowSkipLogin)
        {
            skipButton = UIFactory.MakeButton(bg, "SkipButton", new Color(0.4f, 0.2f, 0.2f), OnSkipClicked);
            UIFactory.Place((RectTransform)skipButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(260, 50), new Vector2(-20, -20));
            var skipLabel = UIFactory.MakeText(skipButton.transform, "Skip Login (Dev)", 22, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(skipLabel.rectTransform);
        }
    }

    // ---------------------------------------------------------------- Buttons

    private void OnLoginClicked()
    {
        if (busy) return;
        SetBusy(true, "Login...");
        AuthManager.Instance.Login(usernameInput.text, passwordInput.text, OnAuthResult);
    }

    private void OnRegisterClicked()
    {
        if (busy) return;
        SetBusy(true, "Mendaftarkan...");
        AuthManager.Instance.Register(usernameInput.text, passwordInput.text, OnAuthResult);
    }

    private void OnAuthResult(bool success, string message, int userId)
    {
        SetBusy(false, message);

        if (success)
        {
            // Sembunyikan form input, tampilkan tombol "Lanjutkan"
            usernameInput.gameObject.SetActive(false);
            passwordInput.gameObject.SetActive(false);
            loginButton.gameObject.SetActive(false);
            registerButton.gameObject.SetActive(false);
            playButton.gameObject.SetActive(true);
        }
    }

    private void OnPlayClicked()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    private void OnSkipClicked()
    {
        // Fallback dev: buat user dengan id acak biar ScoreManager tidak error
        int devId = Random.Range(1000, 9999);
        PlayerSession.Save(devId, "dev_" + devId, "");
        Debug.LogWarning($"[AuthScene] Skip login — memakai dev userId={devId}.");
        SceneManager.LoadScene(mainSceneName);
    }

    private void SetBusy(bool isBusy, string message)
    {
        busy = isBusy;
        if (loginButton != null)    loginButton.interactable    = !isBusy;
        if (registerButton != null) registerButton.interactable = !isBusy;
        if (statusLabel != null)
        {
            statusLabel.text = message ?? "";
            statusLabel.color = isBusy
                ? new Color(0.75f, 0.78f, 0.9f)
                : (message != null && (message.Contains("berhasil") || message.Contains("Selamat"))
                    ? new Color(0.45f, 1f, 0.55f)
                    : new Color(1f, 0.6f, 0.6f));
        }
    }
}