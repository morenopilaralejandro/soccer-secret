using UnityEngine;
using System;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

[RequireComponent(typeof(Collider))]
public class DuelCollider : MonoBehaviour
{
    #region Inspector Fields

    [Header("Duel Settings")]
    [SerializeField] private float duelCooldown = 0.2f;

    #endregion

    #region Private Fields

    private float _nextDuelAllowedTime = 0f;
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
            Debug.LogError("[DuelCollider] Could not find attached Player component in parent.");
        else
            Debug.Log($"[DuelCollider] Player component found: {_cachedPlayer.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("[DuelCollider] OnTriggerEnter detected.");
        TryStartDuel(other);
    }

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("[DuelCollider] OnTriggerStay detected.");
        TryStartDuel(other);
    }

    #endregion

    #region Duel Logic

    private void TryStartDuel(Collider otherCollider)
    {
        //Debug.Log("[DuelCollider] TryStartDuel invoked.");

        if (!CanStartDuel())
        {
            //Debug.Log("[DuelCollider] Cannot start duel: cooldown or movement frozen.");
            return;
        }

        Player otherPlayer = otherCollider.GetComponentInParent<Player>();
        if (otherPlayer == null || otherPlayer == _cachedPlayer)
        {
            //Debug.Log("[DuelCollider] Other collider is not a valid, different Player.");
            return;
        }

        // Both must be on different teams, and the first has possession, and duel is resolved
        if (_cachedPlayer.IsPossession &&
            _cachedPlayer.TeamIndex != otherPlayer.TeamIndex &&
            DuelManager.Instance.IsDuelResolved())
        {
            // Only allow duel to be started by MasterClient (in multiplayer), or in offline
            if (!GameManager.Instance.IsMultiplayer
#if PHOTON_UNITY_NETWORKING
                || PhotonNetwork.IsMasterClient
#endif
            )
            {
                Debug.Log($"[DuelCollider] Starting duel between {_cachedPlayer.name} (Team {_cachedPlayer.TeamIndex}) and {otherPlayer.name} (Team {otherPlayer.TeamIndex}).");

                SetDuelCooldown();

                DuelManager.Instance.StartDuel(DuelMode.Field);
                DuelManager.Instance.RegisterTrigger(_cachedPlayer.gameObject, false);
                DuelManager.Instance.RegisterTrigger(otherPlayer.gameObject, false);

                OnSetStatusPlayer?.Invoke(_cachedPlayer);
                OnSetStatusPlayer?.Invoke(otherPlayer);

             AssignDuelRolesAndBegin(_cachedPlayer, otherPlayer);

                // *_Core UI logic for Field Duel only_*
                if (DuelManager.Instance.GetDuelMode() == DuelMode.Field &&
                   // This player is local human or the other is (supports either being local)
                   (_cachedPlayer.ControlType == ControlType.LocalHuman ||
                    otherPlayer.ControlType == ControlType.LocalHuman))
                {
                    UIManager.Instance.BeginDuelSelectionPhase();
                }
            }
            else
            {
                Debug.Log("[DuelCollider] Duel not started: not master client in multiplayer.");
            }
        }
        else
        {
            //Debug.Log("[DuelCollider] Duel conditions not met (possession, team, duel state).");
        }
    }

    private bool CanStartDuel()
    {
        bool canStart = Time.time >= _nextDuelAllowedTime && !GameManager.Instance.IsMovementFrozen;
        //Debug.Log($"[DuelCollider] CanStartDuel: {canStart} (Time: {Time.time}, NextAllowed: {_nextDuelAllowedTime}, IsMovementFrozen: {GameManager.Instance.IsMovementFrozen})");
        return canStart;
    }

    private void SetDuelCooldown()
    {
        _nextDuelAllowedTime = Time.time + duelCooldown;
        //Debug.Log($"[DuelCollider] Set duel cooldown. Next duel time: {_nextDuelAllowedTime}");
    }

    /// <summary>
    /// Assigns duel categories and selection slots for both participants.
    /// Both are team-based; no AI vs User special-casing.
    /// </summary>
private void AssignDuelRolesAndBegin(Player playerA, Player playerB)
{
    Debug.Log("[DuelCollider] AssignDuelRolesAndBegin started.");

    Category categoryA = Category.Dribble;
    Category categoryB = Category.Block;
    int indexA = 0, indexB = 1;

    Debug.Log($"[DuelCollider] {playerA.name} assigned {categoryA}, {playerB.name} assigned {categoryB}");

    UIManager.Instance.SetDuelSelection(playerA.TeamIndex, categoryA, indexA, playerA);
    UIManager.Instance.SetDuelSelection(playerB.TeamIndex, categoryB, indexB, playerB);

    Debug.Log($"[DuelCollider] Player A ({playerA.name}) ControlType: {playerA.ControlType}; indexA: {indexA}; team: {playerA.TeamIndex}");
    Debug.Log($"[DuelCollider] Player B ({playerB.name}) ControlType: {playerB.ControlType}; indexB: {indexB}; team: {playerB.TeamIndex}");

    // Defensive: only host/authority triggers AI moves in multiplayer, otherwise they race!
    bool isMaster = !GameManager.Instance.IsMultiplayer
#if PHOTON_UNITY_NETWORKING
        || PhotonNetwork.IsMasterClient
#endif
        ;

    if (playerA.ControlType == ControlType.Ai && isMaster)
    {
        Debug.Log($"[DuelCollider] Triggering AI selection for {playerA.name} (team {playerA.TeamIndex}, indexA {indexA})");
        var ai = playerA.GetComponent<PlayerAi>();
        if (ai != null)
            ai.RegisterAiSelections(playerA.TeamIndex, categoryA);
        else
            Debug.LogError($"[DuelCollider] PlayerAi component missing on {playerA.name}!");
    } 
    if (playerB.ControlType == ControlType.Ai && isMaster)
    {
        Debug.Log($"[DuelCollider] Triggering AI selection for {playerB.name} (team {playerB.TeamIndex}, indexB {indexB})");
        var ai = playerB.GetComponent<PlayerAi>();
        if (ai != null)
            ai.RegisterAiSelections(playerB.TeamIndex, categoryB);
        else
            Debug.LogError($"[DuelCollider] PlayerAi component missing on {playerB.name}!");
    } 
}

    #endregion
}
