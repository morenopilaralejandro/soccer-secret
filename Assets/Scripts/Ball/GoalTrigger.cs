using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class GoalTrigger : MonoBehaviour
{
    public Team Team;

#if PHOTON_UNITY_NETWORKING
    private bool IsMultiplayer => PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined;
#else
    private bool IsMultiplayer => false;
#endif

    private void OnTriggerEnter(Collider other)
    {
        // Make sure this script is on the goal, and the ball has a Collider & Rigidbody!
        if (other.CompareTag("Ball") && !GameManager.Instance.IsTimeFrozen)
        {
            if (!IsMultiplayer
#if PHOTON_UNITY_NETWORKING
                || PhotonNetwork.IsMasterClient
#endif
                )
            {
                GameManager.Instance.OnGoalScored(Team);
            }
        }
    }
}
