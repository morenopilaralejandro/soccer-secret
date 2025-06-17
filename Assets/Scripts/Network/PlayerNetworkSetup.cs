using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSetup : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Parent to fieldRoot EVERY time (on all clients)
        transform.SetParent(GameManager.Instance.FieldRoot, worldPositionStays: true);

        // Optional: Reset position/rotation to match spawned values (should not be needed unless transforms are acting up)
        // transform.position = transform.position; // Not needed, shown for clarity
        // transform.rotation = transform.rotation;

        // Rotate 90° round X axis, if needed for your game's visuals:
        transform.Rotate(90f, 0f, 0f, Space.Self);

        // ControlType: Is this my own player or a remote one?
        var player = GetComponent<Player>();
        player.ControlType = photonView.IsMine ? ControlType.LocalHuman : ControlType.RemoteHuman;

        // Retrieve any custom instantiation data (like which team this player is on!):
        if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
        {
            player.TeamIndex = (int)photonView.InstantiationData[0];
        }
    }
}
