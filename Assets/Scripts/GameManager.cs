using UnityEngine;
using System.Collections.Generic;
using TMPro; // Needed for TextMeshProUGUI

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsMovementFrozen { get; private set; } = false;
    public bool IsTimeFrozen { get; private set; } = false;
    public bool IsKickOff { get; private set; } = false;

    [SerializeField] private Team team0;
    [SerializeField] private Team team1;
    [SerializeField] private List<Player> allyPlayers;
    [SerializeField] private List<Player> oppPlayers;
    [SerializeField] private Transform ball; // Transform of the ball
    [SerializeField] private Transform goalTop;
    [SerializeField] private Transform goalBottom;
    [SerializeField] private Vector3 initialBallPosition; // Vector3 to store initial ball position
    [SerializeField] private Vector3 centerKickOffPosition = Vector3.zero;
    [SerializeField] private float timeRemaining = 180f; // 3 minutes
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI textScore0;
    [SerializeField] private TextMeshProUGUI textScore1;
    private int score0 = 0;
    private int score1 = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        team0 =  TeamManager.Instance.GetTeamById("T1");
        team1 = TeamManager.Instance.GetTeamById("T2");

        goalBottom.GetComponent<GoalTrigger>().Team = team0;
        goalTop.GetComponent<GoalTrigger>().Team = team1;

        InitializeTeamPlayers(team0, allyPlayers, true);
        InitializeTeamPlayers(team1, oppPlayers, false);
    }

    void Start()
    {
        UpdateScoreDisplay();
        UpdateTimerDisplay(timeRemaining);
        StartBattle(team0, team1);
    }


    void Update()
    {
        if (!IsTimeFrozen)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
            }
            else
            {
                // Time is up
                IsTimeFrozen = true;
                timeRemaining = 0;
                UpdateTimerDisplay(timeRemaining);
                // Optionally: Trigger end-of-timer event here
            }
        }
    }

    void UpdateTimerDisplay(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, secs);
    }

    private void UpdateScoreDisplay()
    {
        textScore0.text = score0.ToString();
        textScore1.text = score1.ToString();
    }

    public void StartBattle(Team teamA, Team teamB)
    {
        StartKickOff(teamA);
        timeRemaining = 180f;
    }

    public void InitializeTeamPlayers(Team team, List<Player> players, bool isAlly)
    {
        for (int i = 0; i < players.Count; i++) {
            Player player = players[i];
            PlayerData playerData = team.PlayerDataList[i];
            player.Initialize(playerData);
            if (i == 0) 
            {
                player.IsKeeper = true;
            }  
            player.Lv = team.Lv;
            player.IsAlly = isAlly;
            player.IsAi = !isAlly; 
            player.SetWear(team);    
        }
    }

    public void ResetDefaultPositions() 
    {
        ResetPlayerPositions(team0, allyPlayers, true);
        ResetPlayerPositions(team1, oppPlayers, false);
        ball.transform.position = initialBallPosition;
    }

    public void ResetPlayerPositions(Team team, List<Player> players, bool isAlly)
    {
        for (int i = 0; i < players.Count; i++) 
        {
            Player player = players[i];
            player.transform.position = team.Formation.Coords[i];
            if (!isAlly) 
            {
                Vector3 pos = player.transform.position;
                pos.z = pos.z * -1;
                player.transform.position = pos;
            }
        }
    }

    public void StartKickOff(Team kickOffTeam)
    {
        FreezeGame();   
        IsKickOff = true;     
        ResetDefaultPositions();

        // Optionally: Move one of the kickOffTeam's players (usually a striker or midfielder) to be on the center spot
        // Let's assume player 1 is the kick-off taker:
        List<Player> kickOffPlayers = (kickOffTeam == team0) ? allyPlayers : oppPlayers;
        if (kickOffPlayers.Count > 0)
        {
            Player kickOffPlayer = kickOffPlayers[kickOffTeam.Formation.KickOff];
            kickOffPlayer.transform.position = centerKickOffPosition;
            BallBehavior.Instance.GainPossession(kickOffPlayer);
        }

    }

    public void FreezeGame()
    {
        IsMovementFrozen = true;
        IsTimeFrozen = true;
        // Show your UI here, e.g.:
        // UIManager.Instance.ShowFreezePanel();
    }

    public void UnfreezeGame()
    {
        IsMovementFrozen = false;
        IsTimeFrozen = false;
        IsKickOff = false;
        // Hide your UI here, e.g.:
        // UIManager.Instance.HideFreezePanel();
    }

    public void OnGoalScored(Team scoringTeam)
    {
        if (scoringTeam == team0)
            score0++;
        else if (scoringTeam == team1)
            score1++;

        UpdateScoreDisplay();

        Team kickOffTeam = (scoringTeam == team0) ? team1 : team0;
        StartKickOff(kickOffTeam);
    }

    
    public float GetDistanceToOppGoal(Player player) 
    {
        Transform goal = player.IsAlly ? goalTop : goalBottom;
        return Vector3.Distance(player.transform.position, goal.position);
    }

    public float GetDistanceToAllyGoal(Player player) 
    {
        Transform goal = player.IsAlly ? goalBottom : goalTop;
        return Vector3.Distance(player.transform.position, goal.position);
    }
}
