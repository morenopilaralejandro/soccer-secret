using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DuelMode { Field, Shoot }
public enum DuelAction { Offense, Defense }
public enum DuelCommand { Phys, Skill, Secret }

public class DuelManager : MonoBehaviour
{
    public static DuelManager Instance { get; private set; }

    public static event Action<DuelParticipant, float> OnSetStatusPlayerAndCommand;

    private List<DuelParticipantData> stagedParticipants = new List<DuelParticipantData>();
    private Duel currentDuel = new Duel();
    private Coroutine unlockStatusCoroutine;
    private float hpMultiplier = 0.1f;
    private float directBonus = 20f;
    private float keeperBonus = 50f;
    private float keeperGoalDistance = 0.5f;

    #region Unity Lifecycle

    private void Awake()
    {
        // Standard Unity Singleton pattern:
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentDuel.IsResolved = true;
    }

    #endregion

    #region Duel Flow

    public void StartDuel(DuelMode mode)
    {
        Debug.Log("Duel started");
        StopAndCleanupUnlockStatus();
        UIManager.Instance.LockStatus();
        ResetDuel();
        currentDuel.Mode = mode;
    }

    public void ResetDuel()
    {
        stagedParticipants.Clear();
        currentDuel.Reset();
    }

    public void CancelDuel()
    {
        currentDuel.IsResolved = true;
        ShootTriangle.Instance.SetTriangleVisible(false);    
        BallTrail.Instance.SetTrailVisible(false);
        unlockStatusCoroutine = StartCoroutine(UnlockStatusRoutine());    
    }

    public bool IsDuelResolved() => currentDuel.IsResolved;
    public DuelMode GetDuelMode() => currentDuel.Mode;
    public List<DuelParticipant> GetDuelParticipants() => currentDuel.Participants;
    public DuelParticipant GetLastOffense() => currentDuel.LastOffense;
    public DuelParticipant GetLastDefense() => currentDuel.LastDefense;

    public DuelAction GetActionByCategory(Category category) 
    {
        if (category == Category.Block || category == Category.Catch) {
            return DuelAction.Defense;
        } else {
            return DuelAction.Offense;
        }
    }

    #endregion

    #region Duel Participation

    public void AddParticipantToDuel(DuelParticipant participant)
    {
        BallBehavior.Instance.ResumeTravel();

        if (currentDuel.IsResolved)
            return;

        if (participant.Category == Category.Shoot) {
            if (!currentDuel.Participants.Any())
                StartBallTravel();
        }

        currentDuel.Participants.Add(participant);


        if (participant.Secret != null)
        {
            Vector3 playerPos = participant.Player.transform.position; // Or however you get the player's position
            SecretManager.Instance.PlaySecretEffect(participant.Secret, playerPos);
            participant.Player.ReduceSp(participant.Secret.Cost);
            if (participant.Category == Category.Shoot) 
            {
                BallTrail.Instance.SetTrailVisible(true);
                BallTrail.Instance.SetTrailMaterial(participant.Secret.Element);
            }
        } else {
            if (participant.Category == Category.Shoot) 
                BallTrail.Instance.SetTrailVisible(false);
        }

        participant.Player.ReduceHp(Mathf.RoundToInt(participant.Player.Lv * hpMultiplier));

        if (participant.Action == DuelAction.Offense)
        {
            currentDuel.AttackPressure += participant.Damage;
            if (participant.Category == Category.Shoot && participant.IsDirect) 
                currentDuel.AttackPressure += directBonus;
            currentDuel.LastOffense = participant;
            OnSetStatusPlayerAndCommand?.Invoke(participant, currentDuel.AttackPressure);
            Debug.Log($"Offense action increases attack pressure +{participant.Damage}");
        }
        else
        {
            ResolveDefense(participant);
        }
    }

    private void ResolveDefense(DuelParticipant defender)
    {
        if (currentDuel.LastOffense == null)
        {
            Debug.LogWarning("No offense present before defense.");
            return;
        }

        currentDuel.LastDefense = defender;

        ApplyElementalEffectiveness(currentDuel.LastOffense, defender);
        if (defender.Category == Category.Block && defender.Player.IsKeeper && GameManager.Instance.GetDistanceToAllyGoal(defender.Player) < keeperGoalDistance)
        {
            defender.Damage *= keeperBonus;
            Debug.Log("Keeper gets a block bonus!");
        }

        if (defender.Damage >= currentDuel.AttackPressure)
        {
            OnSetStatusPlayerAndCommand?.Invoke(defender, 0f);
            Debug.Log($"{defender.Player.name} stopped the attack! (-{defender.Damage})");
            EndDuel(winningParticipant: defender, winnerAction: DuelAction.Defense);
        }
        else
        {
            currentDuel.AttackPressure -= defender.Damage;
            OnSetStatusPlayerAndCommand?.Invoke(defender, 0f);
            Debug.Log($"Partial block. Attack pressure now {currentDuel.AttackPressure}");

            defender.Player.Stun();

            if (currentDuel.Mode == DuelMode.Field || defender.Category == Category.Catch)
            {
                Debug.Log("Partial block ends the duel.");
                EndDuel(winningParticipant: currentDuel.LastOffense, winnerAction: DuelAction.Offense);
            }
            // Else: duel continues for next defense
        }
    }

