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
        InputManager.LocalDeviceFilter = InputManager.InputDeviceFilter.Auto;

        if (NetworkManager.Singleton.StartHost())
        {
            string dev = UnityEngine.InputSystem.Gamepad.current != null ? "Gamepad" : "All (No Gamepad detected)";
            UpdateUIStatus($"Status: Connected as HOST [{dev}]");
        }
        else
            UpdateUIStatus("Status: Failed to Start Host");
    }

    private void OnClientButtonClicked()
    {
        InputManager.LocalDeviceFilter = InputManager.InputDeviceFilter.KeyboardOnly;

        if (NetworkManager.Singleton.StartClient())
            UpdateUIStatus("Status: Connected as CLIENT [Keyboard]");
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
