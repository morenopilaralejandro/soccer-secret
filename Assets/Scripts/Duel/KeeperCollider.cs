using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class KeeperCollider : MonoBehaviour
{
    private Player cachedPlayer;

    public static event Action<Player> OnSetStatusPlayer;

    private void Awake()
    {
        cachedPlayer = GetComponentInParent<Player>();
        if (cachedPlayer == null)
            Debug.LogError($"{nameof(KeeperCollider)} could not find Player in parent.");
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleTrigger(other);
    }

    private void TryHandleTrigger(Collider otherCollider)
    {
        Debug.Log("KeeperCollider OnTriggerEnter");

        // Only respond to ball collision
        if (!otherCollider.transform.root.CompareTag("Ball"))
            return;

        // Pre-duel state checks
        if (DuelManager.Instance.IsDuelResolved()
            || GameManager.Instance.IsMovementFrozen
            || cachedPlayer == null
            || DuelManager.Instance.GetLastOffense() == null)
        {
            return;
        }

        // Avoid duplicate/circular defense from this player
        DuelParticipant lastDefense = DuelManager.Instance.GetLastDefense();
        DuelParticipant lastOffense = DuelManager.Instance.GetLastOffense();
        if (lastDefense != null && lastDefense.Player == cachedPlayer)
            return;

        // Prevent self-trigger as offense
        if (cachedPlayer == lastOffense.Player)
            return;

        int participantIndex = DuelManager.Instance.GetDuelParticipants().Count;
        DuelManager.Instance.RegisterTrigger(cachedPlayer.gameObject, false);
        OnSetStatusPlayer?.Invoke(cachedPlayer);

        SetupKeeperDuelUI(participantIndex, lastOffense.Player);
    }

    private void SetupKeeperDuelUI(int index, Player lastOffensePlayer)
    {
        if (lastOffensePlayer.IsAi)
        {
            // User must input "catch"
            GameManager.Instance.FreezeGame();
            BallTravelController.Instance.PauseTravel();
            UIManager.Instance.SetUserRole(Category.Catch, index, cachedPlayer);
            UIManager.Instance.SetButtonDuelToggleVisible(true);
        }
        else
        {
            // AI automatically "catch"
            cachedPlayer.GetComponent<PlayerAi>().RegisterAiSelections(index, Category.Catch);
        }
    }
}
