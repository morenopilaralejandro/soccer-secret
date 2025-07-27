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
    [SerializeField] private float dribbleOffset = 0.5f;
    [SerializeField] private float minKickForce = 3f;
    [SerializeField] private float maxKickForce = 6f;
    [SerializeField] private float minMagnitude = 1f;
    [SerializeField] private float maxMagnitude = 3f;

    private readonly PendingKickHandler _pendingKick = new PendingKickHandler();
    private readonly PendingSwipeHandler _pendingSwipe = new PendingSwipeHandler();
    private bool _isPossessed;
    private bool _wasMovementFrozen;

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

        if (IsKickOffWaiting()) ResetPendingInputs();
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
        if (gm.CurrentPhase == GamePhase.KickOff && !gm.IsKickOffReady)
        {
            gm.SetIsKickOffReady(true);
            if (player == null || player.ControlType != ControlType.LocalHuman) return;
        }

        // Possessed by local human
        if (_isPossessed && player.ControlType == ControlType.LocalHuman)
        {
            GameLogger.DebugLog("[BallBehavior] Ball is possessed by local human, processing tap.", this);
            if (TryGoalDuel(player, screenPos)) return;
            ShowCrosshair(screenPos);
            if (ReadyToKickOff(gm)) StartMatch();
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
        if (InputManager.Instance.IsDragging) return;
        if (dir == SwipeDetector.SwipeDirection.Up) TryShootOrQueue();
    }

    private void HandleActionKey() => TryShootOrQueue();

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

        PossessionManager.Instance.Release();
    }

    #endregion

    #region Helpers

    private bool IsKickOffWaiting() => GameManager.Instance.CurrentPhase == GamePhase.KickOff && !GameManager.Instance.IsKickOffReady;
    private void ResetPendingInputs()
    {
        //GameLogger.DebugLog("[BallBehavior] Resetting pending inputs.", this);
        CrosshairManager.Instance.HideCrosshairImmediately();
        _pendingKick.Clear(); _pendingSwipe.Clear();
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
    private bool ReadyToKickOff(GameManager gm) => gm.CurrentPhase == GamePhase.KickOff && gm.IsKickOffReady;
    private void StartMatch()
    {
        GameManager.Instance.SetGamePhase(GamePhase.Battle);
        GameManager.Instance.UnfreezeGame();
        GameLogger.Info("[BallBehavior] Match started after KickOff.", this);
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
        Vector3 forwardOffset = player.transform.forward * dribbleOffset;
        Vector3 target = player.transform.position + forwardOffset;
        target.y = transform.position.y;
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
            player.ShowBubbleVoley();
            GoalDuelInitiator.Instance.TryStartGoalDuelIfValidSwipe(player, true);
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
