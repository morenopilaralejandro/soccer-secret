using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera targetCamera; // If left null, will auto-use Camera.main

    // If you want the sprite to always remain upright on the screen, set this to true!
    public bool keepUpright = true;

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        // Calculate the look direction and "up" (for upright billboarding)
        Vector3 lookDirection = transform.position - targetCamera.transform.position;

        if (keepUpright)
        {
            // 'Up' is the camera's up direction—prevents upside-down look
            transform.rotation = Quaternion.LookRotation(lookDirection, targetCamera.transform.up);
        }
        else
        {
            // Standard billboard (can go upside down with 180° camera)
            transform.LookAt(targetCamera.transform);
        }
    }
}
