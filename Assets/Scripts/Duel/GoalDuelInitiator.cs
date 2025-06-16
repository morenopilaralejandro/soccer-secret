using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class GoalDuelInitiator : MonoBehaviour
{
    public static GoalDuelInitiator Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float shootGoalDistance = 2.2f;

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

    public bool TryStartGoalDuelIfValidTarget(Vector2 screenPos, bool isDirect)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Debug.DrawRay(ray.origin, ray.direction * Mathf.Infinity, Color.red, 2f);

        int goalLayerMask = LayerMask.GetMask("GoalTouchArea");
        Debug.Log($"Attempting to raycast to Goal layer. Mask={goalLayerMask}, TapPos={screenPos}");
        if (Physics.Raycast(ray, out RaycastHit hitGoal, Mathf.Infinity, goalLayerMask))
        {
            Debug.Log($"Raycast hit: {hitGoal.collider.name} on layer {LayerMask.LayerToName(hitGoal.collider.gameObject.layer)} Tag={hitGoal.collider.tag}");
            if (
                GameManager.Instance.GetDistanceToOppGoal(PossessionManager.Instance.PossessionPlayer) < shootGoalDistance
                && hitGoal.collider.CompareTag("Opp")
                && DuelManager.Instance.IsDuelResolved()
                && !GameManager.Instance.IsMovementFrozen)
            {
                Debug.Log("Tap on OPP GOAL detected. Initiating Duel.");
                TryStartGoalNetworkSafe(screenPos, isDirect);
                return true;
            }
        }
        else
        {
            Debug.Log("Raycast did NOT hit anything on 'Goal' layer.");
        }
        return false;
    }

    private void TryStartGoalNetworkSafe(Vector2 screenPos, bool isDirect)
    {
#if PHOTON_UNITY_NETWORKING
        if (IsMultiplayer && !PhotonNetwork.IsMasterClient)
            return;
#endif

        // Only Master (or local) sets triangle and starts duel
        ShootTriangle.Instance.SetTriangleFromTap(PossessionManager.Instance.PossessionPlayer, screenPos);
        StartDuel(isDirect);
    }

    public void StartDuel(bool isDirect)
    {
        DuelManager.Instance.StartDuel(DuelMode.Shoot);
        ShootTriangle.Instance.SetTriangleVisible(true);
        DuelManager.Instance.RegisterTrigger(PossessionManager.Instance.PossessionPlayer.gameObject, isDirect);
        UIManager.Instance.SetDuelSelection(PossessionManager.Instance.PossessionPlayer.TeamIndex, Category.Shoot, 0, PossessionManager.Instance.PossessionPlayer);
        if (PossessionManager.Instance.PossessionPlayer.ControlType == ControlType.LocalHuman)
            UIManager.Instance.BeginDuelSelectionPhase();
    }
}
