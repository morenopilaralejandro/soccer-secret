using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;

public class QuickPlay : MonoBehaviourPunCallbacks
{
    public static event Action<int, bool> OnStatusUpdate;
    public static event Action OnMatchFound;

    void Start()
    {

    }

    public void StartQuickPlay()
    {
        OnStatusUpdate?.Invoke(1, false);
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
        else
            TryQuickPlay();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        TryQuickPlay();
    }

    public void TryQuickPlay()
    {
        OnStatusUpdate?.Invoke(2, false);
        Hashtable expectedCustomRoomProperties = new Hashtable() { { "type", "quick" } };
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        OnStatusUpdate?.Invoke(3, false);
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            CustomRoomProperties = new Hashtable { { "type", "quick" } },
            CustomRoomPropertiesForLobby = new string[] { "type" }
        };
        PhotonNetwork.CreateRoom(null, options);
    }

    public override void OnJoinedRoom()
    {
        OnStatusUpdate?.Invoke(4, false);
        OnMatchFound?.Invoke();
        // Optionally: PhotonNetwork.LoadLevel("YourGameScene");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("QuickPlay OnDisconnected" + cause);
        OnStatusUpdate?.Invoke(5, true);
    }
}
