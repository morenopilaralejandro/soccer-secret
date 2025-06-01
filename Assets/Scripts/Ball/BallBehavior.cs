using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BallBehavior : MonoBehaviour
{
    public static BallBehavior Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Image crosshairImage;

    [Header("Gameplay Settings")]
    [SerializeField] private float dragThreshold = 2f;
    [SerializeField] private float kickForce = 2.0f;
    [SerializeField] private float spinAmount = 5f;
    [SerializeField] private float maxForceDistance = 3f;
    [SerializeField] private float maxVelocity = 10.0f;
    [SerializeField] private float dribbleSpeed = 10f;
    [SerializeField] private float possessionCooldown = 0.2f;
    [SerializeField] private float keeperGoalDistance = 0.5f;
    [SerializeField] private float shootGoalDistance = 2.2f;
    [SerializeField] private float crosshairDisplayDuration = 0.2f;
    [SerializeField] private float travelSpeed = 3f;
    private Vector3 travelVelocity;

    [Header("State")]
    public Player PossessionPlayer = null;

    private GameObject lastPossessionPlayer = null;
    private float lastPossessionPlayerKickTime = -Mathf.Infinity;

    private bool isPossessed = false;
    private bool isDragging = false;
    private bool wasMovementFrozen = false;
    private Coroutine hideCrosshairCoroutine;

    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private Vector2? pendingKickTarget = null;
    private Vector2? allyPendingKickTarget = null;

    private bool isTravelingToPoint = false;
    private bool isTravelPaused = false;
    private Vector3 currentTravelTarget;
    private Action onTravelCanceled;

    public static event Action<Player> OnSetStatusPlayer;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        HandleTravel();

        if (isTravelingToPoint) 
            return;

        if (GameManager.Instance.IsKickOffPhase && !GameManager.Instance.IsKickOffReady)
        {
            HideCrosshairImmediately();
            pendingKickTarget = null;
            allyPendingKickTarget = null;
        }     

        HandlePossessionAndTouches();
    }

    #endregion

    #region Travel Logic

    private void HandleTravel()
    {
        if (!isTravelingToPoint || isTravelPaused) return;

        // Store previous position to compute velocity
        Vector3 previousPosition = transform.position;

        float step = travelSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, currentTravelTarget, step);

        // Calculate the velocity from previous to new position
        travelVelocity = (transform.position - previousPosition) / Time.deltaTime;

        if (Vector3.Distance(transform.position, currentTravelTarget) < 0.01f)
        {
            isTravelingToPoint = false;
            rb.isKinematic = false;

            // Optionally clamp to maxVelocity
            if (travelVelocity.magnitude > maxVelocity)
                travelVelocity = travelVelocity.normalized * maxVelocity;
            rb.velocity = travelVelocity;

            DuelManager.Instance.CancelDuel();
        }
    }

    public void StartTravelToPoint(Vector3 targetPoint)
    {
        Debug.Log("Ball travel started");
        isTravelingToPoint = true;
        currentTravelTarget = targetPoint;
        isTravelPaused = false;
        rb.isKinematic = true;
    }

    public void PauseTravel()
    {
        Debug.Log("Ball travel paused");
        if (isTravelingToPoint) isTravelPaused = true;
    }

    public void ResumeTravel()
    {
        Debug.Log("Ball travel resumed");
        if (isTravelingToPoint) isTravelPaused = false;
    }

    public void CancelTravel()
    {
        isTravelingToPoint = false;
        isTravelPaused = false;
        rb.isKinematic = false;
        onTravelCanceled?.Invoke();
        ShootTriangle.Instance.SetTriangleVisible(false);
        DuelManager.Instance.CancelDuel();
        Debug.Log("Travel canceled");
    }

    #endregion

    #region Input and Touch Handling

    private void HandlePossessionAndTouches()
    {
        if (isPossessed) HandlePossession();

        bool nowFrozen = GameManager.Instance.IsMovementFrozen;
        if (wasMovementFrozen && !nowFrozen && pendingKickTarget.HasValue && PossessionPlayer && PossessionPlayer.IsAlly && !PossessionPlayer.IsStunned)
        {
            Vector2 kickTarget = pendingKickTarget.Value;
            bool triggeredDuel = TryStartGoalDuelIfValid(kickTarget, false);
            if (!triggeredDuel)
            {
                KickBallTo(kickTarget);
            }
            pendingKickTarget = null;
            HideCrosshairImmediately();
        }
        wasMovementFrozen = nowFrozen;

        if (Input.touchCount > 0)
            ProcessTouch(Input.GetTouch(0));
    }

    private void ProcessTouch(Touch touch)
    {
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
                HandleTouchEnded(touch);
                break;

            case TouchPhase.Canceled:
                pendingKickTarget = null;
                hideCrosshairCoroutine = StartCoroutine(HideCrosshairAfterDelay());
                break;
        }
    }

    private void HandleTouchEnded(Touch touch)
    {
        touchEndPos = touch.position;
        bool isTap = !isDragging && Vector2.Distance(touchStartPos, touchEndPos) < dragThreshold;

        // 1. If a shoot duel is currently resolving, abort
        if (!DuelManager.Instance.IsDuelResolved() && DuelManager.Instance.GetDuelMode() == DuelMode.Shoot)
            return;

        // 2. If movement is frozen and user touches the crosshair, cancel pending kick
        if (GameManager.Instance.IsMovementFrozen && IsTouchingCrosshair(touchEndPos))
        {
            AudioManager.Instance.PlaySfx("SfxMenuCancel");
            pendingKickTarget = null;
            HideCrosshairImmediately();
            return;
        }

        // 3. If not possessed, last toucher was ally, not frozen, and tap: queue an ally kick
        if (!isPossessed
            && lastPossessionPlayer != null
            && lastPossessionPlayer.GetComponent<Player>().IsAlly
            && !GameManager.Instance.IsMovementFrozen
            && isTap)
        {
            QueueAllyPendingKick(touchEndPos);
            return;
        }  

        if (GameManager.Instance.IsKickOffPhase && !GameManager.Instance.IsKickOffReady && isTap)
        {
            
            GameManager.Instance.SetIsKickOffReady(true);
            if (PossessionPlayer && !PossessionPlayer.IsAlly)
                return;
        }

        // 4. If ally is in possession and tap: handle kick or queue pending kick
        if (isPossessed && PossessionPlayer && PossessionPlayer.IsAlly && isTap)
        {
            if (TryStartGoalDuelIfValid(touchEndPos, false))
                return;

            crosshairImage.transform.position = touchEndPos;
            crosshairImage.enabled = true;

            if (GameManager.Instance.IsKickOffPhase && GameManager.Instance.IsKickOffReady)
                GameManager.Instance.UnfreezeGame();

            if (!GameManager.Instance.IsMovementFrozen)
            {
                KickBallTo(touchEndPos);
                if (hideCrosshairCoroutine != null)
                    StopCoroutine(hideCrosshairCoroutine);
                hideCrosshairCoroutine = StartCoroutine(HideCrosshairAfterDelay());
            }
            else
            {
                //during offense duel
                AudioManager.Instance.PlaySfx("SfxCrosshair");
                pendingKickTarget = touchEndPos;
            }
            return;
        }

        // 5. If non-ally possesses ball, game frozen, and tap: enable crosshair and queue kick (your newly added case)
        if (PossessionPlayer && !PossessionPlayer.IsAlly && isTap && GameManager.Instance.IsMovementFrozen) 
        {
            //during defense duel
            AudioManager.Instance.PlaySfx("SfxCrosshair");
            crosshairImage.transform.position = touchEndPos;
            crosshairImage.enabled = true;
            pendingKickTarget = touchEndPos;
            return;
        }
    }

    private void QueueAllyPendingKick(Vector2 pos)
    {
        allyPendingKickTarget = pos;
        crosshairImage.transform.position = pos;
        crosshairImage.enabled = true;
        Debug.Log("Queued pending kick for ally on next possession: " + pos);
    }

    #endregion

    #region Ball Actions

    private void HandlePossession()
    {
        if (PossessionPlayer == null) return;

        Vector3 targetPosition = PossessionPlayer.transform.position + PossessionPlayer.transform.forward * 0.5f;
        targetPosition.x += 0.1f;
        targetPosition.y = transform.position.y;
        targetPosition.z -= 0.1f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, dribbleSpeed * Time.deltaTime);
    }

    public void KickBallTo(Vector2 targetScreenPosition)
    {
        Vector3 touchWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(targetScreenPosition.x, targetScreenPosition.y, mainCamera.nearClipPlane));
        KickBall(touchWorldPos);    
    }

    public void KickBall(Vector3 touchWorldPos)
    {
        AudioManager.Instance.PlaySfx("SfxKick");

        isPossessed = false;
        rb.isKinematic = false;

        PossessionPlayer?.Kick();
 
        Vector3 direction = touchWorldPos - transform.position;
        Vector3 rayDirection = direction.normalized;
        float kickDistance = direction.magnitude;

        bool presenceBlocking = false;
        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, kickDistance, LayerMask.GetMask("Presence")))
        {
            if (hit.collider.CompareTag("Presence"))
            {
                GameObject rootObj = hit.collider.transform.parent.gameObject;
                presenceBlocking = !rootObj.GetComponent<Player>().IsAlly;
            }
        }

        float actualKickForce = presenceBlocking ? kickForce * 0.9f : kickForce;
        float actualMaxVelocity = presenceBlocking ? maxVelocity * 0.9f : maxVelocity;
        direction.y = Mathf.Clamp(direction.y, presenceBlocking ? 0.9f : 0, presenceBlocking ? 0.9f : 0.7f);

        if (direction.magnitude > maxForceDistance)
            direction = direction.normalized * maxForceDistance;

        Debug.Log($"Presence block: {presenceBlocking}, Final Direction: {direction}, Kick Force: {actualKickForce}");
        Debug.DrawLine(transform.position, transform.position + direction * 2, presenceBlocking ? Color.yellow : Color.red, 5.0f);

        rb.AddForce(direction * actualKickForce, ForceMode.Impulse);
        rb.AddTorque(Vector3.right * spinAmount, ForceMode.Impulse);
        if (rb.velocity.magnitude > actualMaxVelocity)
            rb.velocity = rb.velocity.normalized * actualMaxVelocity;

        ReleasePossession();
    }

    public void GainPossession(Player player)
    {
        ReleasePossession();
        PossessionPlayer = player;
        PossessionPlayer.IsPossession = true;

        if (!UIManager.Instance.IsStatusLocked)
        {
            UIManager.Instance.HideStatus();
            OnSetStatusPlayer?.Invoke(PossessionPlayer);
        }
        Debug.Log("Possession granted to: " + PossessionPlayer.PlayerId);

        isPossessed = true;
        rb.isKinematic = true;

        Vector3 ballPos = PossessionPlayer.transform.position + PossessionPlayer.transform.forward * 0.5f;
        ballPos.y = transform.position.y;
        transform.position = ballPos;

        // After "isPossessed" && "rb.isKinematic" in GainPossession
        if (player.IsAlly) {
            HandleAllyPendingKickOrControl(player);
        } else {
            HideCrosshairImmediately();
            pendingKickTarget = null;
            allyPendingKickTarget = null;
        }
    }

    private void HandleAllyPendingKickOrControl(Player player)
    {
        if (allyPendingKickTarget.HasValue && player.IsAlly && !player.IsStunned && !GameManager.Instance.IsTimeFrozen && !GameManager.Instance.IsKickOffPhase)
        {
            if (!TryStartGoalDuelIfValid(allyPendingKickTarget.Value, true))
            {
                Debug.Log("Detected pending ally kick. Kicking to target: " + allyPendingKickTarget.Value);
                KickBallTo(allyPendingKickTarget.Value);
                allyPendingKickTarget = null;
                HideCrosshairImmediately();
            }
        }
        else
        {
            allyPendingKickTarget = null;
            HideCrosshairImmediately();
            PossessionPlayer.Control();
        }
    }

    public void ReleasePossession()
    {
        if (PossessionPlayer != null)
        {
            Debug.Log("Possession taken from: " + PossessionPlayer.PlayerId);
            lastPossessionPlayer = PossessionPlayer.gameObject;
            lastPossessionPlayerKickTime = Time.time;
            PossessionPlayer.IsPossession = false;
            PossessionPlayer = null;
            isPossessed = false;
            rb.isKinematic = false;
        }
    }

    #endregion

    #region Crosshair, Duel, and Utility

    private bool TryStartGoalDuelIfValid(Vector2 screenPos, bool isDirect)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Debug.DrawRay(ray.origin, ray.direction * Mathf.Infinity, Color.red, 2f);

        int goalLayerMask = LayerMask.GetMask("GoalTouchArea");
        Debug.Log($"Attempting to raycast to Goal layer. Mask={goalLayerMask}, TapPos={screenPos}");
        if (Physics.Raycast(ray, out RaycastHit hitGoal, Mathf.Infinity, goalLayerMask))
        {
            Debug.Log($"Raycast hit: {hitGoal.collider.name} on layer {LayerMask.LayerToName(hitGoal.collider.gameObject.layer)} Tag={hitGoal.collider.tag}");
            if (
                GameManager.Instance.GetDistanceToOppGoal(PossessionPlayer) < shootGoalDistance
                && hitGoal.collider.CompareTag("Opp")
                && DuelManager.Instance.IsDuelResolved()
                && !GameManager.Instance.IsMovementFrozen)
            {
                Debug.Log("Tap on OPP GOAL detected. Initiating Duel.");
                GameManager.Instance.FreezeGame();
                DuelManager.Instance.StartDuel(DuelMode.Shoot);
                DuelManager.Instance.RegisterTrigger(PossessionPlayer.gameObject, isDirect);
                UIManager.Instance.SetUserRole(Category.Shoot, 0, PossessionPlayer);
                UIManager.Instance.SetButtonDuelToggleVisible(true);
                ShootTriangle.Instance.SetTriangleFromUser(PossessionPlayer, screenPos);
                ShootTriangle.Instance.SetTriangleVisible(true);
                return true;
            }
        }
        else
        {
            Debug.Log("Raycast did NOT hit anything on 'Goal' layer.");
        }
        return false;
    }

    private IEnumerator HideCrosshairAfterDelay()
    {
        yield return new WaitForSeconds(crosshairDisplayDuration);
        crosshairImage.enabled = false;
    }

    private void HideCrosshairImmediately()
    {
        if (hideCrosshairCoroutine != null)
            StopCoroutine(hideCrosshairCoroutine);
        if (crosshairImage)
            crosshairImage.enabled = false;
    }

    private bool IsTouchingCrosshair(Vector2 screenPosition)
    {
        if (!crosshairImage || !crosshairImage.enabled) return false;
        Canvas canvas = crosshairImage.canvas;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(
            crosshairImage.rectTransform, screenPosition, eventCamera);
    }

    #endregion

    #region Collision

    private void OnTriggerEnter(Collider collider)
    {
        GameObject rootObj = collider.transform.root.gameObject;
        Debug.Log("BallBehavior OnTriggerEnter: " + rootObj.name + " (Tag: " + rootObj.tag + ")");

        if (isTravelingToPoint)
            return;

        Player playerComp = rootObj.GetComponent<Player>();
        bool validPossession = false;
        bool isKeeper = false;

        // Standard player touch
        if (collider.CompareTag("Player"))
        {
            validPossession = true;
        }
        // Keeper special case
        else if (
            collider.CompareTag("PlayerKeeperCollider") &&
            playerComp != null &&
            lastPossessionPlayer.GetComponent<Player>().IsAlly != playerComp.IsAlly && //keeper won't stop a pass from a player in it's same team
            GameManager.Instance.GetDistanceToAllyGoal(playerComp) < keeperGoalDistance)
        {
            isKeeper = true;
            validPossession = true;
        }

        // Shared cooldown and possession logic
        if (
            !isPossessed &&
            validPossession &&
            playerComp != null)
        {
            bool isLastPossessionPlayer = (lastPossessionPlayer != null && rootObj == lastPossessionPlayer);
            bool cooldownActiveForThisPlayer = isLastPossessionPlayer && (Time.time <= lastPossessionPlayerKickTime + possessionCooldown);

            if (!cooldownActiveForThisPlayer)
            {
                GainPossession(playerComp);
                if (isKeeper)
                    AudioManager.Instance.PlaySfx("SfxCatch");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObj = collision.collider.gameObject;
        Debug.Log("BallBehavior OnCollisionEnter: " + hitObj.name + " (Tag: " + hitObj.tag + ")");

        if (isTravelingToPoint && hitObj.CompareTag("Bound"))
        {
            CancelTravel();
        }

    }

    #endregion
}
