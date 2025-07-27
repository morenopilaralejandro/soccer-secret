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

    public Player CurrentPlayer { get; private set; }
    public Player LastPlayer { get; private set; }
    public float LastKickTime { get; private set; } = -Mathf.Infinity;

    [SerializeField, Tooltip("Cooldown in seconds before the same player can regain possession")] 
    private float cooldown = 0.2f;

    public event Action<Player> OnGained;
    public event Action<Player> OnLost;

#if PHOTON_UNITY_NETWORKING
    private PhotonView view => PhotonView.Get(this);
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Subscribe(Action<Player> onGained, Action<Player> onLost)
    {
        OnGained += onGained;
        OnLost   += onLost;
    }

    public void Unsubscribe(Action<Player> onGained, Action<Player> onLost)
    {
        OnGained -= onGained;
        OnLost   -= onLost;
    }

    public bool IsOnCooldown(Player player)
    {
        return player == LastPlayer && (Time.time - LastKickTime) <= cooldown;
    }

    public void Gain(Player player)
    {
        if (player == null || player == CurrentPlayer || IsOnCooldown(player))
            return;

#if PHOTON_UNITY_NETWORKING
        if (GameManager.Instance.IsMultiplayer && PhotonNetwork.IsMasterClient)
        {
            view.RPC(nameof(RpcGain), RpcTarget.All, player.PlayerId, player.TeamIndex);
            return;
        }
#endif
        ApplyGain(player);
    }

    public void Release()
    {
        if (CurrentPlayer == null)
            return;

#if PHOTON_UNITY_NETWORKING
        if (GameManager.Instance.IsMultiplayer && PhotonNetwork.IsMasterClient)
        {
            view.RPC(nameof(RpcRelease), RpcTarget.All, CurrentPlayer.PlayerId, CurrentPlayer.TeamIndex);
            return;
        }
#endif
        ApplyRelease();
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RpcGain(string playerId, int team)
    {
        var player = FindPlayer(playerId, team);
        ApplyGain(player);
    }

    [PunRPC]
    private void RpcRelease(string playerId, int team)
    {
        var player = FindPlayer(playerId, team);
        if (player == CurrentPlayer)
            ApplyRelease();
    }

    private Player FindPlayer(string id, int team)
    {
        var players = GameManager.Instance.Teams[team].players;
        foreach (var p in players)
            if (p.PlayerId == id)
                return p;

        GameLogger.Error($"[PossessionManager] Player not found: {id} (team {team})", this);
        return null;
    }
#endif

    private void ApplyGain(Player player)
    {
        Release();

        CurrentPlayer = player;
        player.IsPossession = true;
        GameLogger.Info($"[PossessionManager] Possession gained by {player.PlayerName}", this);
        OnGained?.Invoke(player);
    }

    private void ApplyRelease()
    {
        LastPlayer = CurrentPlayer;
        LastKickTime = Time.time;

        CurrentPlayer.IsPossession = false;
        GameLogger.Info($"[PossessionManager] Possession released by {LastPlayer.PlayerName}", this);
        OnLost?.Invoke(CurrentPlayer);
        CurrentPlayer = null;
    }

    public void ResetState()
    {
        CurrentPlayer = null;
        LastPlayer = null;
        LastKickTime = -Mathf.Infinity;
    }

#if PHOTON_UNITY_NETWORKING
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Intentionally left blank; events handle state sync.
    }
#endif
}
