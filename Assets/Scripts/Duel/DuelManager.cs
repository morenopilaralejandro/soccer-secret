using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DuelMode { Field, Shoot }
public enum DuelAction { Offense, Defense }
public enum DuelCommand { Secret, Phys, Skill }

public class DuelManager : MonoBehaviour
{
    public static DuelManager Instance { get; private set; }

    private List<DuelParticipantData> stagedParticipants = new List<DuelParticipantData>();
    private Duel duel = new Duel();
    private Coroutine unlockStatusCoroutine;

    public static event Action<DuelParticipant, float> OnSetStatusPlayerAndCommand;
    // OnSetStatusPlayer?.Invoke(somePlayer);
    // OnSetStatusPlayerAndCommand?.Invoke(someParticipant, somePressure);


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        duel.IsResolved = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ExecuteDuel(Duel duel, DuelParticipant newDefPart)
    {
        Debug.Log("ExecuteDuel");
        // Use persistent duel state!
        var part = newDefPart;

        if (duel.LastOff == null)
        {
            // Sanity: No offense was ever added, cannot duel
            Debug.LogWarning("No offense present before defense.");
            return;
        }

        duel.LastDef = part;

        // Element effectiveness
        if (ElementManager.Instance.IsEffective(part.CurrentElement, duel.LastOff.CurrentElement))
        {
            part.Damage *= 2f;
            Debug.Log("Defense element is effective!");
        }
        else if (ElementManager.Instance.IsEffective(duel.LastOff.CurrentElement, part.CurrentElement))
        {
            duel.AttackPressure -= duel.LastOff.Damage;
            duel.LastOff.Damage *= 2;
            duel.AttackPressure += duel.LastOff.Damage;
            OnSetStatusPlayerAndCommand?.Invoke(duel.LastOff, duel.AttackPressure);
            Debug.Log("Offense element is effective!");
        }

        if (part.Damage >= duel.AttackPressure)
        {
            OnSetStatusPlayerAndCommand?.Invoke(part, 0f);
            Debug.Log($"{part.Player.name} stopped the attack! (-" + part.Damage +")");
            OnDuelEnd(winningPart: part, duel.LastOff, duel.LastDef, winner: "defense");
            // Optionally clear duel.Parts or reset state here!
        }
        else
        {
            duel.AttackPressure -= part.Damage;
            OnSetStatusPlayerAndCommand?.Invoke(part, 0f);
            Debug.Log("Defense action decreases AttackPressure -" + part.Damage);
            StartCoroutine(duel.LastDef.Player.Stun());
            // Duel continues for next defender...
            Debug.Log($"Partial block. AttackPressure now {duel.AttackPressure}");

            if (duel.Mode == DuelMode.Field || duel.LastDef.Category == Category.Catch)
            {
                Debug.Log("Partial block ends the duel");
                OnDuelEnd(winningPart: duel.LastOff, duel.LastOff, duel.LastDef, winner: "offense");
                // Optionally clear duel.Parts or reset state here!
                // You may want to return here to stop further processing.
                return;
            }
        }
    }

    public void AddParticipantToDuel(Duel duel, DuelParticipant participant)
    {
        Debug.Log("AddParticipantToDuel");
        BallBehavior.Instance.ResumeTravel();
        if (duel.IsResolved) 
            return; // Prevent further processing after resolution

        duel.Parts.Add(participant);

        if (participant.Action == DuelAction.Offense)
        {
            // Update persistent state
            duel.AttackPressure += participant.Damage;
            duel.LastOff = participant;
            OnSetStatusPlayerAndCommand?.Invoke(participant, duel.AttackPressure);
            Debug.Log("Offense action increses AttackPressure +" + participant.Damage);
        }
        else // Defense
        {
            // Evaluate only with new defense
            Debug.Log("Defense action -> ExecuteDuel");
            ExecuteDuel(duel, participant);
        }
    }

