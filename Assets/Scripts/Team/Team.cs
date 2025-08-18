using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class Team
{
    public string TeamId => teamId;
    public string TeamName => teamName;
    public int Lv => lv;
    public Formation Formation => formation;
    public string WearId => wearId;
    public List<PlayerData> PlayerDataList => playerDataList;
    public List<Player> players = new List<Player>();

    [SerializeField] private string teamId;
    [SerializeField] private string teamName;
    [SerializeField] private int lv;
    [SerializeField] private Formation formation;
    [SerializeField] private string wearId;
    [SerializeField] private List<PlayerData> playerDataList = new List<PlayerData>();
    [SerializeField] private string tableCollectionName = "TeamNames";

    private LocalizedString localizedName;


    void Start()
    {

    }

    public void Initialize(TeamData teamData)
    {
        teamId = teamData.teamId;
        lv = teamData.lv;
        formation = TeamManager.Instance.GetFormationById(teamData.formation);
        wearId = teamData.wearId;

        playerDataList.Clear();
        foreach (var playerId in teamData.playerIds)
        {
            PlayerData playerData = PlayerManager.Instance.GetPlayerDataById(playerId);
            if (playerData != null)
                playerDataList.Add(playerData);
            else
                Debug.LogWarning($"PlayerData not found for ID: {playerId}");
        }

        localizedName = new LocalizedString(tableCollectionName, teamId);
        localizedName.StringChanged += (value) =>
        {
            teamName = value;
        };

        // trigger first update
        teamName = localizedName.GetLocalizedString();
    }

}
