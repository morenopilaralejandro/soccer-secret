using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class BallCollider : MonoBehaviour
{
    private void Awake() { }

    private void OnTriggerEnter(Collider collider)
    {
        if (BallTravelController.Instance.IsTraveling) return;

        Player playerComp = collider.GetComponentInParent<Player>();
        bool validPossession = false;
        bool isKeeper = false;

        if (playerComp)
            GameLogger.DebugLog("[BallCollider] OnTriggerEnter: " + playerComp.PlayerId, this);

        // Standard player touch
        if (collider.CompareTag("Player"))
        {
            validPossession = true;
        }
        // Keeper special case
        else if (
            collider.CompareTag("PlayerKeeperCollider") &&
            playerComp != null &&
            PossessionManager.Instance.LastPlayer &&
            PossessionManager.Instance.LastPlayer.TeamIndex != playerComp.TeamIndex && //keeper won't stop a pass from a player in its same team
            GameManager.Instance.GetDistanceToAllyGoal(playerComp) < DuelManager.Instance.KeeperGoalDistance)
        {
            isKeeper = true;
            validPossession = true;
        }

        // Shared cooldown and possession logic
        if (
            PossessionManager.Instance.CurrentPlayer == null &&
            validPossession &&
            playerComp != null)
        {
            if (!PossessionManager.Instance.IsOnCooldown(playerComp))
            {
                // Only allow the master (multiplayer) or anyone (offline) to claim possession:
                if (!GameManager.Instance.IsMultiplayer ||
#if PHOTON_UNITY_NETWORKING
                    PhotonNetwork.IsMasterClient
#else
                    true
#endif
                )
                {
                    PossessionManager.Instance.Gain(playerComp);
                    if (isKeeper)
                        AudioManager.Instance.PlaySfx("SfxCatch");
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObj = collision.collider.gameObject;
        GameLogger.DebugLog("[BallCollider] OnCollisionEnter: " + hitObj.name + " (Tag: " + hitObj.tag + ")", this);

        if (BallTravelController.Instance.IsTraveling && hitObj.CompareTag("Bound"))
        {
            DuelLogManager.Instance.AddDuelCancel();
            BallTravelController.Instance.CancelTravel();
        }
    }
}
