using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class KeyboardDetector : MonoBehaviour
{
    public event Action OnActionKey;
    public event Action OnPauseKey;
    public event Action OnDuelLogKey;

    private Controls controls;

    private bool actionKeyconsumed = false;
    public bool WasActionKeyConsumedThisFrame() => actionKeyconsumed;

    void Awake()
    {
        controls = new Controls();
    }

    void OnEnable()
    {
        controls.PlayerInput.Enable();
        controls.PlayerInput.ActionKey.performed += HandleActionKey;
        controls.PlayerInput.PauseKey.performed += HandlePauseKey;
        controls.PlayerInput.DuelLogKey.performed += HandleDuelLogKey;
    }

    void OnDisable()
    {
        controls.PlayerInput.ActionKey.performed -= HandleActionKey;
        controls.PlayerInput.PauseKey.performed -= HandlePauseKey;
        controls.PlayerInput.DuelLogKey.performed -= HandleDuelLogKey;
        controls.PlayerInput.Disable();
    }

    void HandleActionKey(InputAction.CallbackContext ctx)
    {
        actionKeyconsumed = false;
        OnActionKey?.Invoke();
    }

    public void ConsumeActionKey()
    {
        actionKeyconsumed = true;
    }

    void HandlePauseKey(InputAction.CallbackContext ctx)
    {
        OnPauseKey?.Invoke();
    }

    void HandleDuelLogKey(InputAction.CallbackContext ctx)
    {
        OnDuelLogKey?.Invoke();
    }
}
