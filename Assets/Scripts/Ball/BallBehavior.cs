using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

public class BallBehavior : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , IPunObservable, IPunInstantiateMagicCallback
#endif
{
    public static BallBehavior Instance { get; private set; }

    [Header("References")]  
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rigidbody rb;

    [Header("Gameplay Settings")]
    [SerializeField] private float spinAmount = 5f;
    [SerializeField] private float dribbleSpeed = 10f;
    [SerializeField] private float dribbleOffsetForward = 0.5f;
    [SerializeField] private float dribbleOffsetZ = 0.2f;
    [SerializeField] private float minKickForce = 3f;
    [SerializeField] private float maxKickForce = 6f;
    [SerializeField] private float minMagnitude = 1f;
    [SerializeField] private float maxMagnitude = 3f;

    private readonly PendingKickHandler _pendingKick = new PendingKickHandler();
    private readonly PendingSwipeHandler _pendingSwipe = new PendingSwipeHandler();
    private bool _isPossessed;
    private bool _wasMovementFrozen;

    private Vector3 pausedVelocity;
    private Vector3 pausedAngularVelocity;

    private bool isShootOnSwipeUp;

    public static event Action<Player> OnSetStatusPlayer;

#if PHOTON_UNITY_NETWORKING
    private PhotonView _photonView => PhotonView.Get(this);
#endif

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GameLogger.Info("[BallBehavior] Instance created.", this);

        isShootOnSwipeUp = SettingsManager.GetIsShootOnSwipeUp();
    }

    private void OnEnable()
    {
        PossessionManager.Instance?.Subscribe(OnPossessionGained, OnPossessionLost);
        InputManager.Instance?.Subscribe(HandleTap, HandleSwipe, HandleActionKey);
    }

    private void OnDisable()
    {
        PossessionManager.Instance?.Unsubscribe(OnPossessionGained, OnPossessionLost);
        InputManager.Instance?.Unsubscribe(HandleTap, HandleSwipe, HandleActionKey);
    }

    private void Update()
    {
        if (BallTravelController.Instance.IsTraveling
#if PHOTON_UNITY_NETWORKING
            || (GameManager.Instance.IsMultiplayer && !_photonView.IsMine)
#endif
        ) return;

        if (IsKickoffWaiting()) ResetPendingInputs();
        HandlePossessionAndInputs();
    }

    #endregion

    #region Input Handling

    private void HandlePossessionAndInputs()
    {
        var gm = GameManager.Instance;
        var pm = PossessionManager.Instance;
        var player = pm.CurrentPlayer;

        if (_isPossessed) DribbleTowardsPlayer(player);

        bool nowFrozen = gm.IsMovementFrozen;
        if (_wasMovementFrozen && !nowFrozen && CanProcessPendingKick(player))
            ExecutePendingKick(player);

        if (_pendingSwipe.HasPendingSwipeUp && CanProcessSwipe(player))
            ProcessPendingSwipe(player);

        _wasMovementFrozen = nowFrozen;
    }

    private void HandleTap(Vector2 screenPos)
    {
        var gm = GameManager.Instance;
        var pm = PossessionManager.Instance;
        var player = pm.CurrentPlayer;

        // Early exits
        if (!DuelManager.Instance.IsDuelResolved() && IsShootDuel()) return;
        if (gm.IsMovementFrozen && CrosshairManager.Instance.IsTouchingCrosshair(screenPos))
        {
            CancelKick(); return;
        }

        // Ally queue on last possession
        if (!_isPossessed && IsLastAllyTouched(gm))
        {
            GameLogger.DebugLog("[BallBehavior] Ally queue on last possession.", this);
            QueueKick(screenPos); return;
        }

        // Kick-off readiness
        if (gm.CurrentPhase == GamePhase.Kickoff && !KickoffManager.Instance.IsKickoffReady)
        {
            KickoffManager.Instance.SetTeamReady(GameManager.Instance.GetLocalTeamIndex());
            if (player == null || player.ControlType != ControlType.LocalHuman) return;
        }

        // Possessed by local human
        if (_isPossessed && player.ControlType == ControlType.LocalHuman)
        {
            GameLogger.DebugLog("[BallBehavior] Ball is possessed by local human, processing tap.", this);
            if (TryGoalDuel(player, screenPos)) return;
            ShowCrosshair(screenPos);
            if (ReadyToKickoff(gm)) StartMatch();
            if (!gm.IsMovementFrozen) KickOrQueueImmediate(screenPos);
            else QueueKickDuringFreeze(screenPos);
            return;
        }

        // Defense queue
        if (_isPossessed && IsDefenseQueue(gm, player))
        {
            GameLogger.DebugLog("[BallBehavior] Defense queue logic (field duel - block categoty).", this);
            QueueKick(screenPos); return;
        }

        // Opponent shoot queue
        if (!DuelManager.Instance.IsDuelResolved() && DuelManager.Instance.GetDuelMode() == DuelMode.Shoot)
        {
            GameLogger.DebugLog("[BallBehavior] Opponent shoot queue logic. (field duel - catch categoty)", this);
            QueueKick(screenPos); return;
        }
    }

    private void HandleSwipe(SwipeDetector.SwipeDirection dir)
    {
        if (!isShootOnSwipeUp) return;
        if (InputManager.Instance.IsDragging) return;
        if (InputManager.Instance.SwipeDetector.WasConsumedThisFrame()) return;
        if (PauseManager.Instance.IsPaused) return; 
        if (GameManager.Instance.CurrentPhase != GamePhase.Battle) return; 

        if (dir == SwipeDetector.SwipeDirection.Up) 
        {
            InputManager.Instance.SwipeDetector.Consume();
            TryShootOrQueue();
        }
    }

    private void HandleActionKey() 
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Battle) return; 
        InputManager.Instance.KeyboardDetector.ConsumeActionKey();
        TryShootOrQueue();
    }

    private void TryShootOrQueue()
    {
        var player = PossessionManager.Instance.CurrentPlayer;
        bool canShoot = player != null && player.ControlType == ControlType.LocalHuman
                        && DuelManager.Instance.IsDuelResolved()
                        && !GameManager.Instance.IsMovementFrozen
                        && !player.IsStunned;
        if (canShoot) GoalDuelInitiator.Instance.TryStartGoalDuelIfValidSwipe(player, false);
        else _pendingSwipe.QueuePendingSwipeUp();
    }

    #endregion

    #region Ball Actions

    private void KickBallToNetworkAware(Vector2 target)
    {
#if PHOTON_UNITY_NETWORKING
        if (GameManager.Instance.IsMultiplayer && _photonView.IsMine)
        {
            _photonView.RPC(nameof(RPC_KickBallTo), RpcTarget.All, target.x, target.y);
            return;
        }
#endif
        KickBallTo(target);
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_KickBallTo(float x, float y) => KickBallTo(new Vector2(x, y));
#endif

    private void KickBallTo(Vector2 screenPos)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));
        GameLogger.DebugLog($"[BallBehavior] Kicking ball to {worldPos}.", this);
        KickBall(worldPos);
    }

    public void KickBall(Vector3 worldPos)
    {
        AudioManager.Instance.PlaySfx("SfxKick");
        _isPossessed = false;
        rb.isKinematic = false;
        PossessionManager.Instance.CurrentPlayer?.Kick();

        Vector3 dir = (worldPos - transform.position);
        float dist = Mathf.Clamp(dir.magnitude, minMagnitude, maxMagnitude);
        float force = Mathf.Lerp(minKickForce, maxKickForce, (dist - minMagnitude) / (maxMagnitude - minMagnitude));

        rb.AddForce(dir.normalized * force, ForceMode.Impulse);
        rb.AddTorque(Vector3.right * spinAmount, ForceMode.Impulse);
        GameLogger.Info($"[BallBehavior] Ball kicked with force {force} towards {dir.normalized}", this);

        DuelLogManager.Instance.AddActionPass(PossessionManager.Instance.CurrentPlayer);
        PossessionManager.Instance.Release();
    }

    public void PauseBall()
    {
        if (BallTravelController.Instance.IsTraveling) 
        {
            BallTravelController.Instance.PauseTravel();
        } else 
        {
            if (!rb.isKinematic) // Only if not possessed by a player!
            {
                pausedVelocity = rb.velocity;
                pausedAngularVelocity = rb.angularVelocity;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    public void ResumeBall()
    {
        if (BallTravelController.Instance.IsPaused) 
        {
            BallTravelController.Instance.ResumeTravel();
        } else 
        {
            if (!_isPossessed) // Only resume physics if not dribbling/player controlled
            {
                rb.isKinematic = false;
                rb.velocity = pausedVelocity;
                rb.angularVelocity = pausedAngularVelocity;
            }
        }
    }

    #endregion

    #region Helpers

    private bool IsKickoffWaiting() => GameManager.Instance.CurrentPhase == GamePhase.Kickoff && !KickoffManager.Instance.IsKickoffReady;
    public void ResetPendingInputs()
    {
        //GameLogger.DebugLog("[BallBehavior] Resetting pending inputs.", this);
        CrosshairManager.Instance.HideCrosshairImmediately();
        _pendingKick.Clear(); 
        _pendingSwipe.Clear();
    }
    private bool IsShootDuel() => DuelManager.Instance.GetDuelMode() == DuelMode.Shoot && DuelManager.Instance.GetLastOffense() == null;
    private void CancelKick()
    {
        AudioManager.Instance.PlaySfx("SfxMenuCancel");
        _pendingKick.Clear();
        CrosshairManager.Instance.HideCrosshairImmediately();
        GameLogger.Info("[BallBehavior] Kick cancelled.", this);
    }
    private bool IsLastAllyTouched(GameManager gm)
        => PossessionManager.Instance.LastPlayer != null
           && PossessionManager.Instance.LastPlayer.ControlType == ControlType.LocalHuman
           && !gm.IsMovementFrozen;
    private void QueueKick(Vector2 pos)
    {
        _pendingKick.QueuePendingKick(pos);
        ShowCrosshair(pos);
        GameLogger.DebugLog($"[BallBehavior] Queued kick at {pos}", this);
    }
    private bool TryGoalDuel(Player player, Vector2 pos)
        => GoalDuelInitiator.Instance.TryStartGoalDuelIfValidTarget(player, pos, false);
    private void ShowCrosshair(Vector2 pos)
    {
        CrosshairManager.Instance.ShowCrosshair(pos);
        GameLogger.DebugLog($"[BallBehavior] Showed crosshair at {pos}", this);
    }
    private bool ReadyToKickoff(GameManager gm) => gm.CurrentPhase == GamePhase.Kickoff && KickoffManager.Instance.IsKickoffReady;
    private void StartMatch()
    {
        GameManager.Instance.SetGamePhase(GamePhase.Battle);
        GameManager.Instance.UnfreezeGame();
        GameLogger.Info("[BallBehavior] Match started after Kickoff.", this);
    }
    private void KickOrQueueImmediate(Vector2 pos)
    {
        KickBallToNetworkAware(pos);
        CrosshairManager.Instance.HideCrosshairAfterDelay();
        GameLogger.DebugLog("[BallBehavior] Kick or queue immediate executed.", this);
    }
    private void QueueKickDuringFreeze(Vector2 pos)
    {
        AudioManager.Instance.PlaySfx("SfxCrosshair");
        _pendingKick.QueuePendingKick(pos);
        ShowCrosshair(pos);
        GameLogger.DebugLog("[BallBehavior] Queued kick during freeze.", this);
    }
    private bool IsDefenseQueue(GameManager gm, Player player)
        => player.ControlType != ControlType.LocalHuman && gm.IsMovementFrozen;
    private bool CanProcessPendingKick(Player player)
        => _pendingKick.HasPendingKick && player != null
           && player.ControlType == ControlType.LocalHuman
           && !player.IsStunned
           && DuelManager.Instance.IsDuelResolved();
    private void ExecutePendingKick(Player player)
    {
        _pendingKick.TryConsumePendingKick(out var target);
        GameLogger.DebugLog($"[BallBehavior] Executing pending kick to {target}.", this);
        bool started = GoalDuelInitiator.Instance.TryStartGoalDuelIfValidTarget(player, target, false);
        if (!started) KickBallToNetworkAware(target);
        CrosshairManager.Instance.HideCrosshairImmediately();
    }
    private bool CanProcessSwipe(Player player)
        => player != null
           && player.ControlType == ControlType.LocalHuman
           && DuelManager.Instance.IsDuelResolved()
           && !GameManager.Instance.IsMovementFrozen
           && !player.IsStunned;
    private void ProcessPendingSwipe(Player player)
    {
        if (_pendingSwipe.TryConsumePendingSwipeUp())
        {
            GameLogger.DebugLog("[BallBehavior] Processing pending swipe up.", this);
            GoalDuelInitiator.Instance.TryStartGoalDuelIfValidSwipe(player, false);
        }
    }
    private void DribbleTowardsPlayer(Player player)
    {
        if (player == null) return;

        Vector3 target = player.transform.position + player.transform.forward * dribbleOffsetForward;
        target.y = transform.position.y;
        target.z -= dribbleOffsetZ;

        transform.position = Vector3.Lerp(transform.position, target, dribbleSpeed * Time.deltaTime);
        // GameLogger.DebugLog($"[BallBehavior] Dribbling towards {target}", this); // Uncomment if you want dribble logs
    }

    #endregion

    #region Possession Callbacks

    private void OnPossessionGained(Player player)
    {
        if (!UIManager.Instance.IsStatusLocked)
        {
            UIManager.Instance.HideStatus();
            OnSetStatusPlayer?.Invoke(player);
        }
        _isPossessed = true;
        rb.isKinematic = true;

        Vector3 spawnPos = player.transform.position + player.transform.forward * 0.5f;
        spawnPos.y = transform.position.y;
        transform.position = spawnPos;

        if (player.ControlType == ControlType.LocalHuman)
            HandleAllyPendingKickOrControl(player);
        else
            ResetPendingInputs();

#if PHOTON_UNITY_NETWORKING
        if (GameManager.Instance.IsMultiplayer && player.ControlType == ControlType.LocalHuman)
            _photonView.RequestOwnership();
#endif
        GameLogger.DebugLog($"[BallBehavior] Possession gained by {player?.PlayerName}", this);
    }

    private void OnPossessionLost(Player player)
    {
        _isPossessed = false;
        rb.isKinematic = false;
        GameLogger.DebugLog($"[BallBehavior] Possession lost by {player?.PlayerName}", this);
    }

    private void HandleAllyPendingKickOrControl(Player player)
    {
        if (CanProcessSwipe(player) && _pendingSwipe.TryConsumePendingSwipeUp())
        {
            if (GoalDuelInitiator.Instance.TryStartGoalDuelIfValidSwipe(player, true))
                player.ShowBubbleVoley();
            CrosshairManager.Instance.HideCrosshairImmediately();
            GameLogger.DebugLog("[BallBehavior] Consumed pending swipe upon possession.", this);
            return;
        }

        if (_pendingKick.HasPendingKick && !player.IsStunned)
        {
            _pendingKick.TryConsumePendingKick(out var target);
            player.ShowBubbleVoley();
            bool started = GoalDuelInitiator.Instance.TryStartGoalDuelIfValidTarget(player, target, true);
            if (!started) KickBallToNetworkAware(target);
            CrosshairManager.Instance.HideCrosshairImmediately();
            GameLogger.DebugLog("[BallBehavior] Consumed pending kick upon possession.", this);
            return;
        }

        ResetPendingInputs();
        DuelLogManager.Instance.AddGainPossession(player);
        player.Control();
        GameLogger.DebugLog("[BallBehavior] Player is now controlling the ball.", this);
    }

    #endregion

#if PHOTON_UNITY_NETWORKING
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
            if (!rb.isKinematic)
                rb.velocity = (Vector3)stream.ReceiveNext();
        }
    }
    public void OnPhotonInstantiate(PhotonMessageInfo info) { /* Optional init */ }
#endif
}
