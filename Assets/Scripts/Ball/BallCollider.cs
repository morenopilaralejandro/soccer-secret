using UnityEngine;

public class BallCollider : MonoBehaviour
{
    [SerializeField] private float keeperGoalDistance = 0.5f;

    private void Awake()
    {

    }

    private void OnTriggerEnter(Collider collider)
    {
        GameObject rootObj = collider.transform.root.gameObject;
        Debug.Log("BallBehavior OnTriggerEnter: " + rootObj.name + " (Tag: " + rootObj.tag + ")");

        if (BallTravelController.Instance.IsTraveling) return;

        Player playerComp = rootObj.GetComponent<Player>();
        bool validPossession = false;
        bool isKeeper = false;

        // Standard player touch
        if (collider.CompareTag("Player"))
        {
            validPossession = true;
        }
        // Keeper special case
        else if (
            collider.CompareTag("PlayerKeeperCollider") &&
            playerComp != null &&
            PossessionManager.Instance.LastPossessionPlayer.GetComponent<Player>().IsAlly != playerComp.IsAlly && //keeper won't stop a pass from a player in it's same team
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
                PossessionManager.Instance.GainPossession(playerComp);
                if (isKeeper)
                    AudioManager.Instance.PlaySfx("SfxCatch");
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
