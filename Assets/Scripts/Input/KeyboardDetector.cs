using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class KeyboardDetector : MonoBehaviour
{
    public event Action OnActionKey;

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
    }

    void OnDisable()
    {
        controls.PlayerInput.ActionKey.performed -= HandleActionKey;
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
}
