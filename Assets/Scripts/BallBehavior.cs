using UnityEngine;
using System.Collections.Generic;

public class BallBehavior : MonoBehaviour
{
    public float possessionDistance = 0.4f;
    public float kickForce = 5.0f;
    public float dribbleSpeed = 10f;
    public float possessionCooldown = 3f;
    public float verticalForceMultiplier = 2.0f;

    private Camera mainCamera;
    private Rigidbody rb;
    private Transform currentPlayer;
    private bool isPossessed = false;
    private bool isDragging = false;
    private float lastKickTime = -Mathf.Infinity;
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;

    public List<Transform> allyPlayers; // List of ally players

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isDragging = false;
                    break;

                case TouchPhase.Moved:
                    isDragging = true;
                    break;

                case TouchPhase.Ended:
                    touchEndPos = touch.position;
                    if (isPossessed && IsCurrentPlayerAnAlly() && !isDragging && touchStartPos == touchEndPos) {
                        KickBall();
                    }
                    break;
                case TouchPhase.Canceled:
                    break;
            }
        }
    }

    private bool IsCurrentPlayerAnAlly()
    {
        // Check if the current player possessing the ball is in the list of ally players
        return allyPlayers.Contains(currentPlayer);
    }

    private void HandlePossession()
    {
        if (currentPlayer == null) return;

        Vector3 targetPosition = currentPlayer.position + currentPlayer.forward * 0.5f;
        targetPosition.x += 0.1f;
        targetPosition.y = transform.position.y;
        targetPosition.z -= 0.2f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, dribbleSpeed * Time.deltaTime);
    }

    private void CheckForPossession()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, possessionDistance);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player") && Time.time > lastKickTime + possessionCooldown)
            {
                currentPlayer = hitCollider.transform;
                isPossessed = true;
                rb.isKinematic = true;

                Vector3 immediatePosition = currentPlayer.position + currentPlayer.forward * 0.5f;
                immediatePosition.y = transform.position.y;
                transform.position = immediatePosition;

                break;
            }
        }
    }

    private void KickBall()
    {
        isPossessed = false;
        rb.isKinematic = false;

        // Get the target position from the touch position
        Vector3 targetPosition = GetWorldPositionFromTouch(touchEndPos);

        // Calculate the time to target based on distance and desired speed
        float distance = Vector3.Distance(transform.position, targetPosition);
        float timeToTarget = distance / kickForce; // Adjust kickForce to control speed

        // Calculate the initial velocity needed to reach the target
        Vector3 initialVelocity = CalculateInitialVelocity(transform.position, targetPosition, timeToTarget);

        // Apply the calculated velocity to the ball
        rb.velocity = initialVelocity;

        // Record the time of the kick
        lastKickTime = Time.time;
    }

    private Vector3 CalculateInitialVelocity(Vector3 startPosition, Vector3 targetPosition, float timeToTarget)
    {
        Vector3 displacement = targetPosition - startPosition;
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        Vector3 horizontalVelocity = horizontalDisplacement / timeToTarget;

        // Calculate the maximum height the ball should reach
        float maxHeight = 1.0f; // The maximum height you want the ball to reach

        // Calculate the vertical velocity needed to reach the maximum height
        float verticalVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * (maxHeight - startPosition.y));

        // Ensure the vertical velocity is positive and adjusted for the time to target
        verticalVelocity = Mathf.Min(verticalVelocity, (displacement.y / timeToTarget) + (0.5f * Mathf.Abs(Physics.gravity.y) * timeToTarget * verticalForceMultiplier));

        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    private Vector3 GetWorldPositionFromTouch(Vector2 touchPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isPossessed && collision.transform.CompareTag("Player") && Time.time > lastKickTime + possessionCooldown)
        {
            currentPlayer = collision.transform;
            isPossessed = true;
            rb.isKinematic = true;

            Vector3 immediatePosition = currentPlayer.position + currentPlayer.forward * 0.5f;
            immediatePosition.y = transform.position.y;
            transform.position = immediatePosition;
        }
    }
}
