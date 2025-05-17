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

    public Player PossessionPlayer = null;
    [SerializeField] private GameObject lastPossessionPlayer = null;
    private float lastPossessionPlayerKickTime = -Mathf.Infinity;

    [SerializeField] private Image crosshairImage;
    [SerializeField] private float crosshairDisplayDuration = 0.2f;
    private Coroutine hideCrosshairCoroutine;
    [SerializeField] private Vector2? pendingKickTarget = null;
    [SerializeField] private bool wasMovementFrozen = false;

    public static event Action<Player> OnSetStatusPlayer;
    //public static event Action<Player> OnHideStatusPlayer;

    private bool isTravelingToPoint = false;
    private Vector3 currentTravelTarget;
    private float travelSpeed = 4f; // set as desired
    private bool isTravelPaused = false;

    private Action onTravelCanceled;

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
        if (isTravelingToPoint && !isTravelPaused)
        {
            Vector3 start = transform.position;
            Vector3 end = currentTravelTarget;
            float step = travelSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(start, end, step);

            // Arrived!
            if (Vector3.Distance(transform.position, end) < 0.01f)
            {
                isTravelingToPoint = false;
                rb.isKinematic = false; // enable physics again if needed
            }
        }

        if (isTravelingToPoint) 
            return;

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

                    if (!DuelManager.Instance.IsDuelResolved() && DuelManager.Instance.GetDuelMode() == DuelMode.Shoot)
                        break;

                    // Handle crosshair tap to cancel pending kick
                    if (GameManager.Instance.IsMovementFrozen && IsTouchingCrosshair(touchEndPos))
                    {
                        pendingKickTarget = null;
                        HideCrosshairImmediately();
                        break; // Do not process as a kick
                    }

                    if (isPossessed && PossessionPlayer && PossessionPlayer.IsAlly && isTap)
                    {
                        // --- Goal Raycast from camera to tap position ---                  
                        Ray ray = mainCamera.ScreenPointToRay(touchEndPos);
                        Debug.DrawRay(ray.origin, ray.direction * Mathf.Infinity, Color.red, 2f); // <--- Add this line
                        RaycastHit hitGoal;
                        int goalLayerMask = LayerMask.GetMask("Goal");
                        Debug.Log($"Attempting to raycast to Goal layer. Mask={goalLayerMask}, TapPos={touchEndPos}");
                        if (Physics.Raycast(ray, out hitGoal, Mathf.Infinity, goalLayerMask))
                        {
                            Debug.Log($"Raycast hit: {hitGoal.collider.name} on layer {LayerMask.LayerToName(hitGoal.collider.gameObject.layer)} Tag={hitGoal.collider.tag}");
                            if (GameManager.Instance.GetDistanceToOppGoal(PossessionPlayer) < 2.2f && hitGoal.collider.CompareTag("Opp") && DuelManager.Instance.IsDuelResolved())
                            {
                                Debug.Log("Tap on OPP GOAL detected. Initiating Duel.");
                                GameManager.Instance.FreezeGame();
                                DuelManager.Instance.StartDuel(DuelMode.Shoot);
                                DuelManager.Instance.RegisterTrigger(PossessionPlayer.gameObject);
                                UIManager.Instance.SetButtonDuelToggleVisible(true);
                                UIManager.Instance.SetUserRole(Category.Shoot, 0, PossessionPlayer, DuelAction.Offense);

                                ShootTriangle.Instance.SetTriangleFromPlayer(PossessionPlayer, touchEndPos);
                                ShootTriangle.Instance.SetTriangleVisible(true);
                            }
                            break; // Do nothing else this frame
                        } else {
                            Debug.Log("Raycast did NOT hit anything on 'Goal' layer.");
                        }
                        // --- End Goal raycast

                        crosshairImage.transform.position = touchEndPos;
                        crosshairImage.enabled = true;
                        if (GameManager.Instance.IsKickOff) {
                            GameManager.Instance.UnfreezeGame();
                        }
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
        if (PossessionPlayer == null) return;

        Vector3 targetPosition = PossessionPlayer.gameObject.transform.position + PossessionPlayer.gameObject.transform.forward * 0.5f;
        targetPosition.x += 0.1f;
        targetPosition.y = transform.position.y;
        targetPosition.z -= 0.2f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, dribbleSpeed * Time.deltaTime);
    }

    public void KickBallTo(Vector2 targetScreenPosition) //touchEnd
    {
        isPossessed = false;
        rb.isKinematic = false;

        if (PossessionPlayer != null)
            StartCoroutine(PossessionPlayer.KickCoroutine());

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
        ReleasePossession();
        PossessionPlayer = player;
        PossessionPlayer.IsPossession = true;
        if (!UIManager.Instance.IsStatusLocked) {
            UIManager.Instance.HideStatus();
            BallBehavior.OnSetStatusPlayer?.Invoke(PossessionPlayer);
        }
        Debug.Log("Possession granted to: " + PossessionPlayer.PlayerNameEn);
        isPossessed = true;
        rb.isKinematic = true;

        Vector3 immediatePosition = PossessionPlayer.gameObject.transform.position + PossessionPlayer.gameObject.transform.forward * 0.5f;
        immediatePosition.y = transform.position.y;
        transform.position = immediatePosition;
    }

    public void ReleasePossession()
    {
        if (PossessionPlayer != null) {
            Debug.Log("Possession taken from: " + PossessionPlayer.PlayerNameEn);
            lastPossessionPlayer = PossessionPlayer.gameObject;
            lastPossessionPlayerKickTime = Time.time;
            PossessionPlayer.IsPossession = false;
            PossessionPlayer = null;
            isPossessed = false;
            rb.isKinematic = false;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        GameObject rootObj = collider.transform.root.gameObject;
        Debug.Log("BallBehavior OnTriggerEnter: " + rootObj.name + " (Tag: " + rootObj.tag + ")");

        if (isTravelingToPoint && collider.CompareTag("Bound"))
        {
            CancelTravel();
            return;
        }

        if (isTravelingToPoint)
        {
            return;
        }

        if (!isPossessed && collider.CompareTag("Player"))
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


    public void StartTravelToPoint(Vector3 targetPoint)
    {
        isTravelingToPoint = true;
        currentTravelTarget = targetPoint;
        isTravelPaused = false;
        rb.isKinematic = true; // disables physics!
    }

    public void PauseTravel()
    {
        if (isTravelingToPoint) isTravelPaused = true;
    }

    public void ResumeTravel()
    {
        if (isTravelingToPoint) isTravelPaused = false;
    }

    public void CancelTravel()
    {
        isTravelingToPoint = false;
        isTravelPaused = false;
        rb.isKinematic = false; // re-enables physics
        if (onTravelCanceled != null) onTravelCanceled.Invoke();
        Debug.Log("Travel canceled");
    }

}
