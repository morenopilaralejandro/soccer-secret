using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class GoalDuelInitiator : MonoBehaviour
{
    public static GoalDuelInitiator Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float shootGoalDistance = 2.2f;
    [SerializeField] private GoalTrigger oppGoal;

    private Player _cachedPlayer;
#if PHOTON_UNITY_NETWORKING
    private bool IsMultiplayer => PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined;
#else
    private bool IsMultiplayer => false;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Intentionally left blank
    }

    public bool TryStartGoalDuelIfValidSwipe(Player player, bool isDirect)
    {
        _cachedPlayer = player;
        oppGoal = GameManager.Instance.GetOppGoal(_cachedPlayer);
        float distanceToGoal = GameManager.Instance.GetDistanceToOppGoal(_cachedPlayer);
        if (distanceToGoal < shootGoalDistance)
        {
            ShootTriangle.Instance.SetTriangleFromPlayer(_cachedPlayer, oppGoal.transform.position);
            TryStartGoalNetworkSafe(isDirect);
            return true;
        }
        else
        {
            GameLogger.Info("[GoalDuelInitiator] Player not close enough to shoot goal.", this);
        }
        return false;
    }

    public bool TryStartGoalDuelIfValidTarget(Player player, Vector2 screenPos, bool isDirect)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Debug.DrawRay(ray.origin, ray.direction * Mathf.Infinity, Color.red, 2f);

        int goalLayerMask = LayerMask.GetMask("GoalTouchArea");
        GameLogger.DebugLog($"[GoalDuelInitiator] Attempting to raycast to Goal layer. Mask={goalLayerMask}, TapPos={screenPos}", this);

        if (Physics.Raycast(ray, out RaycastHit hitGoal, Mathf.Infinity, goalLayerMask))
        {
            _cachedPlayer = player;
            oppGoal = GameManager.Instance.GetOppGoal(_cachedPlayer);
            GameLogger.DebugLog($"[GoalDuelInitiator] Raycast hit: {hitGoal.collider.name} on layer {LayerMask.LayerToName(hitGoal.collider.gameObject.layer)} Tag={hitGoal.collider.tag}", this);

            if (GameManager.Instance.GetDistanceToOppGoal(_cachedPlayer) < shootGoalDistance
                && hitGoal.collider.CompareTag("Opp")
                && DuelManager.Instance.IsDuelResolved()
                && !GameManager.Instance.IsMovementFrozen)
            {
                GameLogger.DebugLog("[GoalDuelInitiator] Tap on OPP GOAL detected. Initiating Duel.", this);
                ShootTriangle.Instance.SetTriangleFromTap(_cachedPlayer, screenPos);
                TryStartGoalNetworkSafe(isDirect);
                return true;
            }
        }
        else
        {
            GameLogger.Info("[GoalDuelInitiator] Raycast did NOT hit anything on 'Goal' layer.", this);
        }
        return false;
    }

    private void TryStartGoalNetworkSafe(bool isDirect)
    {
#if PHOTON_UNITY_NETWORKING
        if (IsMultiplayer && !PhotonNetwork.IsMasterClient)
            return;
#endif

        // Only Master (or local) sets triangle and starts duel

        StartDuel(isDirect);
    }

    public void StartDuel(bool isDirect)
    {
        GameLogger.Info($"[GoalDuelInitiator] StartDuel isDirect: {isDirect}", this);
        DuelLogManager.Instance.AddActionShoot(_cachedPlayer);
        DuelManager.Instance.StartDuel(DuelMode.Shoot);
        ShootTriangle.Instance.SetTriangleVisible(true);
        DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, isDirect);
        UIManager.Instance.SetDuelSelection(_cachedPlayer.TeamIndex, Category.Shoot, 0, _cachedPlayer);
        UIManager.Instance.SetShootTeamIndex(_cachedPlayer.TeamIndex);
        UIManager.Instance.BeginDuelSelectionPhase();
    }
}
