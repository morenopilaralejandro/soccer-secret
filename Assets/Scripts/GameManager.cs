using UnityEngine;
using System.Collections.Generic;
using TMPro; // Needed for TextMeshProUGUI

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsMovementFrozen { get; private set; } = false;
    public bool IsTimeFrozen { get; private set; } = false;

    [SerializeField] private Transform[] players; // Array of player transforms
    [SerializeField] private Transform ball; // Transform of the ball
    [SerializeField] private Transform goalTop;
    [SerializeField] private Transform goalBottom;
    [SerializeField] private Transform[] initialPlayerPositions; // Array to store initial player positions
    [SerializeField] private Vector3 initialBallPosition; // Vector3 to store initial ball position
    [SerializeField] private float timeRemaining = 180f; // 3 minutes
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private PlayerCard playerCard0;
    [SerializeField] private PlayerCard playerCard1;
    [SerializeField] private Bar barHp0;
    [SerializeField] private Bar barHp1;
    [SerializeField] private Bar barSp0;
    [SerializeField] private Bar barSp1;
    [SerializeField] private GameObject imagePossesion0;
    [SerializeField] private GameObject imagePossesion1;

    private GameObject[] duelPlayers = new GameObject[2];
    private int duelType;
    private int[] duelAction = new int[2];
    private int[] duelCommand = new int[2];
    private Secret[] duelSecret = new Secret[2];
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

    // Call this method when a goal is scored
    public void OnGoalScored()
    {
        //TODO
        // Reset positions after a goal
        ResetPositions();

        // Additional logic for handling a goal (e.g., updating score) can be added here
    }

    public float GetDistanceFromPlayerToGoal(Player player) 
    {
        Transform goal = player.IsAlly ? goalTop : goalBottom;
        return Vector3.Distance(player.transform.position, goal.position);
    }
}
