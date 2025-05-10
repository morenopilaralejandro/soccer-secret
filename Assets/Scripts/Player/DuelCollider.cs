using UnityEngine;
using System;

public class DuelCollider : MonoBehaviour
{
    [SerializeField] private float duelCooldown = 0.2f; // cooldown in seconds
    private float nextDuelAllowedTime = 0f;
    private Player cachedPlayer;

    public static event Action<Player> OnSetStatusPlayer;

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
            GameManager.Instance.FreezeGame();
            DuelManager.Instance.ResetDuel();
            DuelManager.Instance.RegisterTrigger(thisRootObj);
            DuelManager.Instance.RegisterTrigger(otherRootObj);
            DuelCollider.OnSetStatusPlayer?.Invoke(cachedPlayer);
            DuelCollider.OnSetStatusPlayer?.Invoke(otherRootObj.GetComponent<Player>());

            if (cachedPlayer.IsAi) {
                //ai
                DuelManager.Instance.RegisterUISelections(
                0,
                Category.Dribble,
                DuelAction.Offense,
                DuelCommand.Phys,
                null);

                UIManager.Instance.UserCategory = Category.Block;
                UIManager.Instance.UserIndex = 1;
                UIManager.Instance.UserPlayer = otherRootObj.GetComponent<Player>();
                UIManager.Instance.UserAction = DuelAction.Defense;
            } else {
                UIManager.Instance.UserCategory = Category.Dribble;
                UIManager.Instance.UserIndex = 0;
                UIManager.Instance.UserPlayer = cachedPlayer;
                UIManager.Instance.UserAction = DuelAction.Offense;

                UIManager.Instance.AiCategory = Category.Block;
                UIManager.Instance.AiIndex = 1;
                UIManager.Instance.AiPlayer = otherRootObj.GetComponent<Player>();
                UIManager.Instance.AiAction = DuelAction.Defense;

            }
            UIManager.Instance.SetButtonDuelToggleVisible(true);
        }
    }
}
