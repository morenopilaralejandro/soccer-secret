using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class SwipeDetector : MonoBehaviour
{
    public enum SwipeDirection { Up, Down, Left, Right }
    public event Action<SwipeDirection> OnSwipe;

    private Controls controls;
    private Vector2 startPos, endPos;
    private float startTime;
    private bool isSwiping = false;
    private float minSwipeDistance = 100f; // pixels
    private float maxSwipeTime = 0.5f; // seconds

    void Awake()
    {
        controls = new Controls();
    }

    void OnEnable()
    {
        controls.PlayerInput.Enable();
        controls.PlayerInput.PointerPress.started += OnPointerPressed;
        controls.PlayerInput.PointerPress.canceled += OnPointerReleased;
    }

    void OnDisable()
    {
        controls.PlayerInput.PointerPress.started -= OnPointerPressed;
        controls.PlayerInput.PointerPress.canceled -= OnPointerReleased;
        controls.PlayerInput.Disable();
    }

    private void OnPointerPressed(InputAction.CallbackContext ctx)
    {
        isSwiping = true;
        startTime = Time.time;
        startPos = controls.PlayerInput.PointerPosition.ReadValue<Vector2>();
    }

    private void OnPointerReleased(InputAction.CallbackContext ctx)
    {
        if (!isSwiping) return;
        isSwiping = false;

        endPos = controls.PlayerInput.PointerPosition.ReadValue<Vector2>();
        float swipeDuration = Time.time - startTime;
        Vector2 swipe = endPos - startPos;

        if (swipeDuration > maxSwipeTime) return;
        if (swipe.magnitude < minSwipeDistance) return;

        // Determine primary direction
        SwipeDirection dir;
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        {
            dir = (swipe.x > 0) ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            dir = (swipe.y > 0) ? SwipeDirection.Up : SwipeDirection.Down;
        }

        OnSwipe?.Invoke(dir);
    }
}