    private void OnDuelEnd(DuelParticipant winningPart, DuelParticipant lastOff, DuelParticipant lastDef, string winner)
    {
        Debug.Log("OnDuelEnd Winner: " + winner);
        duel.IsResolved = true;
        UIManager.Instance.ShowTextDuelResult(winningPart);
        ShootTriangle.Instance.SetTriangleVisible(false);
        if (winningPart.Action == DuelAction.Defense)
        {
            StartCoroutine(duel.LastOff.Player.Stun());
            BallBehavior.Instance.GainPossession(winningPart.Player);
        }
        if (unlockStatusCoroutine != null)
            StopCoroutine(unlockStatusCoroutine);
        unlockStatusCoroutine = StartCoroutine(UnlockStatus());
    }

    public void OnDuelStart(DuelMode mode) 
    {
        if (unlockStatusCoroutine != null)
        {
            StopCoroutine(unlockStatusCoroutine);
            unlockStatusCoroutine = null;
    
            UIManager.Instance.HideStatus();
            if (BallBehavior.Instance.PossessionPlayer != null) {
                UIManager.Instance.SetStatusPlayer(BallBehavior.Instance.PossessionPlayer);
            }
        }

        UIManager.Instance.LockStatus();
        ResetDuel();
        duel.Mode = mode;
    }

    public void ResetDuel() 
    {
        stagedParticipants.Clear();
        duel.ResetDuel();
    }

    public bool GetDuelIsResolved() 
    {
        return duel.IsResolved;
    }

    public DuelMode GetDuelMode() 
    {
        return duel.Mode;
    }

    public List<DuelParticipant> GetDuelParticipants() {
        return duel.Parts;
    }

    public void StartBallTravel() 
    {
        BallBehavior.Instance.ReleasePossession();
        BallBehavior.Instance.StartTravelToPoint(
            ShootTriangle.Instance.GetRandomPoint()
        );
    }

    public DuelParticipant GetLastOff() {
        return duel.LastOff;
    }

    public DuelParticipant GetLastDef() {
        return duel.LastDef;
    }

    // Call this when a GameObject enters the trigger
    public void RegisterTrigger(GameObject obj)
    {
        var pd = new DuelParticipantData { gameObj = obj };
        stagedParticipants.Add(pd);
        TryFinalizeParticipant(pd);
    }

    // Call this when UI is confirmed for a participant (provide index if needed)
    public void RegisterUISelections(
        int participantIndex,
        Category cat,
        DuelAction act,
        DuelCommand cmd,
        Secret secret)
    {
        if(participantIndex < 0 || participantIndex >= stagedParticipants.Count)
        {
            Debug.LogError("Invalid participant index");
            return;
        }
        var pd = stagedParticipants[participantIndex];
        pd.category = cat;
        pd.action = act;
        pd.command = cmd;
        pd.secret = secret;
        TryFinalizeParticipant(pd);
    }

    private void TryFinalizeParticipant(DuelParticipantData pd)
    {
        if (pd.IsComplete)
        {
            // Create the real DuelParticipant
            DuelParticipant participant = new DuelParticipant(
                pd.gameObj,
                pd.category.Value,
                pd.action.Value,
                pd.command.Value,
                pd.secret);

            Debug.Log($"Created participant: {participant.Player.name}");
            AddParticipantToDuel(duel, participant);

        }
    }

    private IEnumerator UnlockStatus()
    {
        float duration = 2f;
        yield return new WaitForSeconds(duration);
        // Put the code here that you want to run after 1 second
        Debug.Log("UnlockStatus: Status unlocked after " + duration + " seconds.");
        UIManager.Instance.HideStatus();
        if (BallBehavior.Instance.PossessionPlayer != null) {
            UIManager.Instance.SetStatusPlayer(BallBehavior.Instance.PossessionPlayer);
        }
        UIManager.Instance.UnlockStatus();
    }

}
