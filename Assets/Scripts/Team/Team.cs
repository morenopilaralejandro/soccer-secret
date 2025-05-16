using System.Collections.Generic;
using UnityEngine;

public class Team
{
    public string TeamId => teamId;
    public string TeamNameEn => teamNameEn;
    public string TeamNameJa => teamNameJa;
    public int Lv => lv;
    public Formation Formation => formation;
    public List<PlayerData> PlayerDataList => playerDataList;

    [SerializeField] private string teamId;
    [SerializeField] private string teamNameEn;
    [SerializeField] private string teamNameJa;
    [SerializeField] private int lv;
    [SerializeField] private Formation formation;
    [SerializeField] private List<PlayerData> playerDataList = new List<PlayerData>();

    public void Initialize(TeamData teamData)
    {
        teamId = teamData.teamId;
        teamNameEn = teamData.teamNameEn;
        teamNameJa = teamData.teamNameJa;
        lv = teamData.lv;
        formation = TeamManager.Instance.GetFormationById(teamData.formation);

        playerDataList.Clear();
        foreach (var playerId in teamData.playerIds)
        {
            PlayerData playerData = PlayerManager.Instance.GetPlayerDataById(playerId);
            if (playerData != null)
                playerDataList.Add(playerData);
            else
                Debug.LogWarning($"PlayerData not found for ID: {playerId}");
        }
    }

}
