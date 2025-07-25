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
            GameLogger.Error("[ComboCollider] Could not find Player in parent.", this);
        else
            GameLogger.Info($"[ComboCollider] Found Player: {_cachedPlayer.name}", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleTrigger(other);
    }

    #endregion

    #region Combo Logic

    private void TryHandleTrigger(Collider otherCollider)
    {
        GameLogger.DebugLog("[ComboCollider] TryHandleTrigger invoked.", this);

        float distToGoal = GameManager.Instance.GetDistanceToAllyGoal(_cachedPlayer);
        GameLogger.DebugLog($"Distance to ally goal: {distToGoal}", this);

        bool keeperNearGoal = _cachedPlayer.IsKeeper && distToGoal < DuelManager.Instance.KeeperGoalDistance;
        GameLogger.DebugLog($"Is keeper near goal? {keeperNearGoal}", this);

        // Basic preconditions
        if (DuelManager.Instance.IsDuelResolved())
            return;

        if (DuelManager.Instance.GetDuelMode() != DuelMode.Shoot)
            return;

        if (GameManager.Instance.IsMovementFrozen)
            return;
        
        if (_cachedPlayer == null)
            return;

        DuelParticipant lastOffense = DuelManager.Instance.GetLastOffense();
        DuelParticipant lastDefense = DuelManager.Instance.GetLastDefense();
        bool isSameTeam = lastOffense != null && (_cachedPlayer.TeamIndex == lastOffense.Player.TeamIndex);

        // Prevent repeat triggers by the same defense player
        if (lastDefense != null && lastDefense.Player == _cachedPlayer)
            return;

        if (lastOffense == null || _cachedPlayer == lastOffense.Player)
            return;

        // Only respond to ball collision
        if (!otherCollider.transform.CompareTag("Ball"))
            return;

        if (_cachedPlayer.IsKeeper && (distToGoal < DuelManager.Instance.KeeperGoalDistance))
            return;

        // Network pattern: only game authority can register
        if (!GameManager.Instance.IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            || PhotonNetwork.IsMasterClient
#endif
            )
        {
            int participantIndex = DuelManager.Instance.GetDuelParticipants().Count;

            GameLogger.Info($"[ComboCollider] Registering trigger for {_cachedPlayer.name} as participant {participantIndex}.", this);
            OnSetStatusPlayer?.Invoke(_cachedPlayer);

            Category selectedCategory = isSameTeam ? Category.Shoot : Category.Block;

            if (GameManager.Instance.IsMultiplayer)
            {
#if PHOTON_UNITY_NETWORKING
                if (PhotonNetwork.IsMasterClient)
                {
                    GameLogger.DebugLog("[ComboCollider] Multiplayer authority registering trigger.", this);
                }
#endif
            }
            else // Singleplayer/offline
            {
                // Register both participants before selections
                DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, false);
                UIManager.Instance.SetDuelSelection(_cachedPlayer.TeamIndex, selectedCategory, participantIndex, _cachedPlayer);
                UIManager.Instance.SetShootTeamIndex(_cachedPlayer.TeamIndex); 
                BallTravelController.Instance.PauseTravel();
                UIManager.Instance.BeginDuelSelectionPhase();
            }
        }
    }
    #endregion
}
