using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class BallBehavior : MonoBehaviour
{
    public float possessionDistance = 0.2f;
    public float kickForce = 4.0f;
    public float dribbleSpeed = 10f;
    public float possessionCooldown = 0.3f;
    public float verticalForceMultiplier = 2.0f;
    public Image crosshairImage;
    public float crosshairDisplayDuration = 0.2f;

    private Camera mainCamera;
    private Rigidbody rb;
    private GameObject currentPlayer;
    private bool isPossessed = false;
    private bool isDragging = false;
    private float lastKickTime = -Mathf.Infinity;
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private Coroutine hideCrosshairCoroutine;

    public List<GameObject> allyPlayers;
    public List<GameObject> oppPlayers;

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
                    if (isPossessed && IsCurrentPlayerAnAlly() 
                            && !isDragging && touchStartPos == touchEndPos 
                            && !GameManager.Instance.IsMovementFrozen) {
                        crosshairImage.transform.position = touchEndPos;
                        crosshairImage.enabled = true;
                        KickBall();
                    }
                    if (hideCrosshairCoroutine != null)
                    {
                        StopCoroutine(hideCrosshairCoroutine);
                    }
                    hideCrosshairCoroutine = StartCoroutine(HideCrosshairAfterDelay());
                    break;
                case TouchPhase.Canceled:
                    hideCrosshairCoroutine = StartCoroutine(HideCrosshairAfterDelay());
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

        Vector3 targetPosition = currentPlayer.transform.position + currentPlayer.transform.forward * 0.5f;
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
                currentPlayer = hitCollider.gameObject;
                isPossessed = true;
                rb.isKinematic = true;

                Vector3 immediatePosition = currentPlayer.transform.position + currentPlayer.transform.forward * 0.5f;
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

        // Convert the touch position to a world position
        // Use the camera's position to determine the correct plane
        Vector3 touchPosition = mainCamera.ScreenToWorldPoint(new Vector3(touchEndPos.x, touchEndPos.y, mainCamera.nearClipPlane));

        // Calculate the direction from the ball to the touch position on the x-z plane
        Vector3 direction = (touchPosition - transform.position).normalized;

        // Ensure the direction's y-component is set to allow vertical movement
        // If you want to limit the vertical movement, you can clamp the y-component
        direction.y = Mathf.Clamp(direction.y, -1.0f, 1.0f); // Adjust the clamp values as needed

        // Debugging: Log the calculated positions and direction
        Debug.Log($"Touch World Position: {touchPosition}, Ball Position: {transform.position}, Direction: {direction}");

        // Visualize the direction in the scene view
        Debug.DrawLine(transform.position, transform.position + direction * 2, Color.red, 5.0f); // Duration set to 5 seconds

        // Apply a force in the direction of the touch position
        rb.AddForce(direction * kickForce, ForceMode.Impulse);

        // Record the time of the kick
        lastKickTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isPossessed && collision.transform.CompareTag("Player") && Time.time > lastKickTime + possessionCooldown)
        {
            currentPlayer = collision.gameObject;
            isPossessed = true;
            rb.isKinematic = true;

            Vector3 immediatePosition = currentPlayer.transform.position + currentPlayer.transform.forward * 0.5f;
            immediatePosition.y = transform.position.y;
            transform.position = immediatePosition;
        }
    }

    private void OnTriggerEnter(Collider otherPlayer)
    {
        Debug.Log("OnTriggerEnter");

        if (!isPossessed) return;

        // Get the tag of the other collider
        string otherPlayerTag = otherPlayer.tag;

        // Get the tag of the current player in possession (from their child collider)
        string currentPlayerTag = null;
        foreach (Transform child in currentPlayer.transform)
        {
            if (child.CompareTag("Ally") || child.CompareTag("Opp"))
            {
                currentPlayerTag = child.tag;
                break;
            }
        }
        Debug.Log(currentPlayerTag + ", " + otherPlayerTag);
        // If tags are different and both are either "Ally" or "Opp"
        if (currentPlayerTag != null && (otherPlayerTag == "Ally" || otherPlayerTag == "Opp") && currentPlayerTag != otherPlayerTag)
        {            
            GameManager.Instance.HandleDuel(currentPlayer, otherPlayer.transform.root.gameObject, 0);
        }
    }

    private IEnumerator HideCrosshairAfterDelay()
    {
        // Wait for the specified duration
        yield return new WaitForSeconds(crosshairDisplayDuration);
        crosshairImage.enabled = false;
    }
}
