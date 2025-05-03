using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    public string playerId;
    public int lv;
    public int[] moreStats = new int[9];
    public int currFreedom;
    public List<string> currentSecretIds = new List<string>();
    public List<string> learnedSecretIds = new List<string>();
}
