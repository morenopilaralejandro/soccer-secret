using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera targetCamera; // If left null, will auto-use Camera.main
    public bool keepUpright = true;

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Vector3 lookDirection = transform.position - targetCamera.transform.position;

        if (keepUpright)
        {
            float originalX = transform.eulerAngles.x; // Save the current X rotation

            // Generate the "upright" rotation
            Quaternion uprightRotation = Quaternion.LookRotation(lookDirection, targetCamera.transform.up);

            // Apply that rotation
            transform.rotation = uprightRotation;

            // Restore the original X angle, keep the new Y and Z
            Vector3 fixedEuler = transform.eulerAngles;
            fixedEuler.x = originalX;
            transform.eulerAngles = fixedEuler;
        }
        else
        {
            // Standard billboard (can go upside down with 180° camera)
            transform.LookAt(targetCamera.transform);
        }
    }
}
