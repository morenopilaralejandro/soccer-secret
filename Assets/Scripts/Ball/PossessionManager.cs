using System;
using UnityEngine;

public class PossessionManager : MonoBehaviour
{
    public static PossessionManager Instance { get; private set; }

    public Player PossessionPlayer { get; private set; }
    public Player LastPossessionPlayer { get; private set; }
    public float LastPossessionPlayerKickTime { get; private set; }

    [SerializeField] private float possessionCooldown = 0.2f;

    // Events
    public event Action<Player> OnPossessionGained;
    public event Action<Player> OnPossessionLost;

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject); return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Returns true if this player can't gain possession due to cooldown.
    /// </summary>
    public bool IsCooldownActive(Player player)
    {
        return (LastPossessionPlayer == player && Time.time <= LastPossessionPlayerKickTime + possessionCooldown);
    }

    /// <summary>
    /// Gives player the ball, firing events if changed.
    /// </summary>
    public void GainPossession(Player player)
    {
        if (PossessionPlayer == player)
            return;

        ReleasePossession(); // Release old if any

        PossessionPlayer = player;
        if (PossessionPlayer != null)
            PossessionPlayer.IsPossession = true;

        OnPossessionGained?.Invoke(PossessionPlayer);
    }

    /// <summary>
    /// Releases the current possession, firing event.
    /// </summary>
    public void ReleasePossession()
    {
        if (PossessionPlayer != null)
        {
            LastPossessionPlayer = PossessionPlayer;
            LastPossessionPlayerKickTime = Time.time;

            PossessionPlayer.IsPossession = false;
            OnPossessionLost?.Invoke(PossessionPlayer);
            PossessionPlayer = null;
        }
    }

    public void ResetPossessionState()
    {
        PossessionPlayer = null;
        LastPossessionPlayer = null;
        LastPossessionPlayerKickTime = -Mathf.Infinity;
    }
}
