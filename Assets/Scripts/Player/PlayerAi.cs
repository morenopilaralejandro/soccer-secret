using System.Collections.Generic;
using UnityEngine;

public enum AiDifficulty { Easy, Hard }
public enum AiState { Idle, KickOff, ChaseBall, Attack, Defend, Pass, Shoot }

public class PlayerAi : MonoBehaviour
{
    public AiDifficulty Difficulty => aiDifficulty;

    [SerializeField] private Player player;
    [SerializeField] private List<Player> teammates;
    [SerializeField] private Player bestTeammate;

    [SerializeField] private Transform ballTransform;
    [SerializeField] private Transform allyGoalTransform;
    [SerializeField] private Transform oppGoalTransform;
    [SerializeField] private AiDifficulty aiDifficulty = AiDifficulty.Hard;
    [SerializeField] private AiState currentState = AiState.Idle;
    [SerializeField] private float closeDistance;
    [SerializeField] private float shootGoalDistance = 1.5f;
    //[SerializeField] private float goodPassDistance = 2f;

    private void Start()
    {
        InitializeCloseDistance();
        aiDifficulty = AiDifficulty.Hard;

        // If using as an enemy AI, use GetOppPlayers, else GetAllyPlayers
        teammates = GameManager.Instance.GetOppPlayers(); 
    }

    private void Update()
    {
        UpdateState();
        ActInCurrentState();
    }

    #region State Management

    private void UpdateState()
    {
        if (GameManager.Instance.IsKickOffPhase) 
        {
            currentState = AiState.KickOff;
            return;
        }

        if (ShouldBeIdle())
        {
            currentState = AiState.Idle;
            return;
        }

        if (player.IsPossession)
        {
            currentState = HasBetterTeammate() ? AiState.Pass :
                            IsInShootingPosition() ? AiState.Shoot : AiState.Attack;
        }
        else if (IsOpponentPossessingBall())
        {
            currentState = AiState.Defend;
        }
        else if (IsInDuelIdle())
        {
            currentState = AiState.Idle;
        }
        else
        {
            currentState = AiState.ChaseBall;
        }
    }

    private bool ShouldBeIdle()
    {
        var gm = GameManager.Instance;
        return gm.IsMovementFrozen || gm.IsTimeFrozen ||
               player.IsStunned || player.IsKicking || player.IsControlling;
    }

    private bool IsOpponentPossessingBall()
    {
        var possessor = BallBehavior.Instance.PossessionPlayer;
        return possessor != null && possessor.IsAlly != player.IsAlly;
    }

    private bool IsInDuelIdle()
    {
        var duel = DuelManager.Instance;
        return !duel.IsDuelResolved() && duel.GetLastOffense() != null && duel.GetLastOffense().Player == player;
    }

    #endregion

    #region AI Actions

    private void ActInCurrentState()
    {
        switch (currentState)
        {
            case AiState.Idle:
                // Do nothing
                break;
            case AiState.KickOff:
                ActKickOff();
                break;
            case AiState.ChaseBall:
                ActChaseBall();
                break;
            case AiState.Attack:
                MoveTowards(oppGoalTransform.position);
                break;
            case AiState.Pass:
                PassToBestTeammate();
                break;
            case AiState.Shoot:
                ShootAi();
                break;
            case AiState.Defend:
                ActDefend();
                break;
        }
    }

    private void ActChaseBall()
    {
        bool isAttacker = player.Position == Position.Fw || player.Position == Position.Mf;
        MoveTowards(isAttacker ? ballTransform.position : player.DefaultPosition);
    }

    private void ActDefend()
    {
        var opponent = BallBehavior.Instance.PossessionPlayer;
        if (opponent == null) return;

        Vector3 target = player.DefaultPosition;

        if (Vector3.Distance(player.transform.position, opponent.transform.position) <= closeDistance) {
            Vector3 baseTarget = opponent.transform.position;

            // Calculate separation
            Vector3 separation = Vector3.zero;
            int neighborCount = 0;
            foreach (var teammate in teammates)
            {
                if (teammate == player || teammate.IsStunned) continue;
                float dist = Vector3.Distance(player.transform.position, teammate.transform.position);
                if (dist < 1.0f) // 1 unit is very close—tune this value!
                {
                    separation += (player.transform.position - teammate.transform.position) / dist;
                    neighborCount++;
                }
            }
            if (neighborCount > 0)
                separation /= neighborCount;

            target = baseTarget + separation;
        }

        MoveTowards(target);
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        float moveSpeed = player.GetMoveSpeed();
        targetPosition.y = player.DefaultYPosition;
        player.transform.position = Vector3.MoveTowards(player.transform.position, targetPosition, moveSpeed);
    }

