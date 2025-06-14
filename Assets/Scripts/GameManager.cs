using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public enum GamePhase
{
    KickOff,
    Battle,
    Duel,
    Pause,
    Cutscene,
    Overworld
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GamePhase CurrentPhase { get; private set; } = GamePhase.KickOff;
    public GamePhase PreviousPhase { get; private set; } = GamePhase.KickOff;
    public event Action<GamePhase, GamePhase> OnPhaseChanged;

    public bool IsMovementFrozen { get; private set; } = false;
    public bool IsTimeFrozen { get; private set; } = false;
    public bool IsKickOffReady { get; private set; } = false;

    public List<Team> Teams => teams;

    [SerializeField] private List<Team> teams;
    [SerializeField] private List<Player> localHumanPlayers;
    [SerializeField] private List<Player> remoteHumanPlayers;
    [SerializeField] private List<Player> aiPlayers;
    [SerializeField] private Transform ball;
    [SerializeField] private Vector3 initialBallPosition;
    [SerializeField] private Vector3 centerKickOffPosition = Vector3.zero;
    [SerializeField] private float timeDefault = 180f;
    [SerializeField] private float timeRemaining = 180f;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject panelTimeMessage;
    [SerializeField] private GameObject panelGoalMessage;
    [SerializeField] private Animator textGoalMessage;
    [SerializeField] private GameObject textKickOff;
    [SerializeField] private List<GoalTrigger> goals;
    [SerializeField] private List<TextMeshProUGUI> textScores;
    [SerializeField] private int winScore = 3;
    [SerializeField] private int[] scores = new int[] {0, 0};

    private bool isActiveScene = true;

#if PHOTON_UNITY_NETWORKING
    // Helper for safe network mode
    public bool IsMultiplayer => PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined;
#else
    public bool IsMultiplayer => false;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        teams = new List<Team>();
        teams.Add(TeamManager.Instance.GetTeamById("T1"));
        teams.Add(TeamManager.Instance.GetTeamById("T2"));
    }

    void Start()
    {
        StartBattle();
    }

    void Update()
    {
        if (!isActiveScene) return;
        if (!IsTimeFrozen)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
            }
            CheckEndGame();
        }
    }

    private void Reset()
    {
        scores = new int[] {0, 0};
        timeRemaining = timeDefault;
        SetActivePlayers(localHumanPlayers, false);
        SetActivePlayers(remoteHumanPlayers, false);
        SetActivePlayers(aiPlayers, false);
        AssignGoals();
        AssignTeamPlayers();
        AssignControlTypes();
        InitializeTeamPlayers();
        UpdateScoreDisplay();
        UpdateTimerDisplay(timeDefault);
    }

    public void SetIsKickOffReady(bool ready)
    {
        IsKickOffReady = ready;
        if (ready)
            textKickOff.SetActive(false);
    }

    void UpdateTimerDisplay(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, secs);
    }

    private void UpdateScoreDisplay()
    {
        for (int i = 0; i < scores.Length ; i++) 
        {
            textScores[i].text = scores[i].ToString();
        }
    }


    public void StartBattle()
    {
        // (Optionally, network this StartBattle in multiplayer!)
        Reset();
        StartKickOff(teams[0]);
    }

    public void InitializeTeamPlayers()
    {
        for (int i = 0; i < teams.Count; i++)
        {
            Team team = teams[i];
            for (int j = 0; j < team.players.Count; j++)
            {
                Player player = team.players[j];
                PlayerData playerData = team.PlayerDataList[j];
                player.Initialize(playerData);
                if (j == 0)
                {
                    player.IsKeeper = true;
                }
                player.Lv = team.Lv;
                player.TeamIndex = i;
                player.SetWear(team);
            }
        }                


    }

    public void ResetDefaultPositions()
    {
        ResetPlayerPositions();
        ball.transform.position = initialBallPosition;
    }

    public void ResetPlayerPositions()
    {
        foreach (var t in teams) 
        {
            for (int i = 0; i < t.players.Count; i++)
            {
                Player player = t.players[i];
                player.Unstun();
                player.transform.position = t.Formation.Coords[i];
                if (player.ControlType != ControlType.LocalHuman)
                {
                    Vector3 pos = player.transform.position;
                    pos.z = pos.z * -1;
                    player.transform.position = pos;
                }
                if (player.ControlType != ControlType.Ai)
                {
                    player.transform.Find("Line").GetComponent<PlayerLineRenderer>().ResetLine();
                }
                player.DefaultPosition = player.transform.position;
            }
        }
    }

    // ==== Network-Safe version! ====
    public void StartKickOff(Team kickOffTeam)
    {
        CheckEndGame();
        AudioManager.Instance.PlayBgm("BgmBattle");
        FreezeGame();
        textKickOff.SetActive(true);
        SetGamePhaseNetworkSafe(GamePhase.KickOff);
        IsKickOffReady = false;
        ResetDefaultPositions();

        List<Player> kickOffPlayers = kickOffTeam.players;
        if (kickOffPlayers.Count > 0)
        {
            Player kickOffPlayer = kickOffPlayers[kickOffTeam.Formation.KickOff];
            kickOffPlayer.transform.position = centerKickOffPosition;
            PossessionManager.Instance.GainPossession(kickOffPlayer);
        }
    }

    // Network aware, so phase is always in sync
    public void SetGamePhaseNetworkSafe(GamePhase newPhase)
    {
        if (IsMultiplayer)
        {
#if PHOTON_UNITY_NETWORKING
            // Only master sets phase in online games!
            if (PhotonNetwork.IsMasterClient)
            {
                this.photonView.RPC(nameof(RPC_SetGamePhase), RpcTarget.All, (int)newPhase);
            }
#endif
        }
        else
        {
            SetGamePhase(newPhase);
        }
    }

