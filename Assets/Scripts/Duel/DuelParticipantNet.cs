using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles serialization and deserialization of DuelParticipant for network RPCs.
/// </summary>
public static class 
DuelParticipantNet
{
    /// <summary>
    /// Convert DuelParticipant to an object[] of primitive types for PUN RPC.
    /// </summary>
    public static object[] Serialize(DuelParticipant dp)
    {
        return new object[]
        {
            dp.Player.PlayerId,
            (int)dp.Category,
            (int)dp.Action,
            (int)dp.Command,
            dp.Secret != null ? dp.Secret.SecretId : "",
            dp.IsDirect,
            dp.Damage,
            dp.Player.TeamIndex
        };
    }

    /// <summary>
    /// Rebuild a DuelParticipant from object[] (as sent over network).
    /// </summary>
    public static DuelParticipant Deserialize(object[] data)
    {
        string playerId          = (string)data[0];
        Category category        = (Category)data[1];
        DuelAction action        = (DuelAction)data[2];
        DuelCommand command      = (DuelCommand)data[3];
        string secretId          = (string)data[4];
        bool isDirect            = (bool)data[5];
        float damage             = data.Length > 6 ? Convert.ToSingle(data[6]) : 0f;
        int teamIndex            = data.Length > 7 ? (int)data[7] : (int)data[7];

        Player player = FindPlayerById(playerId, teamIndex);
        if (player == null)
        {
            Debug.LogError($"DuelParticipantNet: Could not find player (ID:{playerId} TeamIndex:{teamIndex})");
        }
        Secret secret = string.IsNullOrEmpty(secretId) ? null : SecretManager.Instance.GetSecretById(secretId);

        var participant = new DuelParticipant(player.gameObject, category, action, command, secret, isDirect);
        participant.Damage = damage;
        return participant;
    }

    /// <summary>
    /// Looks up a Player object by its string id and side.
    /// </summary>
    public static Player FindPlayerById(string playerId, int teamIndex)
    {
        List<Player> searchList = GameManager.Instance.Teams[teamIndex].players;
        foreach (var p in searchList)
            if (p.PlayerId == playerId)
                return p;
        Debug.LogError($"[DuelParticipantNet] PlayerID not found: {playerId}, teamIndex={teamIndex}");
        return null;
    }
}
