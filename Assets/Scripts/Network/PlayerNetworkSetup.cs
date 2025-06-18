using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSetup : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Always parent under the field!
        transform.SetParent(GameManager.Instance.FieldRoot, worldPositionStays: true);

        // Consistent visual orientation
        transform.Rotate(90f, 0f, 0f, Space.Self);

        // Set up references
        var player = GetComponent<Player>();
        
        // Control type: local or remote?
        player.ControlType = photonView.IsMine ? ControlType.LocalHuman : ControlType.RemoteHuman;

        // Get team index and playerId from instantiation data
        if (photonView.InstantiationData != null && photonView.InstantiationData.Length >= 2)
        {
            int teamIndex = (int)photonView.InstantiationData[0];
            string teamId = (string)photonView.InstantiationData[1];
            string playerId = (string)photonView.InstantiationData[2];

            player.TeamIndex = teamIndex;

            // Look up PlayerData
            Team team = TeamManager.Instance.GetTeamById(teamId);
            PlayerData pdata = null;
            if (team != null)
            {
                pdata = PlayerManager.Instance.GetPlayerDataById(playerId);
            }
            if (pdata != null)
            {
                player.Initialize(pdata);
                player.SetWear(team);
            }
            else
            {
                Debug.LogWarning($"Could not initialize remote player: TeamIndex={teamIndex}, PlayerId={playerId}");
            }

            if (!GameManager.Instance.Teams[teamIndex].players.Contains(player))
            {
                GameManager.Instance.Teams[teamIndex].players.Add(player);
            }
        }
        else
        {
            Debug.LogWarning("No instantiation data found for Player setup!");
        }
    }
}
