using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkManagerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Cards")]
    [Tooltip("Optional. If set, Host / Client stay hidden until the player confirmed a 6-card deck.")]
    [SerializeField] private DeckBuilderUI deckBuilder;

    private void Awake()
    {
        hostButton.onClick.AddListener(OnHostButtonClicked);
        clientButton.onClick.AddListener(OnClientButtonClicked);

        if (deckBuilder != null && !deckBuilder.IsConfirmed)
        {
            SetConnectButtons(false);
            deckBuilder.Confirmed += OnDeckConfirmed;
        }
    }

    private void OnDestroy()
    {
        if (deckBuilder != null) deckBuilder.Confirmed -= OnDeckConfirmed;
    }

    private void OnDeckConfirmed()
    {
        SetConnectButtons(true);
    }

    private void SetConnectButtons(bool visible)
    {
        hostButton.gameObject.SetActive(visible);
        clientButton.gameObject.SetActive(visible);
    }

    private void OnHostButtonClicked()
    {
        if (NetworkManager.Singleton.StartHost())
            UpdateUIStatus("Status: Connected as HOST");
        else
            UpdateUIStatus("Status: Failed to Start Host");
    }

    private void OnClientButtonClicked()
    {
        if (NetworkManager.Singleton.StartClient())
            UpdateUIStatus("Status: Connected as CLIENT");
        else
            UpdateUIStatus("Status: Failed to Start Client");
    }

    private void UpdateUIStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        SetConnectButtons(false);
    }
}
