using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragDetector : MonoBehaviour
{
    public event Action<Vector2> OnDragStart;
    public event Action<Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;

    [Header("Drag Settings")]
    [Tooltip("Minimum screen distance before a drag is detected (in pixels)")]
    public float minDragDistance = 0.3f;

    private Controls controls;
    private bool isDragging = false;
    private bool dragDetected = false;
    private Vector2 dragStartPosition;

    void Awake()
    {
        controls = new Controls();
    }

    void OnEnable()
    {
        controls.PlayerInput.Enable();
        controls.PlayerInput.PointerPress.started += HandlePressStarted;
        controls.PlayerInput.PointerPress.canceled += HandlePressEnded;
    }

    void OnDisable()
    {
        controls.PlayerInput.PointerPress.started -= HandlePressStarted;
        controls.PlayerInput.PointerPress.canceled -= HandlePressEnded;
        controls.PlayerInput.Disable();
    }

    private void HandlePressStarted(InputAction.CallbackContext ctx)
    {
        dragStartPosition = controls.PlayerInput.PointerPosition.ReadValue<Vector2>();
        isDragging = true;
        dragDetected = false; // Reset for new press
    }

    private void HandlePressEnded(InputAction.CallbackContext ctx)
    {
        if (isDragging && dragDetected)
        {
            Vector2 pos = controls.PlayerInput.PointerPosition.ReadValue<Vector2>();
            OnDragEnd?.Invoke(pos);
        }
        isDragging = false;
        dragDetected = false;
    }

    void Update()
    {
        if (isDragging)
        {
            Vector2 currentPosition = controls.PlayerInput.PointerPosition.ReadValue<Vector2>();
            float distance = Vector2.Distance(currentPosition, dragStartPosition);

            if (!dragDetected)
            {
                if (distance >= minDragDistance)
                {
                    dragDetected = true;
                    OnDragStart?.Invoke(dragStartPosition);
                }
            }
            else // Drag already detected, send ongoing events
            {
                OnDrag?.Invoke(currentPosition);
            }
        }
    }
}
