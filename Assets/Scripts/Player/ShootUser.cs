using UnityEngine;

public class ShootUser : MonoBehaviour
{
    public static ShootUser Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float shootGoalDistance = 2.2f;

    private void Awake()
    {
        // Classic singleton pattern
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
                ShootTriangle.Instance.SetTriangleFromUser(PossessionManager.Instance.PossessionPlayer, screenPos);
                //ShootTriangle.Instance.SetTriangleFromPlayer(PossessionManager.Instance.PossessionPlayer, worldCoord);
                //ShootTriangle.Instance.SetTriangleFromPlayer(player, GameManager.Instance.GetOppGoal.position);
                StartDuel(isDirect);
                return true;
            }
        }
        else
        {
            Debug.Log("Raycast did NOT hit anything on 'Goal' layer.");
        }
        return false;
    }

    private void StartDuel(bool isDirect) {
        GameManager.Instance.FreezeGame();
        DuelManager.Instance.StartDuel(DuelMode.Shoot);
        DuelManager.Instance.RegisterTrigger(PossessionManager.Instance.PossessionPlayer.gameObject, isDirect);
        UIManager.Instance.SetUserRole(Category.Shoot, 0, PossessionManager.Instance.PossessionPlayer);
        UIManager.Instance.SetButtonDuelToggleVisible(true);
        ShootTriangle.Instance.SetTriangleVisible(true);
    }
}
