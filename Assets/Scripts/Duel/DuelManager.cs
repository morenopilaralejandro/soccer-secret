using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public enum DuelMode { Field, Shoot }
public enum DuelAction { Offense, Defense }
public enum DuelCommand { Phys, Skill, Secret }

public class DuelManager : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , IPunObservable
#endif
{
    public static DuelManager Instance { get; private set; }

    public static event Action<Player> OnSetStatusPlayer;
    public static event Action<DuelParticipant, float> OnSetStatusPlayerAndCommand;

    private List<DuelParticipantData> stagedParticipants = new List<DuelParticipantData>();
    private Duel currentDuel = new Duel();
    private Coroutine unlockStatusCoroutine;
    private float hpMultiplier = 0.1f;
    private float directBonus = 20f;
    private float keeperBonus = 50f;
    private float keeperGoalDistance = 0.5f;

#if PHOTON_UNITY_NETWORKING
    private PhotonView photonView => PhotonView.Get(this);
#endif

    #region Unity Lifecycle

    private void Awake()
    {
        // Standard Unity Singleton pattern:
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentDuel.IsResolved = true;
    }

    private void OnEnable()
    {
        if (BallTravelController.Instance != null)
        {
            BallTravelController.Instance.OnTravelEnd += HandleTravelEnd;
            BallTravelController.Instance.OnTravelCancel += CancelDuel;
        }
    }

    private void OnDisable()
    {
        if (BallTravelController.Instance != null)
        {
            BallTravelController.Instance.OnTravelEnd -= HandleTravelEnd;
            BallTravelController.Instance.OnTravelCancel -= CancelDuel;
        }
    }

    #endregion

    #region Duel Flow

    public void StartDuel(DuelMode mode)
    {
        // Only authority may start duel, everyone else only updates by RPC!
        if (GameManager.Instance.IsMultiplayer)
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(RPC_StartDuel), RpcTarget.All, (int)mode);
#endif
            return;
        }
        StartDuel_Internal(mode);
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_StartDuel(int modeInt) => StartDuel_Internal((DuelMode)modeInt);
#endif

    private void StartDuel_Internal(DuelMode mode)
    {
        Debug.Log("Duel started");
        StopAndCleanupUnlockStatus();
        UIManager.Instance.LockStatus();
        ResetDuel();
        currentDuel.Mode = mode;
        switch (mode)
        {
            case DuelMode.Shoot:
                AudioManager.Instance.PlaySfx("SfxDuelShoot");
                OnSetStatusPlayer?.Invoke(GameManager.Instance.GetOppKeeper(PossessionManager.Instance.PossessionPlayer));
                break;
            default:
                AudioManager.Instance.PlaySfx("SfxDuelField");
                break;
        }
    }

    public void ResetDuel()
    {
        stagedParticipants.Clear();
        currentDuel.Reset();
    }

    public void CancelDuel()
    {
        if (GameManager.Instance.IsMultiplayer)
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(RPC_CancelDuel), RpcTarget.All);
#endif
            return;
        }
        CancelDuel_Internal();
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_CancelDuel() => CancelDuel_Internal();
#endif

    private void CancelDuel_Internal()
    {
        currentDuel.IsResolved = true;
        ShootTriangle.Instance.SetTriangleVisible(false);
        BallTrail.Instance.SetTrailVisible(false);
        unlockStatusCoroutine = StartCoroutine(UnlockStatusRoutine());
    }

    public bool IsDuelResolved() => currentDuel.IsResolved;
    public DuelMode GetDuelMode() => currentDuel.Mode;
    public List<DuelParticipant> GetDuelParticipants() => currentDuel.Participants;
    public DuelParticipant GetLastOffense() => currentDuel.LastOffense;
    public DuelParticipant GetLastDefense() => currentDuel.LastDefense;

    public DuelAction GetActionByCategory(Category category)
    {
        if (category == Category.Block || category == Category.Catch) {
            return DuelAction.Defense;
        } else {
            return DuelAction.Offense;
        }
    }

    #endregion

    #region Duel Participation

    public void AddParticipantToDuel(DuelParticipant participant)
    {
        // This call should only be made by the master client in multiplayer
        if (GameManager.Instance.IsMultiplayer)
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(RPC_AddParticipant), RpcTarget.All, DuelParticipantNet.Serialize(participant));
#endif
            return;
        }
        AddParticipantToDuel_Internal(participant);
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_AddParticipant(object[] netData)
    {
        var participant = DuelParticipantNet.Deserialize(netData);
        AddParticipantToDuel_Internal(participant);
    }
