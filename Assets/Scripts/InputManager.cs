using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    private PlayerInputs PIA;
    public Vector2 MoveInput;

    public event Action OnMove, OffMove, OnSelect, OnBack;
    private Action<InputAction.CallbackContext> onMove, offMove, onSelect, onBack;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
        PIA = new();
    }

    private void OnEnable()
    {
        PIA.Player.Move.performed   += onMove   = ctx =>    { MoveInput = ctx.ReadValue<Vector2>(); OnMove?.Invoke(); };
        PIA.Player.Move.canceled    += offMove  = ctx =>    { MoveInput = Vector2.zero; OffMove?.Invoke(); };
        PIA.Player.Select.performed += onSelect = ctx =>    { OnSelect?.Invoke(); };
        PIA.Player.Back.performed   += onBack   = ctx =>    { OnBack?.Invoke(); };

        PIA.Player.Enable();
    }

    private void OnDisable()
    {
        PIA.Player.Move.performed   -= onMove;
        PIA.Player.Move.canceled    -= offMove;
        PIA.Player.Select.performed -= onSelect;
        PIA.Player.Back.performed   -= onBack;

        PIA.Player.Disable();
        PIA.Dispose();
    }
}