using UnityEngine;

public class TapListener : MonoBehaviour
{
    public TapDetector tapDetector;

    void OnEnable()
    {
        if (tapDetector != null)
            tapDetector.OnTap += HandleTap;
    }

    void OnDisable()
    {
        if (tapDetector != null)
            tapDetector.OnTap -= HandleTap;
    }

    void HandleTap(Vector2 screenPosition)
    {
        Debug.Log("Tapped at: " + screenPosition);
        // Your tap handling logic here
    }
}