#endif

    private void AddParticipantToDuel_Internal(DuelParticipant participant)
    {
        BallTravelController.Instance.ResumeTravel();

        if (currentDuel.IsResolved)
            return;

        if (participant.Category == Category.Shoot)
        {
            if (!currentDuel.Participants.Any())
            {
                PossessionManager.Instance.ReleasePossession();
                BallTravelController.Instance.StartTravel(ShootTriangle.Instance.GetRandomPoint());
            }
        }

        currentDuel.Participants.Add(participant);

        if (participant.Secret != null)
        {
            Vector3 playerPos = participant.Player.transform.position;
            SecretManager.Instance.PlaySecretEffect(participant.Secret, playerPos);
            participant.Player.ReduceSp(participant.Secret.Cost);
            if (participant.Category == Category.Shoot)
            {
                AudioManager.Instance.PlaySfx("SfxShootSpecial");
                BallTrail.Instance.SetTrailVisible(true);
                BallTrail.Instance.SetTrailMaterial(participant.Secret.Element);
            }
        }
        else
        {
            if (participant.Category == Category.Shoot)
            {
                AudioManager.Instance.PlaySfx("SfxShootRegular");
                BallTrail.Instance.SetTrailVisible(false);
            }
        }

        participant.Player.ReduceHp(Mathf.RoundToInt(participant.Player.Lv * hpMultiplier));

        if (participant.Action == DuelAction.Offense)
        {
            currentDuel.AttackPressure += participant.Damage;
            if (participant.Category == Category.Shoot && participant.IsDirect)
                currentDuel.AttackPressure += directBonus;
            currentDuel.LastOffense = participant;
            OnSetStatusPlayerAndCommand?.Invoke(participant, currentDuel.AttackPressure);
            Debug.Log($"Offense action increases attack pressure +{participant.Damage}");
        }
        else
        {
            ResolveDefense(participant);
        }
    }

    private void ResolveDefense(DuelParticipant defender)
    {
        if (currentDuel.LastOffense == null)
        {
            Debug.LogWarning("No offense present before defense.");
            return;
        }

        currentDuel.LastDefense = defender;

        ApplyElementalEffectiveness(currentDuel.LastOffense, defender);
        if (defender.Category == Category.Block && defender.Player.IsKeeper && GameManager.Instance.GetDistanceToAllyGoal(defender.Player) < keeperGoalDistance)
        {
            defender.Damage *= keeperBonus;
            Debug.Log("Keeper gets a block bonus!");
        }

        if (defender.Damage >= currentDuel.AttackPressure)
        {
            if (defender.Category == Category.Catch)
                AudioManager.Instance.PlaySfx("SfxCatch");

            OnSetStatusPlayerAndCommand?.Invoke(defender, 0f);
            Debug.Log($"{defender.Player.name} stopped the attack! (-{defender.Damage})");
            EndDuel(winningParticipant: defender, winnerAction: DuelAction.Defense);
        }
        else
        {
            currentDuel.AttackPressure -= defender.Damage;
            OnSetStatusPlayerAndCommand?.Invoke(defender, 0f);
            Debug.Log($"Partial block. Attack pressure now {currentDuel.AttackPressure}");

            defender.Player.Stun();

            if (currentDuel.Mode == DuelMode.Field || defender.Category == Category.Catch)
            {
                if (defender.Category == Category.Catch)
                    AudioManager.Instance.PlaySfx("SfxKeeperScream");

                Debug.Log("Partial block ends the duel.");
                EndDuel(winningParticipant: currentDuel.LastOffense, winnerAction: DuelAction.Offense);
            }
            // Else: duel continues for next defense
        }
    }

    private void ApplyElementalEffectiveness(DuelParticipant offense, DuelParticipant defense)
    {
        var elements = ElementManager.Instance;

        if (elements.IsEffective(defense.CurrentElement, offense.CurrentElement))
        {
            defense.Damage *= 2f;
            Debug.Log("Defense element is effective!");
        }
        else if (elements.IsEffective(offense.CurrentElement, defense.CurrentElement))
        {
            currentDuel.AttackPressure -= offense.Damage;
            offense.Damage *= 2;
            currentDuel.AttackPressure += offense.Damage;
            OnSetStatusPlayerAndCommand?.Invoke(offense, currentDuel.AttackPressure);
            Debug.Log("Offense element is effective!");
        }
    }

    private void EndDuel(DuelParticipant winningParticipant, DuelAction winnerAction)
    {
        if (GameManager.Instance.IsMultiplayer)
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                // Instead of sending the full participant, send identifying info (PlayerId, etc.)
                photonView.RPC(nameof(RPC_EndDuel), RpcTarget.All, winningParticipant.Player.PlayerId, winningParticipant.Player.TeamIndex, (int)winnerAction);
            }
