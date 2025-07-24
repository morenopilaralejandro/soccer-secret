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

        if (DuelManager.Instance.GetDuelMode() != DuelMode.Shoot) {
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
        bool isSameTeam = _cachedPlayer.TeamIndex == lastOffense.Player.TeamIndex;

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
        if (!otherCollider.transform.CompareTag("Ball"))
        {
            //Debug.Log("[ComboCollider] Triggered by non-ball object. Exiting.");
            return;
        }

        if (!isSameTeam && _cachedPlayer.IsKeeper && (GameManager.Instance.GetDistanceToAllyGoal(_cachedPlayer) < DuelManager.Instance.KeeperGoalDistance)) 
        {
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
            OnSetStatusPlayer?.Invoke(_cachedPlayer);
           
            //DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, false);



        Category selectedCategory;

        if (isSameTeam) 
        {
            selectedCategory = Category.Shoot;
        } else {
            selectedCategory = Category.Block;
        }

        
            if (GameManager.Instance.IsMultiplayer)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("[ComboCollider] Multiplayer");
                }
            }
            else // Singleplayer/offline
            {
                // Important: Register both participants BEFORE selections!
                DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, false);
                UIManager.Instance.SetDuelSelection(_cachedPlayer.TeamIndex, selectedCategory, participantIndex, _cachedPlayer);
                UIManager.Instance.SetShootTeamIndex(_cachedPlayer.TeamIndex); 
                BallTravelController.Instance.PauseTravel();
                UIManager.Instance.BeginDuelSelectionPhase();
            }


/*
            if (DuelManager.Instance.GetDuelMode() == DuelMode.Shoot)
            {
                UIManager.Instance.SetDuelSelection(_cachedPlayer.TeamIndex, selectedCategory, participantIndex, _cachedPlayer);
                if (_cachedPlayer.ControlType == ControlType.Ai)
                {
                    _cachedPlayer.GetComponent<PlayerAi>().RegisterAiSelections(_cachedPlayer.TeamIndex, selectedCategory);
                }
                else
                {
                    BallTravelController.Instance.PauseTravel();
                    if (_cachedPlayer.ControlType == ControlType.LocalHuman)
                        UIManager.Instance.BeginDuelSelectionPhase();
                }
            }
/*
            /*
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

            */
        }
    }



    #endregion
}
