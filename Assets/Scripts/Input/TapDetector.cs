using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TapDetector : MonoBehaviour
{
    public event Action<Vector2> OnTap; // Passes screen position of tap

    private Controls controls;
    private Vector2 tapStartPosition;
    private Vector2? pendingTapPosition = null;
    private float tapStartTime;
    private bool isTapping = false;

    [Header("Tap Sensitivity")]
    public float maxTapDuration = 0.2f;   // seconds
    public float maxTapMovement = 20f;    // pixels

    void Awake()
    {
        controls = new Controls();
    }

    void OnEnable()
    {
        controls.PlayerInput.Enable();
        controls.PlayerInput.PointerPress.started += HandleTapStarted;
        controls.PlayerInput.PointerPress.canceled += HandleTapCancelled;
    }

    void OnDisable()
    {
        controls.PlayerInput.PointerPress.started -= HandleTapStarted;
        controls.PlayerInput.PointerPress.canceled -= HandleTapCancelled;
        controls.PlayerInput.Disable();
    }

    void HandleTapStarted(InputAction.CallbackContext ctx)
    {
        tapStartPosition = controls.PlayerInput.PointerPosition.ReadValue<Vector2>();
        tapStartTime = Time.time;
        isTapping = true;
    }

    void HandleTapCancelled(InputAction.CallbackContext ctx)
    {
        if (!isTapping) return;

        isTapping = false;
        Vector2 tapEndPosition = controls.PlayerInput.PointerPosition.ReadValue<Vector2>();
        float tapDuration = Time.time - tapStartTime;
        float tapDistance = Vector2.Distance(tapStartPosition, tapEndPosition);

        if (tapDuration <= maxTapDuration && tapDistance <= maxTapMovement)
        {
            pendingTapPosition = tapEndPosition;
        }
    }

    void Update()
    {
        if (pendingTapPosition.HasValue)
        {
            Vector2 tapPos = pendingTapPosition.Value;
            pendingTapPosition = null; // Clear it

            // Now check UI - this will be "current"
            if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
            {
                OnTap?.Invoke(tapPos);
            }
        }
    }
}
