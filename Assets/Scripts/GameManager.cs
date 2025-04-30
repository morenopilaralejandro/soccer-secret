using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsMovementFrozen { get; private set; } = false;

    public List<GameObject> allyPlayers;
    public List<GameObject> oppPlayers;    
    public Transform[] players; // Array of player transforms
    public Transform ball; // Transform of the ball
    public Transform[] initialPlayerPositions; // Array to store initial player positions
    public Vector3 initialBallPosition; // Vector3 to store initial ball position

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
        // Store initial positions
        StoreInitialPositions();

        // Reset positions at the start of the game
        ResetPositions();
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
        // Show your UI here, e.g.:
        // UIManager.Instance.ShowFreezePanel();
    }

    public void UnfreezeGame()
    {
        IsMovementFrozen = false;
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
            if (duelPlayers[i].GetComponent<Player>().isAi) 
            {
                duelCommand[i] = 1;
                duelSecret[i] = null;
            } else {
                UIManager.Instance.DuelPlayerIndex = i;
                switch (duelType)
                {
                    case 0:
                        if (duelAction[i] == 0) 
                        {
                            //ui dribble

                        } else {
                            //ui block
                        }
                        break;
                    default:
                        Debug.LogWarning("Unknown duelType: " + duelType);
                        break;
                }
            }
        }

        UIManager.Instance.ShowPanelBottom();
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
            if (TypeManager.Instance.IsPlayerEffective(duelPlayers[0], duelPlayers[1])) {
                duelDamage[0] *= 2; 
            } else {
                if (TypeManager.Instance.IsPlayerEffective(duelPlayers[1], duelPlayers[0])) {
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

        for (int i = 0; i < duelPlayers.Length; i++) 
        {
            Debug.Log(duelDamage[i]);
            if (i == duelWinnerIndex) 
            {
                //give ball
            } else {
                StartCoroutine(duelPlayers[i].GetComponent<Player>().Stun());
            }
        }
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
                    /*dribble strength*/
                    damage = player.control + player.body * 0.05f + player.stamina * 0.02f + player.courage;
                    break;
                case "002":
                    /*dribble technique*/
                    damage = player.control + player.body * 0.05f + player.speed * 0.02f + player.courage;
                    break;
                case "011":
                    /*block strength*/
                    damage = player.body + player.guard * 0.05f + player.stamina * 0.02f + player.courage;
                    break;
                case "012":
                    /*block technique*/
                    damage = player.body + player.guard * 0.05f + player.control * 0.02f + player.courage;
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
