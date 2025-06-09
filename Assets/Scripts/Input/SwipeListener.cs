using UnityEngine;

public class SwipeListener : MonoBehaviour
{
    public SwipeDetector swipeDetector;

    void OnEnable()
    {
        if (swipeDetector != null)
            swipeDetector.OnSwipe += HandleSwipe;
    }

    void OnDisable()
    {
        if (swipeDetector != null)
            swipeDetector.OnSwipe -= HandleSwipe;
    }

    void HandleSwipe(SwipeDetector.SwipeDirection direction)
    {
        Debug.Log("Detected Swipe: " + direction);
        // Add your logic depending on swipe direction!
    }
}
