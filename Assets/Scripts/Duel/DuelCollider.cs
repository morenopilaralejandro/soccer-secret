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
    [SerializeField] private float duelCooldown = 0.3f;

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
    }

    private void OnTriggerEnter(Collider other)   { TryStartDuel(other); }
    private void OnTriggerStay(Collider other)    { TryStartDuel(other); }

    #endregion

    #region Duel Logic

    private void TryStartDuel(Collider otherCollider)
    {
        if (!CanStartDuel()) return;

        Player otherPlayer = otherCollider.GetComponentInParent<Player>();
        if (otherPlayer == null || otherPlayer == _cachedPlayer)
            return;

        // Ensure different teams, possession, and duel not in progress
        if (_cachedPlayer.IsPossession &&
            _cachedPlayer.TeamIndex != otherPlayer.TeamIndex &&
            DuelManager.Instance.IsDuelResolved())
        {

            DuelManager.Instance.StartDuel(DuelMode.Field);
            // For UI status updates
            OnSetStatusPlayer?.Invoke(_cachedPlayer);
            OnSetStatusPlayer?.Invoke(otherPlayer);

            // Set duel cooldown immediately
            SetDuelCooldown();

            // Assign duel roles — customize as needed!
            Player playerA = _cachedPlayer;
            Player playerB = otherPlayer;
            Category categoryA = Category.Dribble; // Offense role
            Category categoryB = Category.Block;   // Defense role
            if (GameManager.Instance.IsMultiplayer)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    // Stage both participants in the same order on all clients via RPC
                    int[] teamIndices = { playerA.TeamIndex, playerB.TeamIndex };
                    int[] categories = { (int)categoryA, (int)categoryB };
                    int[] participantIndices = { 0, 1 };
                    int[] playerViewIDs = {
                        playerA.GetComponent<PhotonView>().ViewID,
                        playerB.GetComponent<PhotonView>().ViewID
                    };
                    PhotonView.Get(UIManager.Instance).RPC(
                        "RpcSetupFieldDuel", Photon.Pun.RpcTarget.All,
                        teamIndices, categories, participantIndices, playerViewIDs
                    );
                }
            }
            else // Singleplayer/offline
            {
                // Important: Register both participants BEFORE selections!
                DuelManager.Instance.RegisterTrigger(playerA.gameObject, false);
                DuelManager.Instance.RegisterTrigger(playerB.gameObject, false);

                UIManager.Instance.SetDuelSelection(playerA.TeamIndex, categoryA, 0, playerA);
                UIManager.Instance.SetDuelSelection(playerB.TeamIndex, categoryB, 1, playerB);
                UIManager.Instance.BeginDuelSelectionPhase();
            }
        }
    }

    private bool CanStartDuel()
    {
        bool isReady = Time.time >= _nextDuelAllowedTime
                       && !GameManager.Instance.IsMovementFrozen
                       && DuelManager.Instance.IsDuelResolved();
        return isReady;
    }

    private void SetDuelCooldown()
    {
        _nextDuelAllowedTime = Time.time + duelCooldown;
    }

    #endregion
}