#endif
            return;
        }
        EndDuel_Internal(winningParticipant, winnerAction);
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_EndDuel(string playerId, int teamIndex, int winnerActionInt)
    {
        // Find the winning DuelParticipant object from the ID/side:
        DuelParticipant winPart = currentDuel.Participants.Find(
            p => p.Player.PlayerId == playerId && p.Player.TeamIndex == teamIndex
        );
        EndDuel_Internal(winPart, (DuelAction)winnerActionInt);
    }
#endif

    private void EndDuel_Internal(DuelParticipant winningParticipant, DuelAction winnerAction)
    {
        if (winningParticipant.Player.ControlType == ControlType.LocalHuman)
        {
            AudioManager.Instance.PlaySfx("SfxDuelWin");
        }
        else
        {
            AudioManager.Instance.PlaySfx("SfxDuelLose");
        }

        winningParticipant.Player.ReduceHp(Mathf.RoundToInt(winningParticipant.Player.Lv * hpMultiplier));
        currentDuel.IsResolved = true;
        UIManager.Instance.ShowTextDuelResult(winningParticipant);
        ShootTriangle.Instance.SetTriangleVisible(false);

        if (winnerAction == DuelAction.Defense)
        {
            BallTravelController.Instance.CancelTravel();
            PossessionManager.Instance.GainPossession(winningParticipant.Player);
            currentDuel.LastOffense.Player.Stun();
        }

        BallTrail.Instance.SetTrailVisible(false);
        unlockStatusCoroutine = StartCoroutine(UnlockStatusRoutine());
        Debug.Log("Duel ended");
    }

    #endregion

    #region Ball and Status Control

    private void HandleTravelEnd(Vector3 end)
    {
        if (!currentDuel.IsResolved)
        {
            CancelDuel();
        }
    }

    private void StopAndCleanupUnlockStatus()
    {
        if (unlockStatusCoroutine != null)
        {
            StopCoroutine(unlockStatusCoroutine);
            unlockStatusCoroutine = null;
        }
        if (currentDuel.IsResolved)
        {
            UIManager.Instance.HideStatus();
            if (PossessionManager.Instance.PossessionPlayer != null)
                UIManager.Instance.SetStatusPlayer(PossessionManager.Instance.PossessionPlayer);
        }
    }

    private IEnumerator UnlockStatusRoutine()
    {
        const float unlockDelay = 2f;
        yield return new WaitForSeconds(unlockDelay);

        StopAndCleanupUnlockStatus();

        UIManager.Instance.UnlockStatus();
    }

    #endregion

    #region Participant Registration

    public void RegisterTrigger(GameObject obj, bool isDirect)
    {
        var pd = new DuelParticipantData { GameObject = obj, IsDirect = isDirect };
        stagedParticipants.Add(pd);
        TryFinalizeParticipant(pd);
    }

    public void RegisterSelection(int index, Category category, DuelCommand command, Secret secret)
    {
        Debug.Log($"[DuelManager] RegisterSelection index={index}, category={category}, command={command}, secret={(secret != null ? secret.name : "None")}, stagedCount={stagedParticipants.Count}");
        if (index < 0 || index >= stagedParticipants.Count)
        {
            Debug.LogError("Invalid participant index");
            return;
        }
        var pd = stagedParticipants[index];
        pd.Category = category;
        pd.Action = GetActionByCategory(category);
        pd.Command = command;
        pd.Secret = secret;
        TryFinalizeParticipant(pd);
    }

    private void TryFinalizeParticipant(DuelParticipantData pd)
    {
        if (!pd.IsComplete) return;

        var participant = new DuelParticipant(
            pd.GameObject,
            pd.Category.Value,
            pd.Action.Value,
            pd.Command.Value,
            pd.Secret,
            pd.IsDirect
        );

        Debug.Log($"Created participant: {participant.Player.name}");
        AddParticipantToDuel(participant);  // Will use authority pattern now
    }

    #endregion

#if PHOTON_UNITY_NETWORKING
    // You could implement OnPhotonSerializeView if you want to sync fields (rarely needed with this RPC approach)
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // (No extra sync needed for logic; all state is managed by explicit RPCs)
    }
#endif
}
