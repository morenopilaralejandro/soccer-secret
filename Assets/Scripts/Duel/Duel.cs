using System.Collections.Generic;
using UnityEngine;

public class Duel
{
    public DuelMode Mode { get; set; }
    public List<DuelParticipant> Participants { get; } = new List<DuelParticipant>();
    public float AttackPressure { get; set; }
    public DuelParticipant LastOffense { get; set; }
    public DuelParticipant LastDefense { get; set; }
    public bool IsResolved { get; set; }

    public void Reset()
    {
        Participants.Clear();
        AttackPressure = 0f;
        LastOffense = null;
        LastDefense = null;
        IsResolved = false;
    }
}
