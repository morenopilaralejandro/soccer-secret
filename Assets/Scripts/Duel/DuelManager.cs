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
            duel.IsResolved = true;
            duel.ResetDuel();
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
        }
    }

    public void AddParticipantToDuel(Duel duel, DuelParticipant participant)
    {
        Debug.Log("AddParticipantToDuel");
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
        if (winningPart.Action == DuelAction.Defense)
        {
            StartCoroutine(duel.LastOff.Player.Stun());
            BallBehavior.Instance.GainPossession(winningPart.Player);
        }
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

    public void ResetDuel() {
        stagedParticipants.Clear();
        duel.ResetDuel();
    }

}
