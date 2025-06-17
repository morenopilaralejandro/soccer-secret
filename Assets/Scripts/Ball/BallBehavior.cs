using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

public class BallBehavior : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , Photon.Pun.IPunObservable, Photon.Pun.IPunInstantiateMagicCallback
#endif
{
    public static BallBehavior Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rigidbody rb;

    [Header("Gameplay Settings")]
    [SerializeField] private float kickForce = 2.0f;
    [SerializeField] private float spinAmount = 5f;
    [SerializeField] private float maxForceDistance = 3f;
    [SerializeField] private float maxVelocity = 10.0f;
    [SerializeField] private float dribbleSpeed = 10f;

    [Header("Other")]
    private PendingKickHandler pendingKickHandler = new PendingKickHandler();

    private bool isPossessed = false;
    private bool wasMovementFrozen = false;

    private Vector2 touchStartPos;
    private Vector2 touchEndPos;

    public static event Action<Player> OnSetStatusPlayer;

#if PHOTON_UNITY_NETWORKING
    private PhotonView photonView => Photon.Pun.PhotonView.Get(this);
#endif

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

    private void OnEnable()
    {
        if (PossessionManager.Instance != null)
        {
            PossessionManager.Instance.OnPossessionGained += OnPossessionGained;
            PossessionManager.Instance.OnPossessionLost += OnPossessionLost;
        }

        if (InputManager.Instance != null)
            InputManager.Instance.TapDetector.OnTap += HandleTap;
    }
    private void OnDisable()
    {
        if (PossessionManager.Instance != null)
        {
            PossessionManager.Instance.OnPossessionGained -= OnPossessionGained;
            PossessionManager.Instance.OnPossessionLost -= OnPossessionLost;
        }

        if (InputManager.Instance != null)
            InputManager.Instance.TapDetector.OnTap -= HandleTap;
    }

    private void Update()
    {
        if (BallTravelController.Instance.IsTraveling) return;

        if (GameManager.Instance.CurrentPhase == GamePhase.KickOff && !GameManager.Instance.IsKickOffReady)
        {
            CrosshairManager.Instance.HideCrosshairImmediately();
            pendingKickHandler.Clear();
        }

#if PHOTON_UNITY_NETWORKING
        // In multiplayer, only current owner of the ball processes logic!
        if (GameManager.Instance.IsMultiplayer && !photonView.IsMine)
            return;
#endif

        HandlePossessionAndTouches();
    }

    #endregion

    #region Tap Handling

    private void HandlePossessionAndTouches()
    {
        if (isPossessed) HandlePossession();

        bool nowFrozen = GameManager.Instance.IsMovementFrozen;
        if (wasMovementFrozen && !nowFrozen && pendingKickHandler.HasPendingKick && PossessionManager.Instance.PossessionPlayer && PossessionManager.Instance.PossessionPlayer.ControlType == ControlType.LocalHuman && !PossessionManager.Instance.PossessionPlayer.IsStunned)
        {
            Vector2 kickTarget;
            pendingKickHandler.TryConsumePendingKick(out kickTarget);
            bool triggeredDuel = GoalDuelInitiator.Instance.TryStartGoalDuelIfValidTarget(kickTarget, false);
            if (!triggeredDuel)
            {
                KickBallToNetworkAware(kickTarget);
                CrosshairManager.Instance.HideCrosshairImmediately();
            }
        }
        wasMovementFrozen = nowFrozen;
    }

    private void HandlePossession()
    {
        if (PossessionManager.Instance.PossessionPlayer == null) return;

        Vector3 targetPosition = PossessionManager.Instance.PossessionPlayer.transform.position + PossessionManager.Instance.PossessionPlayer.transform.forward * 0.5f;
        targetPosition.x += 0.1f;
        targetPosition.y = transform.position.y;
        targetPosition.z -= 0.1f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, dribbleSpeed * Time.deltaTime);
    }

    private void HandleTap(Vector2 screenPosition)
    {

        Vector2 lastTapScreenPosition = screenPosition;

        // 1. If an ally shoot duel is currently resolving, abort
        if (!DuelManager.Instance.IsDuelResolved() && DuelManager.Instance.GetDuelMode() == DuelMode.Shoot && DuelManager.Instance.GetLastOffense() == null)
            return;
        
        // 2. If movement is frozen and user touches the crosshair, cancel pending kick
        if (GameManager.Instance.IsMovementFrozen && CrosshairManager.Instance.IsTouchingCrosshair(screenPosition))
        {
            AudioManager.Instance.PlaySfx("SfxMenuCancel");
            pendingKickHandler.Clear();
            CrosshairManager.Instance.HideCrosshairImmediately();
            return;
        }

        // 3. If not possessed, last toucher was ally, not frozen, and tap: queue an ally kick
        if (!isPossessed
            && PossessionManager.Instance.LastPossessionPlayer != null
            && PossessionManager.Instance.LastPossessionPlayer.ControlType == ControlType.LocalHuman
            && !GameManager.Instance.IsMovementFrozen)
        {
            pendingKickHandler.QueuePendingKick(screenPosition);
            CrosshairManager.Instance.ShowCrosshair(screenPosition);
            return;
        }  
        if (GameManager.Instance.CurrentPhase == GamePhase.KickOff && !GameManager.Instance.IsKickOffReady)
        {
            GameManager.Instance.SetIsKickOffReady(true);
            if (PossessionManager.Instance.PossessionPlayer && PossessionManager.Instance.PossessionPlayer.ControlType != ControlType.LocalHuman)
                return;
        }

        // 4. If ally is in possession and tap: handle kick or queue pending kick
        if (PossessionManager.Instance.PossessionPlayer && PossessionManager.Instance.PossessionPlayer.ControlType == ControlType.LocalHuman)
        {
            if (GoalDuelInitiator.Instance.TryStartGoalDuelIfValidTarget(screenPosition, false))
                return;

            CrosshairManager.Instance.ShowCrosshair(screenPosition);

            if (GameManager.Instance.CurrentPhase == GamePhase.KickOff && GameManager.Instance.IsKickOffReady) {
                GameManager.Instance.SetGamePhase(GamePhase.Battle);                
                GameManager.Instance.UnfreezeGame();   
            }

            if (!GameManager.Instance.IsMovementFrozen)
            {
                KickBallToNetworkAware(screenPosition);  // <<--- Network aware!
                CrosshairManager.Instance.HideCrosshairAfterDelay();
            }
            else
            {
                //during offense field duel
                AudioManager.Instance.PlaySfx("SfxCrosshair");
                pendingKickHandler.QueuePendingKick(screenPosition);
                CrosshairManager.Instance.ShowCrosshair(screenPosition);
            }
            return;
        }

        // 5. If non-ally possesses ball, game frozen, and tap: enable crosshair and queue kick
        if (PossessionManager.Instance.PossessionPlayer && PossessionManager.Instance.PossessionPlayer.ControlType != ControlType.LocalHuman && GameManager.Instance.IsMovementFrozen) 
        {
            //during defense field duel
            AudioManager.Instance.PlaySfx("SfxCrosshair");
            CrosshairManager.Instance.ShowCrosshair(screenPosition);
            pendingKickHandler.QueuePendingKick(screenPosition);
            return;
        }

        if (!DuelManager.Instance.IsDuelResolved() && DuelManager.Instance.GetDuelMode() == DuelMode.Shoot) 
        {
            //during opponent shoot duel
            AudioManager.Instance.PlaySfx("SfxCrosshair");
            CrosshairManager.Instance.ShowCrosshair(screenPosition);
            pendingKickHandler.QueuePendingKick(screenPosition);
            return;
        }
    }

    #endregion

    #region Ball Actions

    /// <summary>
    /// Calls the kick, over the network if needed, locally otherwise.
    /// </summary>
    private void KickBallToNetworkAware(Vector2 targetScreenPosition)
    {
#if PHOTON_UNITY_NETWORKING
        if (GameManager.Instance.IsMultiplayer && photonView.IsMine)
        {
            // Small trick: because Vector2 isn't directly supported by PUN, pass as floats.
            photonView.RPC(nameof(RPC_KickBallTo), Photon.Pun.RpcTarget.All, targetScreenPosition.x, targetScreenPosition.y);
        }
        else
        {
            KickBallTo(targetScreenPosition);
        }
#else
        KickBallTo(targetScreenPosition);
#endif
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_KickBallTo(float x, float y)
    {
        KickBallTo(new Vector2(x, y));
    }
#endif

    private void KickBallTo(Vector2 targetScreenPosition)
    {
        Vector3 touchWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(targetScreenPosition.x, targetScreenPosition.y, mainCamera.nearClipPlane));
        KickBall(touchWorldPos);
    }

    public void KickBall(Vector3 touchWorldPos)
    {
        AudioManager.Instance.PlaySfx("SfxKick");

        isPossessed = false;
        rb.isKinematic = false;

        PossessionManager.Instance.PossessionPlayer?.Kick();

        Vector3 direction = touchWorldPos - transform.position;
        Vector3 rayDirection = direction.normalized;
        float kickDistance = direction.magnitude;

        bool presenceBlocking = false;
        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, kickDistance, LayerMask.GetMask("Presence")))
        {
            if (hit.collider.CompareTag("Presence"))
            {
                GameObject rootObj = hit.collider.transform.parent.gameObject;

                if (rootObj.GetComponent<Player>().TeamIndex == PossessionManager.Instance.PossessionPlayer.TeamIndex) 
                {
                    presenceBlocking = false;    
                } else {
                    presenceBlocking = true;    
                }
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

        PossessionManager.Instance.ReleasePossession();
    }

    private void HandleAllyPendingKickOrControl(Player player)
    {
        if (pendingKickHandler.HasPendingKick && player.ControlType == ControlType.LocalHuman && !player.IsStunned)
        {
            Vector2 targetPosition;
            pendingKickHandler.TryConsumePendingKick(out targetPosition);
            if (!GoalDuelInitiator.Instance.TryStartGoalDuelIfValidTarget(targetPosition, true))
            {
                Debug.Log("Detected pending ally kick. Kicking to target: " + targetPosition);
                KickBallToNetworkAware(targetPosition);
                CrosshairManager.Instance.HideCrosshairImmediately();
            }
        }
        else
        {
            pendingKickHandler.Clear();
            CrosshairManager.Instance.HideCrosshairImmediately();
            PossessionManager.Instance.PossessionPlayer.Control();
        }
    }

    #endregion

    #region Crosshair, Duel, and Utility
    private void OnPossessionGained(Player player)
    {
        Debug.Log("BallBehavior OnPossessionGained: " + player.PlayerId);
        if (!UIManager.Instance.IsStatusLocked)
        {
            UIManager.Instance.HideStatus();
            OnSetStatusPlayer?.Invoke(player);
        }

        isPossessed = true;
        rb.isKinematic = true;

        Vector3 ballPos = player.transform.position + player.transform.forward * 0.5f;
        ballPos.y = transform.position.y;
        transform.position = ballPos;

        if (player.ControlType == ControlType.LocalHuman)
        {
            HandleAllyPendingKickOrControl(player);
        }
        else
        {
            CrosshairManager.Instance.HideCrosshairImmediately();
            pendingKickHandler.Clear();
        }
#if PHOTON_UNITY_NETWORKING
        // Ownership: only relevant if multiplayer, and local player just gained it
        if (GameManager.Instance.IsMultiplayer && player.ControlType == ControlType.LocalHuman /* or similar logic for local player */)
        {
            photonView.RequestOwnership();
        }
#endif
    }

    private void OnPossessionLost(Player player)
    {
        isPossessed = false;
        rb.isKinematic = false;
    }

    #endregion

#if PHOTON_UNITY_NETWORKING
    // For ball transform sync:
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            rb.velocity = (Vector3)stream.ReceiveNext();
        }
    }

    // Photon instantiation hook (optional; for ball spawn ownership etc)
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Custom ball init logic for network (optional)
    }
#endif
}
