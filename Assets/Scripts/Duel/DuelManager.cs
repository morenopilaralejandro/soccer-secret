using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DuelMode { Field, Shoot }
public enum DuelAction { Offense, Defense }
public enum DuelCommand { Secret, Phys, Skill }

public class DuelManager : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ExecuteDuel(Duel duel)
    {
        float attackPressure = 0f;
        DuelParticipant lastOff = null;
        DuelParticipant lastDef = null;

        foreach (var part in duel.Parts)
        {
            if (part.Action == DuelAction.Offense)
            {
                attackPressure += part.Damage;
                lastOff = part;
            }
            else // Defense
            {
                lastDef = part;

                if (lastOff != null && ElementManager.Instance.IsEffective(part.CurrentElement, lastOff.CurrentElement))
                {
                    part.Damage *= 2f;
                    Debug.Log("Defense element is effective against last offense! Damage doubled.");
                } else {
                    if (lastOff != null && ElementManager.Instance.IsEffective(lastOff.CurrentElement, part.CurrentElement))
                    {
                        attackPressure -= lastOff.Damage;
                        lastOff.Damage *= 2;
                        attackPressure += lastOff.Damage;
                        Debug.Log("Last offense element is effective against defense! Damage doubled.");
                    }
                }

                if (part.Damage >= attackPressure)
                {
                    Debug.Log($"{part.Player.name} ({part.Command}) stopped the attack!");
                    Debug.Log($"Last Offense: {lastOff.Player.name} | Last Defense: {lastDef.Player.name}");
                    // PASS THESE PARTICIPANTS TO YOUR GAME LOGIC
                    OnDuelEnd(winningPart: part, lastOff, lastDef, winner: "defense");
                    return;
                } else {
                    attackPressure -= part.Damage;
                    //stun def
                }
            }
        }

        // If no defense wins, offense succeeds
        if (lastOff != null)
        {
            Debug.Log($"{lastOff.Player.name} wins the duel. Last defense was {lastDef?.Player.name}");
            OnDuelEnd(winningPart: lastOff, lastOff, lastDef, winner: "offense");
        }
    }

    private void OnDuelEnd(DuelParticipant winningPart, DuelParticipant lastOff, DuelParticipant lastDef, string winner)
    {
        if (winningPart.Action == DuelAction.Defense)
        {
            //stun last off
            //grant possesion winningPart
        }
    }
}
