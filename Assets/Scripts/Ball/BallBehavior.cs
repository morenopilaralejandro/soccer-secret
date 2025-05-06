using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BallBehavior : MonoBehaviour
{
    [SerializeField] private float possessionDistance = 0.2f;
    [SerializeField] private float kickForce = 2.0f;
    [SerializeField] private float maxForceDistance = 3f;
    [SerializeField] private float maxVelocity = 10.0f;
    [SerializeField] private float dribbleSpeed = 10f;
    [SerializeField] private float possessionCooldown = 0.3f;
    [SerializeField] private float verticalForceMultiplier = 2.0f;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private float crosshairDisplayDuration = 0.2f;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rigidbody rb;
    [SerializeField] public GameObject possesionPlayer;
    [SerializeField] private bool isPossessed = false;
    [SerializeField] private bool isDragging = false;
    [SerializeField] private float lastKickTime = -Mathf.Infinity;
    [SerializeField] private Vector2 touchStartPos;
    [SerializeField] private Vector2 touchEndPos;
    [SerializeField] private Coroutine hideCrosshairCoroutine;

    [SerializeField] private List<GameObject> allyPlayers;
    [SerializeField] private List<GameObject> oppPlayers;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (isPossessed)
        {
            HandlePossession();
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

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
                        KickBallTo(touchEndPos);
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
        return allyPlayers.Contains(possesionPlayer);
    }

    private void HandlePossession()
    {
        if (possesionPlayer == null) return;

        Vector3 targetPosition = possesionPlayer.transform.position + possesionPlayer.transform.forward * 0.5f;
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
                possesionPlayer = hitCollider.gameObject;
                isPossessed = true;
                rb.isKinematic = true;

                Vector3 immediatePosition = possesionPlayer.transform.position + possesionPlayer.transform.forward * 0.5f;
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
        Vector3 direction = touchPosition - transform.position;

        Vector3 rayDirection = direction.normalized;
        float kickDistance = direction.magnitude; // or a fixed max check distance


        bool presenceBlocking = false;

        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Presence");
        if (Physics.Raycast(transform.position, rayDirection, out hit, kickDistance, layerMask))
        {
            if (hit.collider.CompareTag("Presence"))
            {
                GameObject opponent = hit.collider.transform.parent.gameObject;
                // Optionally: test if this is in oppPlayers
                presenceBlocking = true;
            }
        }

        // If presence is blocking, adjust kick to go higher and slower
        float actualKickForce = kickForce;
        float actualMaxVelocity = maxVelocity;
        if (presenceBlocking)
        {
            // Example values; tweak as needed for your game feel
            direction.y = Mathf.Clamp(direction.y, 0.9f, 0.9f); // vertical force is always capped below 1
            actualKickForce *= 0.9f;     // Less force = slower
            actualMaxVelocity *= 5f;   // Lower max velocity
        }
        else
        {
            direction.y = Mathf.Clamp(direction.y, 0f, 0.7f); // vertical force is always capped below 1
            // actualKickForce and actualMaxVelocity unchanged (normal fast kick)
        }

        // Clamp the direction afterwards, so the modification above is respected
        if (direction.magnitude > maxForceDistance)
            direction = direction.normalized * maxForceDistance;

        // Visualize and debug
        Debug.Log($"Presence block: {presenceBlocking}, Final Direction: {direction}, Kick Force: {actualKickForce}");
        Debug.DrawLine(transform.position, transform.position + direction * 2, presenceBlocking ? Color.yellow : Color.red, 5.0f);

        // Kick!
        rb.AddForce(direction * actualKickForce, ForceMode.Impulse);

        if (rb.velocity.magnitude > actualMaxVelocity)
            rb.velocity = rb.velocity.normalized * actualMaxVelocity;

        lastKickTime = Time.time;


    }

    public void KickBallTo(Vector2 targetScreenPosition) //touchEnd
    {
        isPossessed = false;
        rb.isKinematic = false;

        // Convert the screen position to a world position
        Vector3 touchPosition = mainCamera.ScreenToWorldPoint(new Vector3(targetScreenPosition.x, targetScreenPosition.y, mainCamera.nearClipPlane));

        Vector3 direction = touchPosition - transform.position;
        Vector3 rayDirection = direction.normalized;
        float kickDistance = direction.magnitude;

        bool presenceBlocking = false;

        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Presence");
        if (Physics.Raycast(transform.position, rayDirection, out hit, kickDistance, layerMask))
        {
            if (hit.collider.CompareTag("Presence"))
            {
                GameObject rootObj = hit.collider.transform.parent.gameObject;
                if (!rootObj.GetComponent<Player>().IsAlly) {
                    presenceBlocking = true;
                }
            }
        }

        float actualKickForce = kickForce;
        float actualMaxVelocity = maxVelocity;
        if (presenceBlocking)
        {
            direction.y = Mathf.Clamp(direction.y, 0.9f, 0.9f);
            actualKickForce *= 0.9f;
            actualMaxVelocity *= 5f;
        }
        else
        {
            direction.y = Mathf.Clamp(direction.y, 0f, 0.7f);
        }

        if (direction.magnitude > maxForceDistance)
            direction = direction.normalized * maxForceDistance;

        Debug.Log($"Presence block: {presenceBlocking}, Final Direction: {direction}, Kick Force: {actualKickForce}");
        Debug.DrawLine(transform.position, transform.position + direction * 2, presenceBlocking ? Color.yellow : Color.red, 5.0f);

        rb.AddForce(direction * actualKickForce, ForceMode.Impulse);

        if (rb.velocity.magnitude > actualMaxVelocity)
            rb.velocity = rb.velocity.normalized * actualMaxVelocity;

        lastKickTime = Time.time;

        ReleasePossession();        
    }

    public void GainPossession(GameObject player)
    {
            Debug.Log("Possession granted to: " + player.name);
            possesionPlayer = player;
            isPossessed = true;
            rb.isKinematic = true;

            Vector3 immediatePosition = possesionPlayer.transform.position + possesionPlayer.transform.forward * 0.5f;
            immediatePosition.y = transform.position.y;
            transform.position = immediatePosition;
    }

    public void ReleasePossession()
    {
        Debug.Log("Possession taken from: " + possesionPlayer.name);
        currentPlayer = null;
        isPossessed = false;
        rb.isKinematic = false;
    }

    private void OnTriggerEnter(Collider collider)
    {
        GameObject rootObj = collider.transform.root.gameObject;
        Debug.Log("BallBehavior OnTriggerEnter: " + rootObj.name + " (Tag: " + rootObj.tag + ")");
        if (!isPossessed && rootObj.CompareTag("Player") && Time.time > lastKickTime + possessionCooldown)
        {
            GainPossession(rootObj);
        }









        /*
        if (isPossessed && !otherPlayer.CompareTag("Player")) return;

        // Get the tag of the other collider
        string otherPlayerTag = otherPlayer.tag;

        // Get the tag of the current player in possession (from their child collider)
        string possesionPlayerTag = null;
        foreach (Transform child in possesionPlayer.transform)
        {
            if (child.CompareTag("Ally") || child.CompareTag("Opp"))
            {
                possesionPlayerTag = child.tag;
                break;
            }
        }
        Debug.Log(possesionPlayerTag + ", " + otherPlayerTag);
        // If tags are different and both are either "Ally" or "Opp"
        if (possesionPlayerTag != null && (otherPlayerTag == "Ally" || otherPlayerTag == "Opp") && possesionPlayerTag != otherPlayerTag)
        {            
            GameManager.Instance.HandleDuel(possesionPlayer, otherPlayer.transform.root.gameObject, 0);
        }
        */
    }

    private IEnumerator HideCrosshairAfterDelay()
    {
        // Wait for the specified duration
        yield return new WaitForSeconds(crosshairDisplayDuration);
        crosshairImage.enabled = false;
    }
}
