using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class DuelCollider : MonoBehaviour
{
    [Header("Duel Settings")]
    [SerializeField] private float duelCooldown = 0.2f;

    private float nextDuelAllowedTime = 0f;
    private Player cachedPlayer;

    public static event Action<Player> OnSetStatusPlayer;

    private void Awake()
    {
        cachedPlayer = GetComponentInParent<Player>();
        if (cachedPlayer == null)
            Debug.LogError("DuelCollider could not find attached Player component in parent.");
    }

    private void OnTriggerEnter(Collider other) => TryStartDuel(other);
    private void OnTriggerStay(Collider other) => TryStartDuel(other);

    private void TryStartDuel(Collider otherCollider)
    {
        if (!CanStartDuel()) return;

        var otherPlayer = otherCollider.GetComponentInParent<Player>();
        if (otherPlayer == null || otherPlayer == cachedPlayer)
            return;

        // Tag logic: You could refactor these if you’re consistently using tags or layer masks elsewhere
        string thisTag = CompareTag("Ally") ? "Ally" : (CompareTag("Opp") ? "Opp" : string.Empty);
        string otherTag = otherCollider.CompareTag("Ally") ? "Ally" : (otherCollider.CompareTag("Opp") ? "Opp" : string.Empty);

        if (
            cachedPlayer.IsPossession &&
            !string.IsNullOrEmpty(thisTag) &&
            !string.IsNullOrEmpty(otherTag) &&
            thisTag != otherTag &&
            DuelManager.Instance.IsDuelResolved()
        )
        {
            SetDuelCooldown();

            GameManager.Instance.FreezeGame();
            DuelManager.Instance.StartDuel(DuelMode.Field);
            DuelManager.Instance.RegisterTrigger(cachedPlayer.gameObject, false);
            DuelManager.Instance.RegisterTrigger(otherPlayer.gameObject, false);

            OnSetStatusPlayer?.Invoke(cachedPlayer);
            OnSetStatusPlayer?.Invoke(otherPlayer);

            AssignUserAndAiRoles(otherPlayer);
            UIManager.Instance.SetButtonDuelToggleVisible(true);
        }
    }

    private bool CanStartDuel()
    {
        return Time.time >= nextDuelAllowedTime && !GameManager.Instance.IsMovementFrozen;
    }

    private void SetDuelCooldown()
    {
        nextDuelAllowedTime = Time.time + duelCooldown;
    }

    /// <summary>
    /// Assigns categories and roles for UI and Duel participants, depending on which is AI/user.
    /// </summary>
    private void AssignUserAndAiRoles(Player otherPlayer)
    {
        // If this player is AI, they're the offense, other is defense (human)
        if (cachedPlayer.IsAi)
        {
            cachedPlayer.GetComponent<PlayerAi>().RegisterAiSelections(0, Category.Dribble);
            UIManager.Instance.SetUserRole(Category.Block, 1, otherPlayer);
        }
        else
        {
            UIManager.Instance.SetUserRole(Category.Dribble, 0, cachedPlayer);
            UIManager.Instance.SetAiRole(Category.Block, 1, otherPlayer);
        }
    }
}
