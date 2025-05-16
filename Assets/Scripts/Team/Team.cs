using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Team : MonoBehaviour
{
    public string TeamId => teamId;
    public string TeamNameEn => teamNameEn;
    public string TeamNameJa => teamNameJa;
    public int Lv => lv;
    public Formation Formation => formation;
    public List<PlayerData> PlayerDatas => playerDatas;

    [SerializeField] private string teamId;
    [SerializeField] private string teamNameEn;
    [SerializeField] private string teamNameJa;
    [SerializeField] private int lv;
    [SerializeField] private Formation formation;
    [SerializeField] private List<PlayerData> playerDatas;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(TeamData teamData)
    {
        teamId = teamData.teamId;
        teamNameEn = teamData.teamNameEn;
        teamNameJa = teamData.teamNameJa;

        formation = TeamManager.Instance.GetFormationById(teamData.formation);
        //create a list of player object in the inspector and call Initialize
        for (int i = 0; i < teamData.playerIds.Length; i++) 
        {
            PlayerData playerData  = PlayerManager.Instance.GetPlayerDataById(teamData.playerIds[i]);
            playerDatas.Add(playerData); 
        }
    }
}
