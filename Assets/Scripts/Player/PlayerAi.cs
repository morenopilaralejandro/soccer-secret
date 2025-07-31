using System.Collections.Generic;
using UnityEngine;

public enum AiDifficulty { Easy, Normal, Hard }
public enum AiState { Idle, KickOff, ChaseBall, Attack, Defend, Keeper, Pass, Shoot }

public class PlayerAi : MonoBehaviour
{
    #region Editor References

    [Header("Player / Field References")]
    [SerializeField] private Player player;
    [SerializeField] private Transform ballTransform;
    [SerializeField] private Transform allyGoalTransform;
    [SerializeField] private Transform oppGoalTransform;

    [Header("Team")]
    [SerializeField] private List<Player> teammates = new List<Player>();
    [SerializeField] private List<Player> opponents = new List<Player>();

    #endregion

    #region AI Tuning

    [Header("AI Settings")]
    [SerializeField] private AiDifficulty aiDifficulty;
    [SerializeField] private AiState currentState = AiState.Idle;
    [SerializeField] private float shootGoalDistance = 2f;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private float defendDistance = 1.2f;

    private float closeDistanceOpponent;
    private float closeDistanceBall;
    //private float closeDistanceOppGoal = 1.5f;
    //private float closeDistanceAllyGoal = 0.8f;

    #endregion

    #region Passing Logic

    private Player lastPassReceiver;
    private float lastPassTime = -100f;
    private const float minPassReturnDistance = 1.5f;
    private const float passLoopCooldown = 2f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {

    }

    private void Start()
    {
        if (!player) player = GetComponent<Player>();
        AssignGameReferences();
        InitializeDistances();
    }

    private void Update()
    {
        UpdateCurrentAiState();
        ExecuteCurrentAiState();
    }
    #endregion

    #region Initialization

    private void AssignGameReferences()
    {
        var gm = GameManager.Instance;

        if (!ballTransform)
            ballTransform = GameObject.FindGameObjectWithTag("Ball")?.transform;

        if (!allyGoalTransform && player)
            allyGoalTransform = gm.GetAllyGoal(player)?.transform;

        if (!oppGoalTransform && player)
            oppGoalTransform = gm.GetOppGoal(player)?.transform;

        if (player)
        {
            teammates = gm.Teams[player.TeamIndex].players;
            int opponentTeamIndex = gm.GetLocalTeamIndex();
            opponents = gm.Teams[opponentTeamIndex].players;

            aiDifficulty = AiDifficulty.Hard;
        }
    }

    private void InitializeDistances()
    {
        switch (player.Coord.Position)
        {
            case Position.Gk:
                closeDistanceOpponent = 0.5f;
                closeDistanceBall = 0.5f;
                break;
            case Position.Df:
                closeDistanceOpponent = 2f;
                closeDistanceBall = 2f;
                break;
            default:
                closeDistanceOpponent = 10f;
                closeDistanceBall = 10f;
                break;
        }
    }

    #endregion

    #region AI State & Decision Logic

    private void UpdateCurrentAiState()
    {
        var gm = GameManager.Instance;
        if (gm.CurrentPhase == GamePhase.KickOff) { currentState = AiState.KickOff; return; }
        if (IsFrozenOrLockedOut()) { currentState = AiState.Idle; return; }
        if (IsInUnresolvedDuel()) { currentState = AiState.Idle; return; }

        if (player.IsPossession)
        {
            if (HasValidPassTarget()) currentState = AiState.Pass;
            else if (IsInShootingRange()) currentState = AiState.Shoot;
            else currentState = AiState.Attack;
        }
        else if (player.Coord.Position == Position.Gk)
        {
            currentState = AiState.Keeper;
        }
        else if (OpponentHasBall() && IsOpponentInRange())
        {
            currentState = AiState.Defend;
        }
        else if (IsBallFree() && IsBallInRange())
        {
            currentState = AiState.ChaseBall;
        }
        else
        {
            currentState = (player.Coord.Position == Position.Df) ? AiState.Defend : AiState.Attack;
        }
    }

    private bool IsFrozenOrLockedOut()
    {
        var gm = GameManager.Instance;
        return gm.IsMovementFrozen || gm.IsTimeFrozen ||
               player.IsStunned || player.IsKicking || player.IsControlling;
    }

