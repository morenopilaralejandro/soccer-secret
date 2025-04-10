using UnityEngine;

public class BallBehavior : MonoBehaviour
{
    public float possessionDistance = 0.4f; // Distance within which a player can possess the ball
    public float kickForce = 6.0f; // Base force applied when kicking the ball
    public float dribbleSpeed = 10f; // Speed of the ball when dribbling
    public float possessionCooldown = 3f; // Time in seconds before a player can repossess the ball
    public float verticalForceMultiplier = 2.0f; // Multiplier for vertical force to create a parabolic trajectory

    private Camera mainCamera;
    private Rigidbody rb;
    private Transform currentPlayer; // Reference to the current player possessing the ball
    private bool isPossessed = false;
    private float lastKickTime = -Mathf.Infinity; // Track the last time the ball was kicked
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Improve collision handling
    }

    void Update()
    {
        if (isPossessed)
        {
            HandlePossession();
        }
        else
        {
            CheckForPossession();
        }

        // Handle touch input for kicking
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                touchEndPos = touch.position;
                if (isPossessed && IsTouchOnPitch(touchStartPos) && IsTouchOnPitch(touchEndPos))
                {
                    // Only kick if the touch ends on a "pitch" object
                    KickBall();
                }
            }
        }
    }

    private bool IsTouchOnPitch(Vector2 touchPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Pitch"))
            {
                Debug.Log("Hit object: " + hit.collider.name);
                return true;
            }
        }
        return false;
    }

    private void HandlePossession()
    {
        if (currentPlayer == null) return;

        // Keep the ball at the player's feet, maintaining the current y position
        Vector3 targetPosition = currentPlayer.position + currentPlayer.forward * 0.5f;
        targetPosition.x += 0.1f;
        targetPosition.y = transform.position.y; // Maintain the current y position
        targetPosition.z -= 0.2f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, dribbleSpeed * Time.deltaTime);
    }

    private void CheckForPossession()
    {
        // Check if the ball can be possessed by any player
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, possessionDistance);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player") && Time.time > lastKickTime + possessionCooldown)
            {
                currentPlayer = hitCollider.transform;
                isPossessed = true;
                rb.isKinematic = true; // Disable physics immediately when possessed

                // Immediately adjust the ball's position to be in front of the player
                Vector3 immediatePosition = currentPlayer.position + currentPlayer.forward * 0.5f;
                immediatePosition.y = transform.position.y; // Maintain the current y position
                transform.position = immediatePosition;

                break;
            }
        }
    }

    private void KickBall()
    {
        isPossessed = false;
        rb.isKinematic = false; // Re-enable physics

        // Convert touch end position to world coordinates
        Vector3 targetPosition = GetWorldPositionFromTouch(touchEndPos);

        // Calculate the initial velocity required to reach the target position
        Vector3 initialVelocity = CalculateInitialVelocity(transform.position, targetPosition, 1.0f); // 1.0f is the time to reach the target

        // Apply the calculated initial velocity to the ball, scaled by kickForce
        rb.velocity = initialVelocity * kickForce;
        lastKickTime = Time.time; // Record the time of the kick
    }

    private Vector3 CalculateInitialVelocity(Vector3 startPosition, Vector3 targetPosition, float timeToTarget)
    {
        // Calculate the displacement
        Vector3 displacement = targetPosition - startPosition;

        // Calculate the horizontal velocity
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        Vector3 horizontalVelocity = horizontalDisplacement / timeToTarget;

        // Calculate the vertical velocity with a multiplier for parabolic trajectory
        float verticalVelocity = (displacement.y / timeToTarget) + (0.5f * Mathf.Abs(Physics.gravity.y) * timeToTarget * verticalForceMultiplier);

        // Combine horizontal and vertical velocities
        Vector3 initialVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        return initialVelocity;
    }

    private Vector3 GetWorldPositionFromTouch(Vector2 touchPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point; // Return the point where the ray hits an object
        }
        return Vector3.zero; // Return a default value if nothing is hit
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If the ball collides with a player while free, it can be possessed again
        if (!isPossessed && collision.transform.CompareTag("Player") && Time.time > lastKickTime + possessionCooldown)
        {
            currentPlayer = collision.transform;
            isPossessed = true;
            rb.isKinematic = true;

            // Immediately adjust the ball's position to be in front of the player
            Vector3 immediatePosition = currentPlayer.position + currentPlayer.forward * 0.5f;
            immediatePosition.y = transform.position.y; // Maintain the current y position
            transform.position = immediatePosition;
        }
    }
}
