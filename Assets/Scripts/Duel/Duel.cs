using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Duel
{
    public DuelMode Mode;
    public List<DuelParticipant> Parts = new List<DuelParticipant>();

    public float AttackPressure = 0f;
    public DuelParticipant LastOff = null;
    public DuelParticipant LastDef = null;

    public bool IsResolved = false;

    public void ResetDuel()
    {
        Parts.Clear();
        AttackPressure = 0;
        LastOff = null;
        LastDef = null;
        IsResolved = false;
    }
}
