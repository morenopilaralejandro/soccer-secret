using UnityEngine;

public class DuelCollider : MonoBehaviour
{
    [SerializeField] private float duelCooldown = 0.2f; // cooldown in seconds
    private float nextDuelAllowedTime = 0f;
    private Player cachedPlayer;

    void Awake()
    {
        cachedPlayer = GetComponentInParent<Player>();
    }

    void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (Time.time < nextDuelAllowedTime) return;
        if (GameManager.Instance.IsMovementFrozen) return;

        HandleTrigger(other);
        nextDuelAllowedTime = Time.time + duelCooldown;
    }

    private void HandleTrigger(Collider otherDuelCollider)
    {
        GameObject thisRootObj = cachedPlayer.gameObject;
        GameObject otherRootObj = otherDuelCollider.transform.root.gameObject;

        string possessionPlayerTag = tag;
        string otherPlayerTag = otherDuelCollider.tag;

        if (cachedPlayer.IsPossession
            && possessionPlayerTag != null
            && (otherPlayerTag == "Ally" || otherPlayerTag == "Opp")
            && possessionPlayerTag != otherPlayerTag)
        {
            GameManager.Instance.HandleDuel(thisRootObj, otherRootObj, 0);
        }
    }
}
