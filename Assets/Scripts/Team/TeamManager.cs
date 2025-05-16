using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    [SerializeField] private string pathScriptableObjects = "ScriptableObjects/";
    [SerializeField] private string pathTeam = "Team";
    [SerializeField] private string pathFormation = "Formation";
    [SerializeField] private string pathCoord = "Coord";

    // Dictionaries for Data
    private Dictionary<string, TeamData> teamDataDict = new Dictionary<string, TeamData>();
    private Dictionary<string, FormationData> formationDataDict = new Dictionary<string, FormationData>();
    private Dictionary<string, CoordData> coordDataDict = new Dictionary<string, CoordData>();

    // Dictionaries for runtime objects
    private Dictionary<string, Team> teamDict = new Dictionary<string, Team>();
    private Dictionary<string, Formation> formationDict = new Dictionary<string, Formation>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load and cache all CoordData
        CoordData[] allCoordData = Resources.LoadAll<CoordData>(pathScriptableObjects + pathCoord);
        foreach (CoordData cd in allCoordData)
        {
            AddCoordDataToDict(cd);
        }

        // Load and cache all FormationData
        FormationData[] allFormationData = Resources.LoadAll<FormationData>(pathScriptableObjects + pathFormation);
        foreach (FormationData fd in allFormationData)
        {
            AddFormationDataToDict(fd);
            Formation formation = new Formation();
            formation.Initialize(fd);
            AddFormationToDict(formation);
        }

        // Load and cache all TeamData
        TeamData[] allTeamData = Resources.LoadAll<TeamData>(pathScriptableObjects + pathTeam);
        foreach (TeamData td in allTeamData)
        {
            AddTeamDataToDict(td);
            // Optionally: create Team from TeamData
            Team team = new Team();
            team.Initialize(td);
            AddTeamToDict(team);
        }
    }

    // === Team ===
    public void AddTeamDataToDict(TeamData teamData)
    {
        if (!teamDataDict.ContainsKey(teamData.teamId))
            teamDataDict.Add(teamData.teamId, teamData);
        else
            Debug.LogWarning("Duplicate Team id: " + teamData.teamId);
    }
    public void AddTeamToDict(Team team)
    {
        if (!teamDict.ContainsKey(team.TeamId))
            teamDict.Add(team.TeamId, team);
        else
            Debug.LogWarning("Duplicate Team id: " + team.TeamId);
    }
    public TeamData GetTeamDataById(string id)
    {
        if (teamDataDict.TryGetValue(id, out var teamData))
            return teamData;
        Debug.LogWarning("TeamData not found: " + id);
        return null;
    }
    public Team GetTeamById(string id)
    {
        if (teamDict.TryGetValue(id, out var team))
            return team;
        Debug.LogWarning("Team not found: " + id);
        return null;
    }

    // === Formation ===
    public void AddFormationDataToDict(FormationData formationData)
    {
        if (!formationDataDict.ContainsKey(formationData.formationId))
            formationDataDict.Add(formationData.formationId, formationData);
        else
            Debug.LogWarning("Duplicate Formation id: " + formationData.formationId);
    }
    public void AddFormationToDict(Formation formation)
    {
        if (!formationDict.ContainsKey(formation.FormationId))
            formationDict.Add(formation.FormationId, formation);
        else
            Debug.LogWarning("Duplicate Formation id: " + formation.FormationId);
    }
    public FormationData GetFormationDataById(string id)
    {
        if (formationDataDict.TryGetValue(id, out var formationData))
            return formationData;
        Debug.LogWarning("FormationData not found: " + id);
        return null;
    }
    public Formation GetFormationById(string id)
    {
        if (formationDict.TryGetValue(id, out var formation))
            return formation;
        Debug.LogWarning("Formation not found: " + id);
        return null;
    }

    // === Coord ===
    public void AddCoordDataToDict(CoordData coordData)
    {
        if (!coordDataDict.ContainsKey(coordData.coordId))
            coordDataDict.Add(coordData.coordId, coordData);
        else
            Debug.LogWarning("Duplicate Coord id: " + coordData.coordId);
    }
    public CoordData GetCoordDataById(string id)
    {
        if (coordDataDict.TryGetValue(id, out var coordData))
            return coordData;
        Debug.LogWarning("CoordData not found: " + id);
        return null;
    }

}