    private void ApplyElementalEffectiveness(DuelParticipant offense, DuelParticipant defense)
    {
        var elements = ElementManager.Instance; // Singleton assumed

        if (elements.IsEffective(defense.CurrentElement, offense.CurrentElement))
        {
            defense.Damage *= 2f;
            Debug.Log("Defense element is effective!");
        }
        else if (elements.IsEffective(offense.CurrentElement, defense.CurrentElement))
        {
            currentDuel.AttackPressure -= offense.Damage;
            offense.Damage *= 2;
            currentDuel.AttackPressure += offense.Damage;
            OnSetStatusPlayerAndCommand?.Invoke(offense, currentDuel.AttackPressure);
            Debug.Log("Offense element is effective!");
        }
    }

    private void EndDuel(DuelParticipant winningParticipant, DuelAction winnerAction)
    {
        winningParticipant.Player.ReduceHp(Mathf.RoundToInt(winningParticipant.Player.Lv * hpMultiplier));
        currentDuel.IsResolved = true;
        UIManager.Instance.ShowTextDuelResult(winningParticipant);
        ShootTriangle.Instance.SetTriangleVisible(false);

        if (winnerAction == DuelAction.Defense)
        {
            BallBehavior.Instance.CancelTravel();
            BallBehavior.Instance.GainPossession(winningParticipant.Player);
            currentDuel.LastOffense.Player.Stun();
        }

        BallTrail.Instance.SetTrailVisible(false);
        unlockStatusCoroutine = StartCoroutine(UnlockStatusRoutine());
        Debug.Log("Duel ended");
    }

    #endregion

    #region Ball and Status Control

    public void StartBallTravel()
    {
        BallBehavior.Instance.ReleasePossession();
        BallBehavior.Instance.StartTravelToPoint(ShootTriangle.Instance.GetRandomPoint());
    }

    private void StopAndCleanupUnlockStatus()
    {
        if (unlockStatusCoroutine != null)
        {
            StopCoroutine(unlockStatusCoroutine);
            unlockStatusCoroutine = null;
        }
        if (currentDuel.IsResolved) {
            UIManager.Instance.HideStatus();
            if (BallBehavior.Instance.PossessionPlayer != null)
                UIManager.Instance.SetStatusPlayer(BallBehavior.Instance.PossessionPlayer);
        }
    }

    private IEnumerator UnlockStatusRoutine()
    {
        const float unlockDelay = 2f;
        yield return new WaitForSeconds(unlockDelay);

        StopAndCleanupUnlockStatus(); // <--- Now call it AFTER delay

        UIManager.Instance.UnlockStatus();
    }

    #endregion

    #region Participant Registration

    public void RegisterTrigger(GameObject obj, bool isDirect)
    {
        var pd = new DuelParticipantData { GameObject = obj, IsDirect = isDirect };
        stagedParticipants.Add(pd);
        TryFinalizeParticipant(pd);
    }

    public void RegisterUISelections(int index, Category category, DuelAction action, DuelCommand command, Secret secret)
    {
        if (index < 0 || index >= stagedParticipants.Count)
        {
            Debug.LogError("Invalid participant index");
            return;
        }
        var pd = stagedParticipants[index];
        pd.Category = category;
        pd.Action = action;
        pd.Command = command;
        pd.Secret = secret;
        TryFinalizeParticipant(pd);
    }

    private void TryFinalizeParticipant(DuelParticipantData pd)
    {
        if (!pd.IsComplete) return;

        var participant = new DuelParticipant(
            pd.GameObject,
            pd.Category.Value,
            pd.Action.Value,
            pd.Command.Value,
            pd.Secret,
            pd.IsDirect
        );

        Debug.Log($"Created participant: {participant.Player.name}");
        AddParticipantToDuel(participant);
    }

    #endregion
}
