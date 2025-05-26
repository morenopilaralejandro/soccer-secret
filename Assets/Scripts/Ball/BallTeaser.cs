using UnityEngine;

public class BallTeaser : MonoBehaviour
{
    public Vector3 spinAxis = new Vector3(1, 1, 0); // Y-axis spin by default
    public float spinSpeed = 0.1f; // Adjust as needed

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Give an initial torque to spin
        rb.AddTorque(spinAxis.normalized * spinSpeed, ForceMode.Impulse);
    }
}