    private bool IsInUnresolvedDuel()
    {
        var duel = DuelManager.Instance;
        return duel && !duel.IsDuelResolved() && duel.GetLastOffense()?.Player == player;
    }

    private bool OpponentHasBall()
    {
        var possessor = PossessionManager.Instance.CurrentPlayer;
        return possessor && possessor.ControlType != ControlType.Ai;
    }

    private bool IsOpponentInRange()
    {
        var op = PossessionManager.Instance.CurrentPlayer;
        return op && Vector3.Distance(player.transform.position, op.transform.position) < closeDistanceOpponent;
    }

    private bool IsBallFree()
    {
        return PossessionManager.Instance.CurrentPlayer == null;
    }

    private bool IsBallInRange()
    {
        return ballTransform && Vector3.Distance(player.transform.position, ballTransform.position) < closeDistanceBall;
    }

    private bool HasValidPassTarget()
    {
        return player.Coord.Position != Position.Fw && GetBestPassTeammate() != null;
    }

    private bool IsInShootingRange()
    {
        return GameManager.Instance.GetDistanceToOppGoal(player) < shootGoalDistance;
    }

    #endregion

    #region AI Actions

    private void ExecuteCurrentAiState()
    {
        switch (currentState)
        {
            case AiState.Idle:      break;
            case AiState.KickOff:   ActKickOff(); break;
            case AiState.ChaseBall: MoveTowards(ballTransform?.position ?? player.transform.position); break;
            case AiState.Attack:    ActAttack(); break;
            case AiState.Pass:      PerformPass(); break;
            case AiState.Shoot:     ShootAtGoal(); break;
            case AiState.Defend:    ActDefend(); break;
            case AiState.Keeper:    ActKeeper(); break;
        }
    }

    private void ActKickOff()
    {
        if (!IsKickOffPlayer() || !GameManager.Instance.IsKickOffReady)
            return;
        var target = GetKickOffPassTarget();
        GameManager.Instance.SetGamePhase(GamePhase.Battle);
        GameManager.Instance.UnfreezeGame();
        if (target) BallBehavior.Instance.KickBall(target.transform.position);
    }

    private bool IsKickOffPlayer() => player.IsPossession;

    private Player GetKickOffPassTarget()
    {
        Player best = null; float minDist = float.MaxValue;
        foreach (var mate in teammates)
        {
            if (mate == player || mate.IsKeeper || mate.IsStunned) continue;
            float dist = Vector3.Distance(player.transform.position, mate.transform.position);
            if (dist < minDist) { minDist = dist; best = mate; }
        }
        return best;
    }

    private void ActKeeper()
    {
        var basePos = allyGoalTransform ? allyGoalTransform.position : player.DefaultPosition;
        MoveTowards(basePos);
        // Additional keeper logic can be added here (intercept, block, patrol, etc)
    }

    private void ActAttack()
    {
        // Move toward opp goal; separate if crowded
        Vector3 baseTarget = oppGoalTransform ? oppGoalTransform.position : player.DefaultPosition;
        Vector3 separation = Vector3.zero;
        int closeTeammates = 0;

        foreach (var mate in teammates)
        {
            if (mate == player || mate.IsStunned) continue;
            float dist = Vector3.Distance(player.transform.position, mate.transform.position);
            if (dist < attackDistance)
            {
                separation += (player.transform.position - mate.transform.position) / dist;
                closeTeammates++;
            }
        }

        if (closeTeammates > 0)
            separation /= closeTeammates;

        MoveTowards(baseTarget + separation);
    }

    private void ActDefend()
    {
        var target = player.DefaultPosition;

        var op = PossessionManager.Instance.CurrentPlayer;
        if (op && Vector3.Distance(player.transform.position, op.transform.position) <= closeDistanceOpponent)
        {
            Vector3 separation = Vector3.zero; int count = 0;
            foreach (var mate in teammates)
            {
                if (mate == player || mate.IsStunned) continue;
                float dist = Vector3.Distance(player.transform.position, mate.transform.position);
                if (dist < defendDistance)
                {
                    separation += (player.transform.position - mate.transform.position) / dist;
                    count++;
                }
            }
            if (count > 0) separation /= count;
            target = op.transform.position + separation;
        }

        MoveTowards(target);
    }

