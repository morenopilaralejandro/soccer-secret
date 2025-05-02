using UnityEngine;

public class CameraFOVAdjuster : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float defaultFOV = 60f; // Default field of view
    [SerializeField] private float targetAspect = 9f / 16f; // Target aspect ratio (e.g., 9:16 for portrait)

    void Start()
    {
        AdjustCameraFOV();
    }

    void AdjustCameraFOV()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleFactor = windowAspect / targetAspect;

        // Adjust the field of view based on the aspect ratio
        mainCamera.fieldOfView = defaultFOV * scaleFactor;
    }
}
