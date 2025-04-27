using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameFrozen { get; private set; } = false;
    public GameObject OffPlayer { get; set; }
    public GameObject DefPlayer { get; set; }

    public List<GameObject> allyPlayers;
    public List<GameObject> oppPlayers;    
    public Transform[] players; // Array of player transforms
    public Transform ball; // Transform of the ball
    public Transform[] initialPlayerPositions; // Array to store initial player positions
    public Vector3 initialBallPosition; // Vector3 to store initial ball position

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
        IsGameFrozen = true;
        // Show your UI here, e.g.:
        // UIManager.Instance.ShowFreezePanel();
    }

    public void UnfreezeGame()
    {
        IsGameFrozen = false;
        // Hide your UI here, e.g.:
        // UIManager.Instance.HideFreezePanel();
    }

    public void HandleDuel(GameObject offPlayer, GameObject defPlayer, int duelType)
    {
        /* 0 field 1 shoot*/
        OffPlayer = offPlayer;
        DefPlayer = defPlayer;
        bool offPlayerIsAlly = OffPlayer.GetComponent<Player>().isAlly;

        switch (duelType)
        {
            case 0:
                if (offPlayerIsAlly) {
                    //ui dribble
                } else {
                    //uiblock
                }
                break;
            default:
                Debug.LogWarning("Unknown duelType: " + duelType);
                break;
        }

    }

    public float DamageCalc(GameObject playerObject, string command)
    {
        Player player = playerObject.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("No Player script found on this GameObject!");
            return 0f;
        }

        float damage = 0f;
        switch (command)
        {
            case "blockStrength":
                damage = player.body + player.guard * 0.05f + player.stamina * 0.02f + player.courage;
                break;
            case "blockTechnique":
                damage = player.body + player.guard * 0.05f + player.control * 0.02f + player.courage;
                break;
            case "dribbleStrength":
                damage = player.control + player.body * 0.05f + player.stamina * 0.02f + player.courage;
                break;
            case "dribbleTechnique":
                damage = player.control + player.body * 0.05f + player.speed * 0.02f + player.courage;
                break;
            default:
                Debug.LogWarning("Unknown command: " + command);
                break;
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
