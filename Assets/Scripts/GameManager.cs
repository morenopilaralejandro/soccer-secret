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

    public Transform FieldRoot => fieldRoot;

    [SerializeField] private List<Team> teams;
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

    [SerializeField] private Transform fieldRoot;
    [SerializeField] private GameObject prefabPlayer;
    [SerializeField] private GameObject prefabPlayerOpp;

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
    #if PHOTON_UNITY_NETWORKING
        if (IsMultiplayer)
            MultiplayerStart();
        else
            OfflineStart();
    #else
        OfflineStart();
    #endif

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
        AssignGoals();
        InitializeTeamPlayers();
        UpdateScoreDisplay();
        UpdateTimerDisplay(timeDefault);
        FlipFieldIfNeeded(GetLocalTeamIndex());
    }

    void FlipFieldIfNeeded(int myTeamIndex)
    {
        if (myTeamIndex == 1)
            fieldRoot.localScale = new Vector3(1, 1, -1);
        else
            fieldRoot.localScale = new Vector3(1, 1, 1);
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

    private void MultiplayerStart() {
        SpawnMyPlayers_Multiplayer();
        StartBattle();
    }

    private void OfflineStart()
    {
        SpawnMyPlayers_Singleplayer();
        SpawnAiPlayers_Singleplayer();
        StartBattle();
    }

    private void SpawnMyPlayers_Multiplayer()
    {
    #if PHOTON_UNITY_NETWORKING
        int myTeamIndex = GetLocalTeamIndex();
        Team myTeam = teams[myTeamIndex];

        for (int i = 0; i < myTeam.PlayerDataList.Count; i++)
        {
            Vector3 spawnPos = myTeam.Formation.Coords[i];
            // Only this client spawns its own team's players
            Player player = InstantiatePlayer_Multiplayer(spawnPos, Quaternion.identity, myTeamIndex, ControlType.LocalHuman);
            myTeam.players.Add(player);
        }
    #endif
    }

    void SpawnMyPlayers_Singleplayer()
    {
            int teamIndex = 0;
            Team team = teams[teamIndex];
            for (int i = 0; i < team.PlayerDataList.Count; i++)
            {
                Vector3 spawnPos = team.Formation.Coords[i];
                Player player = InstantiatePlayer_Singleplayer(spawnPos, Quaternion.identity, teamIndex, ControlType.LocalHuman, prefabPlayer);
                team.players.Add(player);
            }   
    }

    void SpawnAiPlayers_Singleplayer()
    {
            int teamIndex = 1;
            Team team = teams[teamIndex];
            for (int i = 0; i < team.PlayerDataList.Count; i++)
            {
                Vector3 spawnPos = team.Formation.Coords[i];
                Player player = InstantiatePlayer_Singleplayer(spawnPos, Quaternion.identity, teamIndex, ControlType.Ai, prefabPlayerOpp);
                team.players.Add(player);
            }   
    }

    private Player InstantiatePlayer_Singleplayer(Vector3 pos, Quaternion rot, int teamIndex, ControlType controlType, GameObject prefab)
    {
        GameObject go = Instantiate(prefab, pos, rot, fieldRoot);
        Player playerComponent = go.GetComponent<Player>();
        playerComponent.TeamIndex = teamIndex;
        playerComponent.ControlType = controlType;
        go.transform.Rotate(90f, 0f, 0f);
        return playerComponent;
    }

    private Player InstantiatePlayer_Multiplayer(Vector3 pos, Quaternion rot, int teamIndex, ControlType controlType) {
        // Pass teamIndex and/or controlType as instantiation data if needed:
        object[] instantiationData = new object[] { teamIndex };
        GameObject go = PhotonNetwork.Instantiate("Prefabs/Player/Player", pos, rot, 0, instantiationData);

        // Return the Player component (could be null on remote clients, but is valid on the spawning client)
        return go.GetComponent<Player>();
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
                    player.UpdateKeeperColliderState();
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
                if (player.TeamIndex != 0)
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
}
