using UnityEngine;
using System;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

[RequireComponent(typeof(Collider))]
public class ComboCollider : MonoBehaviour
{
    #region Private Fields

    private Player _cachedPlayer;

    #endregion

    #region Events

    public static event Action<Player> OnSetStatusPlayer;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _cachedPlayer = GetComponentInParent<Player>();
        if (_cachedPlayer == null)
            Debug.LogError("[ComboCollider] Could not find Player in parent.");
        else
            Debug.Log($"[ComboCollider] Found Player: {_cachedPlayer.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("[ComboCollider] OnTriggerEnter invoked.");
        TryHandleTrigger(other);
    }

    #endregion

    #region Combo Logic

    private void TryHandleTrigger(Collider otherCollider)
    {
        //Debug.Log("[ComboCollider] TryHandleTrigger invoked.");

        // Basic preconditions
        if (DuelManager.Instance.IsDuelResolved())
        {
            //Debug.Log("[ComboCollider] Duel already resolved. Exiting.");
            return;
        }
        if (GameManager.Instance.IsMovementFrozen)
        {
            //Debug.Log("[ComboCollider] Movement is frozen. Exiting.");
            return;
        }
        if (_cachedPlayer == null)
        {
            //Debug.LogError("[ComboCollider] Cached player is null. Exiting.");
            return;
        }

        DuelParticipant lastOffense = DuelManager.Instance.GetLastOffense();
        DuelParticipant lastDefense = DuelManager.Instance.GetLastDefense();

        // Prevent repeat triggers by the same defense player
        if (lastDefense != null && lastDefense.Player == _cachedPlayer)
        {
            //Debug.Log("[ComboCollider] Last defense player is this player. Exiting.");
            return;
        }
        if (lastOffense == null || _cachedPlayer == lastOffense.Player)
        {
            //Debug.Log("[ComboCollider] No valid last offense, or offense player is same as cached. Exiting.");
            return;
        }

        // Only respond to ball collision
        if (!otherCollider.transform.root.CompareTag("Ball"))
        {
            //Debug.Log("[ComboCollider] Triggered by non-ball object. Exiting.");
            return;
        }

        // Network pattern: only game authority can register
        if (!GameManager.Instance.IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            || PhotonNetwork.IsMasterClient
#endif
            )
        {
            int participantIndex = DuelManager.Instance.GetDuelParticipants().Count;
            Debug.Log($"[ComboCollider] Registering trigger for {_cachedPlayer.name} as participant {participantIndex}.");
            DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, false);
            OnSetStatusPlayer?.Invoke(_cachedPlayer);


            AssignComboRoles(lastOffense.Player, participantIndex);
            // <<<<< Core logic focus for Shoot >>>>>
            if (DuelManager.Instance.GetDuelMode() == DuelMode.Shoot &&
                _cachedPlayer.ControlType == ControlType.LocalHuman)
            {
                // Only the shooter gets UI, and only if human and local.
                UIManager.Instance.BeginDuelSelectionPhase();
            }
        }
        else
        {
            //Debug.Log("[ComboCollider] Not game authority. Duel not registered.");
        }
    }

 private void AssignComboRoles(Player lastOffensePlayer, int participantIndex)
    {
        bool isSameTeam = _cachedPlayer.TeamIndex == lastOffensePlayer.TeamIndex;
        Category selectedCategory = isSameTeam
            ? (_cachedPlayer.ControlType == ControlType.Ai ? Category.Block : Category.Shoot)
            : (_cachedPlayer.ControlType == ControlType.Ai ? Category.Shoot : Category.Block);

Debug.Log($"[ComboCollider] AssignComboRoles: {_cachedPlayer.name} (Team: {_cachedPlayer.TeamIndex}, AI: {_cachedPlayer.ControlType == ControlType.Ai}, Category: {selectedCategory})");

        if (_cachedPlayer.ControlType == ControlType.Ai)
        {
Debug.Log("[ComboCollider] Registered AI selection.");
            _cachedPlayer.GetComponent<PlayerAi>().RegisterAiSelections(participantIndex, selectedCategory);
        }
        else
        {
            // No need to call UIManager here; that happens above only for Shoot mode and local human
            BallTravelController.Instance.PauseTravel();
            UIManager.Instance.SetDuelSelection(_cachedPlayer.TeamIndex, selectedCategory, participantIndex, _cachedPlayer);
            // UI prompt happens in BeginDuelSelectionPhase call (above)
        }
    }

    #endregion
}
