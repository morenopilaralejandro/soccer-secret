using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BallBehavior : MonoBehaviour
{
    public static BallBehavior Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private bool isPossessed = false;
    [SerializeField] private bool isDragging = false;
    [SerializeField] private Vector2 touchStartPos;
    [SerializeField] private Vector2 touchEndPos;
    [SerializeField] private float dragThreshold = 2f; // minimum pixels for drag

    [SerializeField] private float kickForce = 2.0f;
    [SerializeField] private float maxForceDistance = 3f;
    [SerializeField] private float maxVelocity = 10.0f;
    [SerializeField] private float dribbleSpeed = 10f;
    [SerializeField] private float possessionCooldown = 0.2f;

    [SerializeField] private Player possessionPlayer = null;
    [SerializeField] private GameObject lastPossessionPlayer = null;
    private float lastPossessionPlayerKickTime = -Mathf.Infinity;

    [SerializeField] private Image crosshairImage;
    [SerializeField] private float crosshairDisplayDuration = 0.2f;
    private Coroutine hideCrosshairCoroutine;
    [SerializeField] private Vector2? pendingKickTarget = null;
    [SerializeField] private bool wasMovementFrozen = false;

    public static event Action<Player> OnSetStatusPlayer;
    public static event Action<Player> OnHideStatusPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {

    }

    void Update()
    {
        if (isPossessed) HandlePossession();

        bool nowFrozen = GameManager.Instance.IsMovementFrozen;
        // If movement just resumed, check for a pending kick
        if (wasMovementFrozen && !nowFrozen && pendingKickTarget.HasValue)
        {
            KickBallTo(pendingKickTarget.Value);
            pendingKickTarget = null;
            HideCrosshairImmediately();
        }
        wasMovementFrozen = nowFrozen; // Update at end

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isDragging = false;
                    break;
                case TouchPhase.Moved:
                    if (Vector2.Distance(touchStartPos, touch.position) > dragThreshold)
                        isDragging = true;
                    break;
                case TouchPhase.Ended:
                    touchEndPos = touch.position;
                    bool isTap = !isDragging && Vector2.Distance(touchStartPos, touchEndPos) < dragThreshold;

                    // Handle crosshair tap to cancel pending kick
                    if (GameManager.Instance.IsMovementFrozen && IsTouchingCrosshair(touchEndPos))
                    {
                        pendingKickTarget = null;
                        HideCrosshairImmediately();
                        break; // Do not process as a kick
                    }

                    if (isPossessed && possessionPlayer && possessionPlayer.IsAlly && isTap)
                    {
                        crosshairImage.transform.position = touchEndPos;
                        crosshairImage.enabled = true;
                        if (!GameManager.Instance.IsMovementFrozen)
                        {
                            KickBallTo(touchEndPos);
                            if (hideCrosshairCoroutine != null)
                                StopCoroutine(hideCrosshairCoroutine);
                            hideCrosshairCoroutine = StartCoroutine(HideCrosshairAfterDelay());
                        }
                        else
                        {
                            // Movement frozen: store pending kick target and show crosshair, but DO NOT hide it!
                            pendingKickTarget = touchEndPos;
                        }
                    }
                    /*
                    else if (hideCrosshairCoroutine != null)
                    {
                        StopCoroutine(hideCrosshairCoroutine);
                        hideCrosshairCoroutine = StartCoroutine(HideCrosshairAfterDelay());
                    }
                    */
                    break;
                case TouchPhase.Canceled:
                    pendingKickTarget = null;
                    hideCrosshairCoroutine = StartCoroutine(HideCrosshairAfterDelay());
                    break;
            }
        }
    }

    private void HandlePossession()
    {
        if (possessionPlayer == null) return;

        Vector3 targetPosition = possessionPlayer.gameObject.transform.position + possessionPlayer.gameObject.transform.forward * 0.5f;
        targetPosition.x += 0.1f;
        targetPosition.y = transform.position.y;
        targetPosition.z -= 0.2f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, dribbleSpeed * Time.deltaTime);
    }

    public void KickBallTo(Vector2 targetScreenPosition) //touchEnd
    {
        isPossessed = false;
        rb.isKinematic = false;

        if (possessionPlayer != null)
            StartCoroutine(possessionPlayer.KickCoroutine());

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
            actualMaxVelocity *= 0.9f;
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

        ReleasePossession();        
    }

    public void GainPossession(Player player)
    {
        possessionPlayer = player;
        BallBehavior.OnSetStatusPlayer?.Invoke(possessionPlayer);
        Debug.Log("Possession granted to: " + possessionPlayer.PlayerNameEn);
        possessionPlayer.IsPossession = true;
        isPossessed = true;
        rb.isKinematic = true;

        Vector3 immediatePosition = possessionPlayer.gameObject.transform.position + possessionPlayer.gameObject.transform.forward * 0.5f;
        immediatePosition.y = transform.position.y;
        transform.position = immediatePosition;
    }

    public void ReleasePossession()
    {
        if (possessionPlayer != null) {
            BallBehavior.OnHideStatusPlayer?.Invoke(possessionPlayer);
            Debug.Log("Possession taken from: " + possessionPlayer.PlayerNameEn);
            lastPossessionPlayer = possessionPlayer.gameObject;
            lastPossessionPlayerKickTime = Time.time;
            possessionPlayer.IsPossession = false;
            possessionPlayer = null;
            isPossessed = false;
            rb.isKinematic = false;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        GameObject rootObj = collider.transform.root.gameObject;
        Debug.Log("BallBehavior OnTriggerEnter: " + rootObj.name + " (Tag: " + rootObj.tag + ")");
        if (!isPossessed && rootObj.CompareTag("Player"))
        {
            bool isLastPossessionPlayer = (lastPossessionPlayer != null && rootObj == lastPossessionPlayer);
            bool cooldownActiveForThisPlayer = isLastPossessionPlayer && (Time.time <= lastPossessionPlayerKickTime + possessionCooldown);

            // Only block if it's the same player within their cooldown
            if (!cooldownActiveForThisPlayer)
            {
                GainPossession(rootObj.GetComponent<Player>());
            }
        }
    }

    private IEnumerator HideCrosshairAfterDelay()
    {
        // Wait for the specified duration
        yield return new WaitForSeconds(crosshairDisplayDuration);
        crosshairImage.enabled = false;
    }

    private void HideCrosshairImmediately()
    {
        if (hideCrosshairCoroutine != null)
            StopCoroutine(hideCrosshairCoroutine);
        if (crosshairImage) crosshairImage.enabled = false;
    }

    private bool IsTouchingCrosshair(Vector2 screenPosition)
    {
        if (!crosshairImage || !crosshairImage.enabled) return false;
        // If your Canvas's render mode is Camera or Overlay, use Camera as needed, else pass null.
        Canvas canvas = crosshairImage.canvas;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(
            crosshairImage.rectTransform, screenPosition, eventCamera);
    }

}