    private void MoveTowards(Vector3 target)
    {
        float speed = player.GetMoveSpeed();
        target.y = player.DefaultPosition.y;
        var next = Vector3.MoveTowards(player.transform.position, target, speed);
        player.transform.position = BoundsClamp.Clamp(next);
    }

    private void PerformPass()
    {
        var mate = GetBestPassTeammate();
        if (!mate) return;
        BallBehavior.Instance.KickBall(mate.transform.position);
        lastPassReceiver = mate;
        lastPassTime = Time.time;
    }

    private Player GetBestPassTeammate()
    {
        float myDist = GameManager.Instance.GetDistanceToOppGoal(player);
        Player best = null; float bestDist = myDist;

        foreach (var mate in teammates)
        {
            if (mate == player || mate.IsKeeper || mate.IsStunned) continue;
            if (mate == lastPassReceiver && Time.time - lastPassTime < passLoopCooldown) continue;
            if (Vector3.Distance(player.transform.position, mate.transform.position) < minPassReturnDistance) continue;

            float mateDist = GameManager.Instance.GetDistanceToOppGoal(mate);
            if (mateDist < bestDist)
            {
                bestDist = mateDist;
                best = mate;
            }
        }
        return best;
    }

    private void ShootAtGoal()
    {
        GoalDuelInitiator.Instance.TryStartGoalDuelIfValidSwipe(player, false);
    }

    #endregion

    #region Duel AI (Public API)

    public void SetAiDifficulty(AiDifficulty diff) => aiDifficulty = diff;

    public DuelCommand GetCommandByCategory(Category category)
    {
        switch (aiDifficulty)
        {
            case AiDifficulty.Easy:
                return GetBasicCommand();
            case AiDifficulty.Normal:
                if (Random.value < 0.4f && HasAffordableSecret(category))
                    return DuelCommand.Secret;
                return GetBasicCommand();
            case AiDifficulty.Hard:
                return HasAffordableSecret(category) ? DuelCommand.Secret : GetBasicCommand();
            default: return DuelCommand.Phys;
        }
    }

    private DuelCommand GetBasicCommand()
        => (player.GetStat(PlayerStats.Body) > player.GetStat(PlayerStats.Control)) ? DuelCommand.Phys : DuelCommand.Skill;

    public Secret GetSecretByCommandAndCategory(DuelCommand cmd, Category cat)
    {
        if (cmd != DuelCommand.Secret) return null;
        return (aiDifficulty == AiDifficulty.Normal) ?
            GetRandomAffordableSecret(cat) :
            GetBestAffordableSecret(cat);
    }

    private bool HasAffordableSecret(Category cat)
    {
        int sp = player.GetStat(PlayerStats.Sp);
        foreach (var s in player.CurrentSecret)
            if (s && s.Category == cat && s.Cost <= sp) return true;
        return false;
    }

    private Secret GetRandomAffordableSecret(Category cat)
    {
        int sp = player.GetStat(PlayerStats.Sp);
        var candidates = new List<Secret>();
        foreach (var s in player.CurrentSecret)
            if (s && s.Category == cat && s.Cost <= sp)
                candidates.Add(s);
        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private Secret GetBestAffordableSecret(Category cat)
    {
        int sp = player.GetStat(PlayerStats.Sp);
        Secret best = null; int bestPower = int.MinValue;
        foreach (var s in player.CurrentSecret)
            if (s && s.Category == cat && s.Cost <= sp && s.Power > bestPower)
            {
                best = s; bestPower = s.Power;
            }
        return best;
    }

    public void RegisterAiSelections(int teamIdx, Category cat)
    {
        var dm = DuelManager.Instance; if (!dm) return;
        DuelCommand cmd = GetCommandByCategory(cat);
        Secret s = cmd == DuelCommand.Secret ? GetSecretByCommandAndCategory(cmd, cat) : null;
        UIManager.Instance.DuelSelectionMade(teamIdx, cmd, s);
    }

    #endregion
}
