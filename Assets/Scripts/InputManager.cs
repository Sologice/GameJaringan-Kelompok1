using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInputs PIA;
    public Vector2 MoveInput;

    public event Action OnMove, OffMove, OnSelect, OnBack, OnTestA, OnTestB, OnServe;
    private Action<InputAction.CallbackContext> onMove, offMove, onSelect, onBack, onTestA, onTestB, onServe;

    /// <summary>Card slot pressed: 0 = Card1, 1 = Card2, 2 = Card3</summary>
    public event Action<int> OnCard;
    private Action<InputAction.CallbackContext> onCard1, onCard2, onCard3;

    private void OnEnable()
    {
        PIA = new PlayerInputs();

        PIA.Player.Move.performed   += onMove   = ctx => { MoveInput = ctx.ReadValue<Vector2>(); OnMove?.Invoke(); };
        PIA.Player.Move.canceled    += offMove  = ctx => { MoveInput = Vector2.zero; OffMove?.Invoke(); };
        PIA.Player.Select.performed += onSelect = ctx => OnSelect?.Invoke();
        PIA.Player.Back.performed   += onBack   = ctx => OnBack?.Invoke();
        PIA.Player.TestA.performed  += onTestA  = ctx => OnTestA?.Invoke();
        PIA.Player.TestB.performed  += onTestB  = ctx => OnTestB?.Invoke();
        PIA.Player.Serve.performed  += onServe  = ctx => OnServe?.Invoke();

        // Subscribing card actions defined in the Input Action asset
        PIA.Player.Card1.performed  += onCard1  = ctx => OnCard?.Invoke(0);
        PIA.Player.Card2.performed  += onCard2  = ctx => OnCard?.Invoke(1);
        PIA.Player.Card3.performed  += onCard3  = ctx => OnCard?.Invoke(2);

        PIA.Player.Enable();
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
}   