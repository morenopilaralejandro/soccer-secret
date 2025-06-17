using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class BallCollider : MonoBehaviour
{
    [SerializeField] private float keeperGoalDistance = 0.5f;

    private void Awake() { }

    private void OnTriggerEnter(Collider collider)
    {
        if (BallTravelController.Instance.IsTraveling) return;

        Player playerComp = collider.GetComponentInParent<Player>();
        bool validPossession = false;
        bool isKeeper = false;

        if (playerComp)
            Debug.Log("BallBehavior OnTriggerEnter: " + playerComp.PlayerId);

        // Standard player touch
        if (collider.CompareTag("Player"))
        {
            validPossession = true;
        }
        // Keeper special case
        else if (
            collider.CompareTag("PlayerKeeperCollider") &&
            playerComp != null &&
            PossessionManager.Instance.LastPossessionPlayer.ControlType != ControlType.LocalHuman && //keeper won't stop a pass from a player in its same team
            GameManager.Instance.GetDistanceToAllyGoal(playerComp) < keeperGoalDistance)
        {
            isKeeper = true;
            validPossession = true;
        }

        // Shared cooldown and possession logic
        if (
            PossessionManager.Instance.PossessionPlayer == null &&
            validPossession &&
            playerComp != null)
        {
            if (!PossessionManager.Instance.IsCooldownActive(playerComp))
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
                    PossessionManager.Instance.GainPossession(playerComp);
                    if (isKeeper)
                        AudioManager.Instance.PlaySfx("SfxCatch");
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObj = collision.collider.gameObject;
        Debug.Log("BallBehavior OnCollisionEnter: " + hitObj.name + " (Tag: " + hitObj.tag + ")");

        if (BallTravelController.Instance.IsTraveling && hitObj.CompareTag("Bound"))
        {
            BallTravelController.Instance.CancelTravel();
        }
    }
}
