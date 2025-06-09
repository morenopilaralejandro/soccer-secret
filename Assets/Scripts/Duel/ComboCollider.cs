using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class ComboCollider : MonoBehaviour
{
    private Player cachedPlayer;

    public static event Action<Player> OnSetStatusPlayer;

    private void Awake()
    {
        cachedPlayer = GetComponentInParent<Player>();
        if (cachedPlayer == null)
            Debug.LogError($"{nameof(ComboCollider)} could not find Player in parent.");
    }

    private void OnTriggerEnter(Collider other) => TryHandleTrigger(other);

    private void TryHandleTrigger(Collider otherCollider)
    {
        Debug.Log("ComboCollider OnTriggerEnter");

        // Basic conditions before progressing
        if (DuelManager.Instance.IsDuelResolved()
            || GameManager.Instance.IsMovementFrozen
            || cachedPlayer == null)
            return;

        DuelParticipant lastOffense = DuelManager.Instance.GetLastOffense();
        DuelParticipant lastDefense = DuelManager.Instance.GetLastDefense();

        // Prevent repeat triggers by the same defense player
        if (lastDefense != null && lastDefense.Player == cachedPlayer)
            return;

        if (lastOffense == null || cachedPlayer == lastOffense.Player)
            return;

        // Only respond to ball collision
        if (!otherCollider.transform.root.CompareTag("Ball"))
            return;

        int participantIndex = DuelManager.Instance.GetDuelParticipants().Count;
        DuelManager.Instance.RegisterTrigger(cachedPlayer.gameObject, false);
        OnSetStatusPlayer?.Invoke(cachedPlayer);

        // Role assignment based on previous play and ai/user
        AssignComboRoles(lastOffense.Player, participantIndex);
    }

    private void AssignComboRoles(Player lastOffensePlayer, int idx)
    {
        bool isUser = !cachedPlayer.IsAi;

        if (lastOffensePlayer.IsAlly)
        {
            // Allies were last offense; defense blocks, chain offense can be user or ai
            if (cachedPlayer.IsAi)
            {
                cachedPlayer.GetComponent<PlayerAi>().RegisterAiSelections(idx, Category.Block);
            }
            else
            {
                HandleUserChain(idx, Category.Shoot, cachedPlayer);
            }
        }
        else
        {
            // Opponent was last offense; so now chain or block
            if (cachedPlayer.IsAi)
            {
                cachedPlayer.GetComponent<PlayerAi>().RegisterAiSelections(idx, Category.Shoot);
            }
            else
            {
                HandleUserChain(idx, Category.Block, cachedPlayer);
            }
        }
    }

    private void HandleUserChain(int index, Category category, Player cachedPlayer)
    {
        GameManager.Instance.FreezeGame();
        BallTravelController.Instance.PauseTravel();
        GameManager.Instance.SetGamePhase(GamePhase.Duel);
        UIManager.Instance.SetUserRole(category, index, cachedPlayer);
        UIManager.Instance.SetButtonDuelToggleVisible(true);
    }
}