    private void PassToBestTeammate()
    {
        bestTeammate = GetBestTeammate();
        if (bestTeammate == null) return;

        BallBehavior.Instance.KickBall(bestTeammate.transform.position);
    }

    private void ShootAi()
    {
        Debug.Log("ShootAi. Initiating Duel.");
        var duel = DuelManager.Instance;

        duel.StartDuel(DuelMode.Shoot);
        duel.RegisterTrigger(player.gameObject, false);
        ShootTriangle.Instance.SetTriangleFromPlayer(player, oppGoalTransform.position);
        ShootTriangle.Instance.SetTriangleVisible(true);
        RegisterAiSelections(0, Category.Shoot);
    }

    private void ActKickOff()
    {
        // Only the kickoff player attempts to pass; others stand still.
        if (IsKickOffPlayer() && GameManager.Instance.IsKickOffReady)
        {
            Player target = GetKickOffPassTarget();
            GameManager.Instance.UnfreezeGame();
            BallBehavior.Instance.KickBall(target.transform.position);
        }
    }

    private bool IsKickOffPlayer()
    {
        return player.IsPossession;
    }

    private Player GetKickOffPassTarget()
    {
        // Choose a nearby teammate, or a predefined kickoff partner.
        // Example: pick closest eligible teammate (not self, not stunned/keeper)
        float minDist = float.MaxValue;
        Player best = null;
        foreach (var teammate in teammates)
        {
            if (teammate == player || teammate.IsKeeper) continue;
            float dist = Vector3.Distance(player.transform.position, teammate.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                best = teammate;
            }
        }
        return best;
    }

    #endregion

    #region Decision Making

    private void InitializeCloseDistance()
    {
        switch (player.Position)
        {
            case Position.Gk:
                closeDistance = 0.3f;
                break;
            case Position.Df:
                closeDistance = 3f;
                break;
            default:
                closeDistance = 10f;
                break;
        }
    }

    private bool IsInShootingPosition()
    {
        return GameManager.Instance.GetDistanceToOppGoal(player) < shootGoalDistance;
    }

    private bool HasBetterTeammate()
    {
        return player.Position == Position.Fw ? false : GetBestTeammate() != null;
    }

    private Player GetBestTeammate()
    {
        float myDistance = GameManager.Instance.GetDistanceToOppGoal(player);
        Player best = null;
        float bestDist = myDistance;

        foreach (var teammate in teammates)
        {
            if (teammate == player || teammate.IsKeeper || teammate.IsStunned)
                continue;

            float dist = GameManager.Instance.GetDistanceToOppGoal(teammate);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = teammate;
            }
        }
        return best;
    }

    #endregion

    #region Duel Commands & Secrets

    public void SetAiDifficulty(AiDifficulty diff) => aiDifficulty = diff;

    public DuelCommand GetCommandByCategory(Category category)
    {
        switch (aiDifficulty)
        {
            case AiDifficulty.Easy:
                return GetBasicCommand();
            case AiDifficulty.Hard:
                return HasAffordableSecret(category) ? DuelCommand.Secret : GetBasicCommand();
            default:
                return DuelCommand.Phys;
        }
    }

    private DuelCommand GetBasicCommand()
    {
        return player.GetStat(PlayerStats.Body) > player.GetStat(PlayerStats.Control)
            ? DuelCommand.Phys
            : DuelCommand.Skill;
    }

    public Secret GetSecretByCommandAndCategory(DuelCommand command, Category category)
    {
        return command == DuelCommand.Secret ? GetBestAffordableSecret(category) : null;
    }

    private Secret GetBestAffordableSecret(Category category)
    {
        int currentSp = player.GetStat(PlayerStats.Sp);
        Secret bestSecret = null;
        int highestPower = int.MinValue;

        foreach (var secret in player.CurrentSecret)
        {
            if (secret != null && secret.Category == category && secret.Cost <= currentSp && secret.Power > highestPower)
            {
                highestPower = secret.Power;
                bestSecret = secret;
            }
        }
        return bestSecret;
    }

    private bool HasAffordableSecret(Category category)
    {
        int currentSp = player.GetStat(PlayerStats.Sp);
        foreach (var secret in player.CurrentSecret)
        {
            if (secret != null && secret.Category == category && secret.Cost <= currentSp)
                return true;
        }
        return false;
    }

    public void RegisterAiSelections(int index, Category category)
    {
        var duel = DuelManager.Instance;
        if (duel == null) return;

        DuelAction action = duel.GetActionByCategory(category);
        DuelCommand command = GetCommandByCategory(category);
        Secret secret = GetSecretByCommandAndCategory(command, category);

        duel.RegisterUISelections(index, category, action, command, secret);
    }

    #endregion
}
