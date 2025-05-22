using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AiDifficulty { Easy, Hard }

public class PlayerAi : MonoBehaviour
{
    public AiDifficulty AiDifficulty => aiDifficulty;

    [SerializeField] private Player player;
    [SerializeField] private Transform ballTransform;
    [SerializeField] private Transform oppGoalTransform;
    [SerializeField] private AiDifficulty aiDifficulty;
    [SerializeField] private float ballCloseDistance;
    [SerializeField] private float shootGoalDistance = 1.3f;


    // Start is called before the first frame update
    void Start()
    {
        ballCloseDistance = player.IsKeeper ? 0.3f : 1.5f;
        aiDifficulty = AiDifficulty.Hard;
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.Instance.IsMovementFrozen && !GameManager.Instance.IsTimeFrozen && !player.IsControlling && !player.IsKicking && !player.IsStunned)
        {
            if (DuelManager.Instance.IsDuelResolved()) 
            {
                MoveAi();
            } else {
                if (!DuelManager.Instance.GetLastOffense().Player == player) 
                {
                    MoveAi();                    
                }
            }
        }
        if (!GameManager.Instance.IsMovementFrozen && !GameManager.Instance.IsTimeFrozen && DuelManager.Instance.IsDuelResolved() && player.IsPossession && GameManager.Instance.GetDistanceToOppGoal(player) < shootGoalDistance)
            ShootAi();
    }

    private void MoveAi()
    {
        Vector3 targetPosition = player.DefaultPosition;

        if (!player.IsPossession) 
        {
            if (BallBehavior.Instance.PossessionPlayer && BallBehavior.Instance.PossessionPlayer.IsAlly != player.IsAlly) {
                float distanceToBall = Vector3.Distance(player.transform.position, ballTransform.position);

                if (distanceToBall <= ballCloseDistance)
                {
                    // Move towards the ball
                    targetPosition = ballTransform.position;
                }
            } else {
                targetPosition = player.DefaultPosition;
            }
        } else {
            if (!player.IsKeeper) {
                targetPosition = oppGoalTransform.position;
            }
        }

        float moveSpeed = player.GetMoveSpeed();

        // Move towards the target position
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        player.transform.position = newPosition;
    }
    
    private void ShootAi() 
    {
        Debug.Log("ShootAi. Initiating Duel.");
        DuelManager.Instance.StartDuel(DuelMode.Shoot);
        DuelManager.Instance.RegisterTrigger(player.gameObject, false);
        ShootTriangle.Instance.SetTriangleFromPlayer(player, oppGoalTransform.position);
        ShootTriangle.Instance.SetTriangleVisible(true);
        RegisterAiSelections(0, Category.Shoot);
        DuelManager.Instance.StartBallTravel();
    }

    public void SetAiDifficulty(AiDifficulty aiDifficulty) 
    {
        this.aiDifficulty = aiDifficulty;
    }

    public DuelCommand GetCommandByCategory(Category category) 
    {
        switch (aiDifficulty) 
        {
            case AiDifficulty.Easy:
                return GetBasicCommand();
            case AiDifficulty.Hard:
                if (HasAffordableSecret(category)) {
                    return DuelCommand.Secret;
                } else {
                    return GetBasicCommand();
                }
            default:
                return DuelCommand.Phys;
        }
    }

    private DuelCommand GetBasicCommand() 
    {
        if (player.GetStat(PlayerStats.Body) > player.GetStat(PlayerStats.Control)) {
            return DuelCommand.Phys;
        } else {
            return DuelCommand.Skill;
        }
    }

    public Secret GetSecretByCommandAndCategory(DuelCommand command, Category category) 
    {
        if (command == DuelCommand.Secret) 
        {
            return GetBestAffordableSecret(category);
        }
        return null;
    }

    private Secret GetBestAffordableSecret(Category category)
    {
        int currentSp = player.GetStat(PlayerStats.Sp);
        Secret bestSecret = null;
        int highestPower = int.MinValue;

        foreach (var secret in player.CurrentSecret)
        {
            if (secret != null && secret.Category == category && secret.Cost <= currentSp)
            {
                if (secret.Power > highestPower)
                {
                    highestPower = secret.Power;
                    bestSecret = secret;
                }
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
            {
                return true;
            }
        }
        return false;
    }

    public void RegisterAiSelections(int index, Category category)
    {
        if (DuelManager.Instance != null) {
            DuelAction action = DuelManager.Instance.GetActionByCategory(category);
            DuelCommand command = GetCommandByCategory(category);
            Secret secret = GetSecretByCommandAndCategory(command, category);

            DuelManager.Instance.RegisterUISelections(
                index, 
                category, 
                action, 
                command, 
                secret);
        }
    }
}
