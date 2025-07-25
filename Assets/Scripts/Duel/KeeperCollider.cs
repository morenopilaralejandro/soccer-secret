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
            GameLogger.Error("[KeeperCollider] Could not find Player in parent.", this);
        else
            GameLogger.Info($"[KeeperCollider] Player found: {_cachedPlayer.name}", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleTrigger(other);
    }

    #endregion

    #region Keeper Logic

    private void TryHandleTrigger(Collider otherCollider)
    {
        // Only respond to ball collision
        if (!otherCollider.transform.CompareTag("Ball"))
            return;

        if (DuelManager.Instance.GetDuelMode() != DuelMode.Shoot)
            return;

        // Pre-duel state checks
        if (DuelManager.Instance.IsDuelResolved())
            return;
        if (GameManager.Instance.IsMovementFrozen)
            return;
        if (_cachedPlayer == null)
            return;
        if (DuelManager.Instance.GetLastOffense() == null)
            return;

        DuelParticipant lastDefense = DuelManager.Instance.GetLastDefense();
        DuelParticipant lastOffense = DuelManager.Instance.GetLastOffense();

        // Prevent repeat triggers and self defense
        if (lastDefense != null && lastDefense.Player == _cachedPlayer)
            return;
        if (lastOffense != null && _cachedPlayer == lastOffense.Player)
            return;

        // Prevent catching friendly fire
        if (lastOffense.Player.TeamIndex == _cachedPlayer.TeamIndex)
            return;

        // Only allow if close enough to own goal
        float distanceToGoal = GameManager.Instance.GetDistanceToAllyGoal(_cachedPlayer);
        if (distanceToGoal > DuelManager.Instance.KeeperGoalDistance)
            return;

        // Only allow by authority/master client
        if (!GameManager.Instance.IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            || PhotonNetwork.IsMasterClient
#endif
        )
        {
            int participantIndex = DuelManager.Instance.GetDuelParticipants().Count;
            GameLogger.Info($"[KeeperCollider] Registering trigger for {_cachedPlayer.name} as participant {participantIndex}.", this);
            OnSetStatusPlayer?.Invoke(_cachedPlayer);

            if (GameManager.Instance.IsMultiplayer)
            {
#if PHOTON_UNITY_NETWORKING
                if (PhotonNetwork.IsMasterClient)
                {
                    GameLogger.DebugLog("[KeeperCollider] Multiplayer authority registering trigger.", this);
                }
#endif
            }
            else // Singleplayer/offline
            {
                // Register both participants BEFORE selections
                DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, false);
                UIManager.Instance.SetDuelSelection(_cachedPlayer.TeamIndex, Category.Catch, participantIndex, _cachedPlayer);
                UIManager.Instance.SetShootTeamIndex(_cachedPlayer.TeamIndex);
                BallTravelController.Instance.PauseTravel();
                UIManager.Instance.BeginDuelSelectionPhase();
            }
        }
    }

    #endregion
}