#if PHOTON_UNITY_NETWORKING
    private PhotonView photonView => PhotonView.Get(this);

    [PunRPC]
    void RPC_SetGamePhase(int phase) // for network sync
    {
        SetGamePhase((GamePhase)phase);
    }
#endif

    public void SetGamePhase(GamePhase newPhase)
    {
        if (CurrentPhase != newPhase)
        {
            Debug.Log("GamePhase: " + newPhase);
            PreviousPhase = CurrentPhase;
            CurrentPhase = newPhase;
            OnPhaseChanged?.Invoke(CurrentPhase, PreviousPhase);
        }
    }

    // You may want to make Freeze/Unfreeze also network-aware (optional):
    public void FreezeGame()
    {
        IsMovementFrozen = true;
        IsTimeFrozen = true;
    }

    public void UnfreezeGame()
    {
        IsMovementFrozen = false;
        IsTimeFrozen = false;
    }

    public void OnGoalScored(Team scoredTeam)
    {
        // (Optional: sync via network if needed)
        for (int i = 0; i < teams.Count; i++) {
            Team team = teams[i];
            if (team != scoredTeam) 
            {
                scores[i]++;
            }
        }

        UpdateScoreDisplay();

        StartCoroutine(GoalSequence(scoredTeam));
    }

    private IEnumerator GoalSequence(Team kickOffTeam)
    {
        float duration = 2f;
        IsTimeFrozen = true;
        AudioManager.Instance.PlayBgm("BgmOle");
        panelGoalMessage.SetActive(true);
        textGoalMessage.Play("TextGoalSlide", -1, 0f);

        yield return new WaitForSeconds(duration);
        panelGoalMessage.SetActive(false);
        IsTimeFrozen = false;
        StartKickOff(kickOffTeam);
    }

    private IEnumerator TimeSequence()
    {
        float duration = 2f;
        IsTimeFrozen = true;
        timeRemaining = 0;
        AudioManager.Instance.PlayBgm("BgmTimeUp");
        UpdateTimerDisplay(timeRemaining);
        panelTimeMessage.SetActive(true);

        yield return new WaitForSeconds(duration);
        panelTimeMessage.SetActive(false);
        SceneManager.LoadScene("GameOver");
    }

    public Player GetOppKeeper(Player player)
    {
        int oppTeamIdx = 1 - player.TeamIndex;
        return teams[oppTeamIdx].players[0];
    }

    public float GetDistanceToAllyGoal(Player player)
    {
        Transform goal = GetAllyGoal(player).transform;
        return Vector3.Distance(player.transform.position, goal.position);
    }

    public float GetDistanceToOppGoal(Player player)
    {
        Transform goal = GetOppGoal(player).transform;
        return Mathf.Abs(player.transform.position.z - goal.position.z);
    }

    public GoalTrigger GetAllyGoal(Player player)
    {
        return goals[player.TeamIndex];  
    }

    public GoalTrigger GetOppGoal(Player player)
    {
        int oppIndex = 1 - player.TeamIndex;
        return goals[oppIndex];
    }

    private void CheckEndGame()
    {
        // If either team reaches winScore
        if (scores[0] >= winScore)
        {
            SceneManager.LoadScene("BattleResult");
        }
        else if (scores[1] >= winScore)
        {
            SceneManager.LoadScene("GameOver");
        }
        else if (timeRemaining <= 0)
        {
            StartCoroutine(TimeSequence());
        }
    }

    public int GetLocalTeamIndex()
    {
    #if PHOTON_UNITY_NETWORKING
        Photon.Realtime.Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
            if (players[i] == PhotonNetwork.LocalPlayer)
                return i; // Local player's index in PlayerList = team index
        return 0;
    #else
        return 0;
    #endif
    }

    private void AssignGoals() 
    {
        for (int i = 0; i < teams.Count; i++)
        {
            goals[i].Team = teams[i];
        }
    }

    private void AssignTeamPlayers()
    {
        int localTeamIndex = GetLocalTeamIndex();
        bool isMultiplayer = IsMultiplayer;

        if (!isMultiplayer)
        {
            // Singleplayer: Local human controls team[0], AI controls team[1]
            teams[0].players = localHumanPlayers;
            teams[1].players = aiPlayers;
            SetActivePlayers(localHumanPlayers, true);
            SetActivePlayers(aiPlayers, true);
        }
        else
        {
            // Multiplayer: Local controls team[localTeamIndex], remote controls the other
            if (localTeamIndex == 0)
            {
                teams[0].players = localHumanPlayers;
                teams[1].players = remoteHumanPlayers;
            }
            else // localTeamIndex == 1
            {
                teams[0].players = remoteHumanPlayers;
                teams[1].players = localHumanPlayers;
            }
            SetActivePlayers(localHumanPlayers, true);
            SetActivePlayers(remoteHumanPlayers, true);
        }
    }

    private void AssignControlTypes()
    {
        int myTeamIndex = GetLocalTeamIndex();

        for (int i = 0; i < teams.Count; i++)
        {
            ControlType ct = (i == myTeamIndex)
                ? ControlType.LocalHuman
                : (IsMultiplayer ? ControlType.RemoteHuman : ControlType.Ai);
            foreach (var p in teams[i].players)
                p.ControlType = ct;
        }
    }

    public void SetActivePlayers(List<Player> players, bool isActive)
    {
        foreach (Player player in players)
            player.gameObject.SetActive(isActive);
    }
}
