using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthSceneUINew : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private string mainSceneName = "MainScene";
    [Tooltip("Centang untuk menampilkan tombol 'Skip Login (Dev)' — hanya untuk testing cepat.")]
    [SerializeField] private bool allowSkipLogin = false;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button skipButton;

    [Header("Menu Panel")]
    [SerializeField] private GameObject backgroundPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private bool busy;

    private void Awake()
    {
        if (allowSkipLogin)
            skipButton.gameObject.SetActive(true);
        else
            skipButton.gameObject.SetActive(false);

        Debug.Log($"[AuthScene] Awake — IsLoggedIn = {PlayerSession.IsLoggedIn}, UserId = {PlayerSession.UserId}, Username = {PlayerSession.Username}");
        
        // Kalau sudah login sebelumnya (PlayerPrefs persist), langsung ke MainScene
        if (PlayerSession.IsLoggedIn)
        {
            Debug.Log($"[AuthScene] Sudah login (user {PlayerSession.UserId}). Langsung ke {mainSceneName}.");
            SceneManager.LoadScene(mainSceneName);
        }
    }

    // ---------------------------------------------------------------- Buttons

    public void OnLoginClicked()
    {
        if (busy) return;
        SetBusy(true, "Login...");
        AuthManager.Instance.Login(usernameInput.text, passwordInput.text, OnAuthResult);
    }

    public void OnRegisterClicked()
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

    public void OnLeaderboardClicked()
    {
        backgroundPanel.SetActive(false);
        leaderboardPanel.SetActive(true);
    }

    public void OnCloseClicked()
    {
        backgroundPanel.SetActive(true);
        leaderboardPanel.SetActive(false);
    }

    public void OnPlayClicked()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    public void OnSkipClicked()
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