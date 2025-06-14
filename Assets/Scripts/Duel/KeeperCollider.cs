using UnityEngine;
using System;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

[RequireComponent(typeof(Collider))]
public class KeeperCollider : MonoBehaviour
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
            Debug.LogError("[KeeperCollider] Could not find Player in parent.");
        else
            Debug.Log($"[KeeperCollider] Player found: {_cachedPlayer.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("[KeeperCollider] OnTriggerEnter invoked.");
        TryHandleTrigger(other);
    }

    #endregion

    #region Keeper Logic

    private void TryHandleTrigger(Collider otherCollider)
    {
        //Debug.Log("[KeeperCollider] TryHandleTrigger invoked.");

        // Only respond to ball collision
        if (!otherCollider.transform.root.CompareTag("Ball"))
        {
            //Debug.Log("[KeeperCollider] Non-ball collision. Exiting.");
            return;
        }

        // Pre-duel state checks
        if (DuelManager.Instance.IsDuelResolved())
        {
            //Debug.Log("[KeeperCollider] Duel already resolved. Exiting.");
            return;
        }
        if (GameManager.Instance.IsMovementFrozen)
        {
            //Debug.Log("[KeeperCollider] Movement is frozen. Exiting.");
            return;
        }
        if (_cachedPlayer == null)
        {
            //Debug.LogError("[KeeperCollider] Cached player is null. Exiting.");
            return;
        }
        if (DuelManager.Instance.GetLastOffense() == null)
        {
            //Debug.Log("[KeeperCollider] No last offense. Exiting.");
            return;
        }

        DuelParticipant lastDefense = DuelManager.Instance.GetLastDefense();
        DuelParticipant lastOffense = DuelManager.Instance.GetLastOffense();

        if (lastDefense != null && lastDefense.Player == _cachedPlayer)
        {
            //Debug.Log("[KeeperCollider] Last defense is this player. Exiting.");
            return;
        }
        if (lastOffense != null && _cachedPlayer == lastOffense.Player)
        {
            //Debug.Log("[KeeperCollider] Offense player is cached player. Exiting.");
            return;
        }

        // Only allow by authority/master client
        if (!GameManager.Instance.IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            || PhotonNetwork.IsMasterClient
#endif
        )
        {
            int participantIndex = DuelManager.Instance.GetDuelParticipants().Count;
            Debug.Log($"[KeeperCollider] Registering trigger for {_cachedPlayer.name} as participant {participantIndex}.");
            DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, false);
            OnSetStatusPlayer?.Invoke(_cachedPlayer);
            SetupKeeperDuelUI(participantIndex, lastOffense.Player);
        }
        else
        {
            //Debug.Log("[KeeperCollider] Not game authority. Duel not registered.");
        }
    }

    private void SetupKeeperDuelUI(int participantIndex, Player lastOffensePlayer)
    {
        Debug.Log($"[KeeperCollider] SetupKeeperDuelUI called for participant {_cachedPlayer.name} (Index {participantIndex}).");

        if (_cachedPlayer.ControlType == ControlType.Ai)
        {
            Debug.Log("[KeeperCollider] Cached player is AI. Registering AI 'Catch' selection.");
            _cachedPlayer.GetComponent<PlayerAi>().RegisterAiSelections(participantIndex, Category.Catch);
        }
        else
        {
            Debug.Log("[KeeperCollider] Cached player is human. Showing UI for 'Catch' selection.");
            BallTravelController.Instance.PauseTravel();
            UIManager.Instance.SetDuelSelection(_cachedPlayer.TeamIndex, Category.Catch, participantIndex, _cachedPlayer);
            // Only run selection phase for the local human keeper
            if (_cachedPlayer.ControlType == ControlType.LocalHuman /* or your equivalent check */)
                UIManager.Instance.BeginDuelSelectionPhase();
        }
    }

    #endregion
}
