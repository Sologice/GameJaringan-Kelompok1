using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class InputManager : MonoBehaviour
{
    public enum InputDeviceFilter
    {
        Auto,          // Host uses Gamepad (if connected), Client uses Keyboard
        GamepadOnly,   // Strictly listen to Gamepad
        KeyboardOnly,  // Strictly listen to Keyboard
        All            // Listen to both Keyboard & Gamepad
    }

    public static InputDeviceFilter LocalDeviceFilter = InputDeviceFilter.Auto;

    private PlayerInputs PIA;
    public Vector2 MoveInput;

    public event Action OnMove, OffMove, OnSelect, OnBack, OnTestA, OnTestB, OnServe;
    private Action<InputAction.CallbackContext> onMove, offMove, onSelect, onBack, onTestA, onTestB, onServe;
    public event Action<int> OnCard;
    private Action<InputAction.CallbackContext> onCard1, onCard2, onCard3;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        PIA = new PlayerInputs();
        ApplyDeviceFilter();

        PIA.Player.Move.performed   += onMove   = ctx => { MoveInput = ctx.ReadValue<Vector2>(); OnMove?.Invoke(); };
        PIA.Player.Move.canceled    += offMove  = ctx => { MoveInput = Vector2.zero; OffMove?.Invoke(); };
        PIA.Player.Select.performed += onSelect = ctx => OnSelect?.Invoke();
        PIA.Player.Back.performed   += onBack   = ctx => OnBack?.Invoke();

        PIA.Player.TestA.performed  += onTestA  = ctx => OnTestA?.Invoke();
        PIA.Player.TestB.performed  += onTestB  = ctx => OnTestB?.Invoke();
        PIA.Player.Serve.performed  += onServe  = ctx => OnServe?.Invoke();

        PIA.Player.Card1.performed  += onCard1  = ctx => OnCard?.Invoke(0);
        PIA.Player.Card2.performed  += onCard2  = ctx => OnCard?.Invoke(1);
        PIA.Player.Card3.performed  += onCard3  = ctx => OnCard?.Invoke(2);

        PIA.Player.Enable();
    }

    public void ApplyDeviceFilter()
    {
        if (PIA == null) return;

        InputDeviceFilter filter = LocalDeviceFilter;

        if (filter == InputDeviceFilter.Auto)
        {
            bool isHost = playerController == null || playerController.IsOwnedByServer;
            if (isHost && Gamepad.current != null)
                filter = InputDeviceFilter.GamepadOnly;
            else if (!isHost)
                filter = InputDeviceFilter.KeyboardOnly;
            else
                filter = InputDeviceFilter.All;
        }

        switch (filter)
        {
            case InputDeviceFilter.GamepadOnly:
                if (Gamepad.current != null)
                {
                    PIA.asset.devices = new ReadOnlyArray<InputDevice>(new InputDevice[] { Gamepad.current });
                    Debug.Log($"[InputManager] Player Locked to Gamepad: {Gamepad.current.displayName}");
                }
                else
                {
                    PIA.asset.devices = null;
                    Debug.LogWarning("[InputManager] Gamepad Not Found! Player listening to All Devices.");
                }
                break;

            case InputDeviceFilter.KeyboardOnly:
                if (Keyboard.current != null)
                {
                    PIA.asset.devices = new ReadOnlyArray<InputDevice>(new InputDevice[] { Keyboard.current });
                    Debug.Log("[InputManager] Player Locked to Keyboard.");
                }
                else
                {
                    PIA.asset.devices = null;
                }
                break;

            default:
                PIA.asset.devices = null;
                Debug.Log("[InputManager] Player listening to All Devices.");
                break;
        }
    }

    private void OnDisable()
    {
        PIA.Player.Move.performed   -= onMove;
        PIA.Player.Move.canceled    -= offMove;
        PIA.Player.Select.performed -= onSelect;
        PIA.Player.Back.performed   -= onBack;

        PIA.Player.TestA.performed  -= onTestA;
        PIA.Player.TestB.performed  -= onTestB;
        PIA.Player.Serve.performed  -= onServe;

        PIA.Player.Card1.performed  -= onCard1;
        PIA.Player.Card2.performed  -= onCard2;
        PIA.Player.Card3.performed  -= onCard3;

        PIA.Player.Disable();
        PIA.Dispose();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnableBackgroundInput()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
    }
}   