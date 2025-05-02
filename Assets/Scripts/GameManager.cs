using UnityEngine;
using System.Collections.Generic;
using TMPro; // Needed for TextMeshProUGUI

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsMovementFrozen { get; private set; } = false;
    public bool IsTimeFrozen { get; private set; } = false;

    [SerializeField] private List<GameObject> allyPlayers;
    [SerializeField] private List<GameObject> oppPlayers;    
    [SerializeField] private Transform[] players; // Array of player transforms
    [SerializeField] private Transform ball; // Transform of the ball
    [SerializeField] private Transform[] initialPlayerPositions; // Array to store initial player positions
    [SerializeField] private Vector3 initialBallPosition; // Vector3 to store initial ball position
    [SerializeField] private float timeRemaining = 180f; // 3 minutes
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private PlayerCardUI playerCard0;
    [SerializeField] private PlayerCardUI playerCard1;
    [SerializeField] private BarUI barHp0;
    [SerializeField] private BarUI barHp1;
    [SerializeField] private BarUI barSp0;
    [SerializeField] private BarUI barSp1;
    [SerializeField] private GameObject imagePossesion0;
    [SerializeField] private GameObject imagePossesion1;

    private GameObject[] duelPlayers = new GameObject[2];
    private int duelType;
    private int[] duelAction = new int[2];
    private int[] duelCommand = new int[2];
    private string[] duelSecret = new string[2];
    private float[] duelDamage = new float[2];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        UpdateTimerDisplay(timeRemaining);

        // Store initial positions
        StoreInitialPositions();

        // Reset positions at the start of the game
        ResetPositions();
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

    void StoreInitialPositions()
    {
        initialPlayerPositions = new Transform[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            initialPlayerPositions[i] = players[i];
        }
        initialBallPosition = ball.position;
    }

    public void ResetPositions()
    {
        // Reset player positions
        for (int i = 0; i < players.Length; i++)
        {
            players[i].position = initialPlayerPositions[i].position;
        }

        // Reset ball position
        ball.position = initialBallPosition;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero; // Stop the ball
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
        // Hide your UI here, e.g.:
        // UIManager.Instance.HideFreezePanel();
    }

    public void HandleDuel(GameObject player0, GameObject player1, int duelTypeParam)
    {
        /* 0 field 1 shoot*/
        GameManager.Instance.FreezeGame();
        duelType = duelTypeParam;
        duelPlayers[0] = player0;
        duelPlayers[1] = player1;
        duelAction[0] = 0;
        duelAction[1] = 1;
        

        for (int i = 0; i < duelPlayers.Length; i++) {
            Player auxPlayer = duelPlayers[i].GetComponent<Player>();

            if (auxPlayer.IsAi) 
            {
                duelCommand[i] = 1;
                duelSecret[i] = null;
            } else {
                UIManager.Instance.DuelPlayerIndex = i;
                if (duelAction[i] == 0) 
                {
                    imagePossesion0.SetActive(true);
                    imagePossesion1.SetActive(false);
                } else {
                    imagePossesion0.SetActive(false);
                    imagePossesion1.SetActive(true);
                }
                switch (duelType)
                {
                    case 0:
                        if (duelAction[i] == 0) 
                        {
                            //ui dribble only select dribble secret
                        } else {
                            //ui block
                        }
                        break;
                    default:
                        Debug.LogWarning("Unknown duelType: " + duelType);
                        break;
                }
            }

            if(auxPlayer.IsAlly) {
                playerCard0.SetPlayer(auxPlayer);
                barHp0.SetPlayer(auxPlayer);
                barSp0.SetPlayer(auxPlayer);
            } else {
                playerCard1.SetPlayer(auxPlayer);
                barHp1.SetPlayer(auxPlayer);
                barSp1.SetPlayer(auxPlayer);
            }
        }

        UIManager.Instance.SetDuelUiMainVisible(true);
    }

    public void ExecuteDuel(int duelPlayerIndex, int command, string secret)
    {
        duelCommand[duelPlayerIndex] = command;
        duelSecret[duelPlayerIndex] = secret;
        int duelWinnerIndex = 1;        
        bool isSecret = false;

        for (int i = 0; i < duelPlayers.Length; i++) 
        {
            duelDamage[i] = DamageCalc(duelPlayers[i], duelType, duelAction[i], duelCommand[i], duelSecret[i]);
            if (duelCommand[i] == 0) 
            {
                isSecret = true;
            }
        }

        //check effectiveness
        if (isSecret) 
        {
            
        } else {
            if (ElementManager.Instance.IsPlayerEffective(duelPlayers[0], duelPlayers[1])) {
                duelDamage[0] *= 2; 
            } else {
                if (ElementManager.Instance.IsPlayerEffective(duelPlayers[1], duelPlayers[0])) {
                    duelDamage[1] *= 2; 
                }
            } 
        }

        if (duelDamage[0] > duelDamage[1]) 
        {
            duelWinnerIndex = 0;
        } else {
            duelWinnerIndex = 1;
        }
        Debug.Log("Winner: " + duelWinnerIndex + " (" + duelDamage[0] + ", " + duelDamage[1] + ")");
        for (int i = 0; i < duelPlayers.Length; i++) 
        {
            if (i == duelWinnerIndex) 
            {
                //give ball
            } else {
                StartCoroutine(duelPlayers[i].GetComponent<Player>().Stun());
            }
        }

        UIManager.Instance.SetDuelUiMainVisible(false);
        UnfreezeGame();
    }

    public float DamageCalc(GameObject playerObject, int duelType, int action, int command, string secret)
    {
        /*
            duelType: 
                0 field
                1 goal
            action:
                0 off
                1 def
            command:
                0 secret
                1 strength
                2 technique
        */
        string formulaId = "" + duelType + action + command;
        Debug.Log("formulaId: " + formulaId);


        Player player = playerObject.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("No Player script found on this GameObject!");
            return 0f;
        }

        float damage = 0f;
        if (secret == null) {
            switch (formulaId)
            {
                case "001":
                    /*dribble Phys*/
                    damage = player.GetStat(PlayerStats.Control) + player.GetStat(PlayerStats.Body) * 0.05f + player.GetStat(PlayerStats.Stamina) * 0.02f + player.GetStat(PlayerStats.Courage);
                    break;
                case "002":
                    /*dribble Skill*/
                    damage = player.GetStat(PlayerStats.Control) + player.GetStat(PlayerStats.Body) * 0.05f + player.GetStat(PlayerStats.Speed) * 0.02f + player.GetStat(PlayerStats.Courage);
                    break;
                case "011":
                    /*block Phys*/
                    damage = player.GetStat(PlayerStats.Body) + player.GetStat(PlayerStats.Guard) * 0.05f + player.GetStat(PlayerStats.Stamina) * 0.02f + player.GetStat(PlayerStats.Courage);
                    break;
                case "012":
                    /*block Skill*/
                    damage = player.GetStat(PlayerStats.Body) + player.GetStat(PlayerStats.Guard) * 0.05f + player.GetStat(PlayerStats.Control) * 0.02f + player.GetStat(PlayerStats.Courage);
                    break;
                default:
                    Debug.LogWarning("Unknown formulaId: " + formulaId);
                    break;
            }
        } else
        {
            //calculate secret power and check stab
        }
        return damage;
    }

    // Call this method when a goal is scored
    public void OnGoalScored()
    {
        // Reset positions after a goal
        ResetPositions();

        // Additional logic for handling a goal (e.g., updating score) can be added here
    }
}
