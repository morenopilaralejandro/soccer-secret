using System;
using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class PossessionManager : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , IPunObservable
#endif
{
    public static PossessionManager Instance { get; private set; }

    public Player PossessionPlayer { get; private set; }
    public Player LastPossessionPlayer { get; private set; }
    public float LastPossessionPlayerKickTime { get; private set; }

    [SerializeField] private float possessionCooldown = 0.2f;

    // Events
    public event Action<Player> OnPossessionGained;
    public event Action<Player> OnPossessionLost;

#if PHOTON_UNITY_NETWORKING
    private PhotonView photonView => PhotonView.Get(this);
#endif

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
    /// Gives player the ball, firing events if changed. (Network-safe)
    /// </summary>
    public void GainPossession(Player player)
    {
        if (PossessionPlayer == player)
            return;

        if (GameManager.Instance.IsMultiplayer)
        {
#if PHOTON_UNITY_NETWORKING
            // Only master client controls true ball ownership!
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_GainPossession), RpcTarget.All, player.PlayerId, player.TeamIndex);
            }
#endif
            return;
        }
        GainPossessionInternal(player);
    }

    /// <summary>
    /// Releases the current possession, firing event (network-safe)
    /// </summary>
    public void ReleasePossession()
    {
        if (PossessionPlayer == null)
            return;

        if (GameManager.Instance.IsMultiplayer)
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient && PossessionPlayer != null)
            {
                photonView.RPC(nameof(RPC_ReleasePossession), RpcTarget.All, PossessionPlayer.PlayerId, PossessionPlayer.TeamIndex);
            }
#endif
            return;
        }
        ReleasePossessionInternal();
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_GainPossession(string playerId, int teamIndex)
    {
        Player p = FindPlayerById(playerId, teamIndex);
        GainPossessionInternal(p);
    }

    [PunRPC]
    private void RPC_ReleasePossession(string playerId, int teamIndex)
    {
        Player p = FindPlayerById(playerId, teamIndex);
        if (p == PossessionPlayer)
            ReleasePossessionInternal();
    }

    private Player FindPlayerById(string playerId, int teamIndex)
    {
        var players = GameManager.Instance.Teams[teamIndex].players;
        foreach (var p in players)
            if (p.PlayerId == playerId)
                return p;

        Debug.LogError($"PossessionManager: Could not find player with ID {playerId} (teamIndex={teamIndex})");
        return null;
    }
#endif

    private void GainPossessionInternal(Player player)
    {
        if (PossessionPlayer == player)
            return;

        ReleasePossessionInternal(); // Release old if any

        PossessionPlayer = player;
        if (PossessionPlayer != null)
            PossessionPlayer.IsPossession = true;
        Debug.Log("GainPossession: " + (player != null ? player.PlayerId : "null"));
        OnPossessionGained?.Invoke(PossessionPlayer);
    }

    private void ReleasePossessionInternal()
    {
        if (PossessionPlayer != null)
        {
            LastPossessionPlayer = PossessionPlayer;
            Debug.Log("ReleasePossession: " + LastPossessionPlayer.PlayerId);
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

#if PHOTON_UNITY_NETWORKING
    // Optional: sync PossessionPlayer state over the network if you want extra robustness:
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Unused for now—events are enough for possession
    }
#endif
}
