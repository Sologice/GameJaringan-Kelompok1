using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInputs PIA;
    public Vector2 MoveInput;

    public event Action OnMove, OffMove, OnSelect, OnBack, OnTestA, OnTestB, OnServe;
    private Action<InputAction.CallbackContext> onMove, offMove, onSelect, onBack, onTestA, onTestB, onServe;

    private void OnEnable()
    {
        PIA = new();

        PIA.Player.Move.performed   += onMove   = ctx => { MoveInput = ctx.ReadValue<Vector2>(); OnMove?.Invoke(); };
        PIA.Player.Move.canceled    += offMove  = ctx => { MoveInput = Vector2.zero; OffMove?.Invoke(); };
        PIA.Player.Select.performed += onSelect = ctx => { OnSelect?.Invoke(); };
        PIA.Player.Back.performed   += onBack   = ctx => { OnBack?.Invoke(); };
        PIA.Player.TestA.performed  += onTestA  = ctx => { OnTestA?.Invoke(); };
        PIA.Player.TestB.performed  += onTestB  = ctx => { OnTestB?.Invoke(); };
        PIA.Player.Serve.performed  += onServe  = ctx => { OnServe?.Invoke(); };

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

        PIA.Player.Disable();
        PIA.Dispose();
    }
}