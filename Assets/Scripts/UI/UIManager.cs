using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public struct DuelTeamSelection
{
    public int ParticipantIndex;
    public Player Player;
    public Category Category;
}

public class UIManager : MonoBehaviourPun
{ // MonoBehaviourPun is Photon’s MonoBehaviour+PhotonView

    #region Singleton
    public static UIManager Instance { get; private set; }
    public bool IsStatusLocked { get; private set; } = false;
    #endregion

    #region Inspector References

    [Header("Panels & UI Elements")]
    [SerializeField] private GameObject panelSecret;
    [SerializeField] private GameObject panelCommand;
    [SerializeField] private PanelStatusSide panelStatusSideAlly;
    [SerializeField] private PanelStatusSide panelStatusSideOpp;
    [SerializeField] private GameObject panelWaitingForOpponent;
    [SerializeField] private GameObject buttonDuelToggle;
    [SerializeField] private GameObject buttonSwap;
    [SerializeField] private GameObject textWin;
    [SerializeField] private GameObject textLose;
    [SerializeField] private Image imageCategory;

    #endregion

    #region Fields

private bool _duelContextReady = false;
// Store picks received before context is ready
private List<(int teamIndex, int commandInt, string secretIdOrNull)> _pendingPicks = new();

    private DuelTeamSelection[] _duelSelections = new DuelTeamSelection[2];
    private bool[] _isTeamReady = new bool[2];
    private bool _selectionsRegistered = false;
    private DuelCommand[] _commands = new DuelCommand[2];
    private Secret[] _secrets = new Secret[2];

    private int shootTeamIndex;

    private float _selectionTimer = 10f;
    private bool _waitingForMultiplayerDuel = false;

private struct RemotePick { public int team, command; public string secret; }


    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[UIManager] Instance created.");
        }
        else
        {
            Debug.LogWarning("[UIManager] Duplicate instance detected, destroying object.");
            Destroy(gameObject);
            return;
        }
        HideStatus();
    }

    private void OnEnable() => SubscribeEvents(true);
    private void OnDisable() => SubscribeEvents(false);
    private void OnDestroy() => SubscribeEvents(false);

    private void SubscribeEvents(bool subscribe)
    {
        if (subscribe)
        {
            BallBehavior.OnSetStatusPlayer += SetStatusPlayer;
            DuelManager.OnSetStatusPlayer += SetStatusPlayer;
            DuelCollider.OnSetStatusPlayer += SetStatusPlayer;
            ComboCollider.OnSetStatusPlayer += SetStatusPlayer;
            KeeperCollider.OnSetStatusPlayer += SetStatusPlayer;
            DuelManager.OnSetStatusPlayerAndCommand += SetStatusPlayerAndCommand;
        }
        else
        {
            BallBehavior.OnSetStatusPlayer -= SetStatusPlayer;
            DuelManager.OnSetStatusPlayer -= SetStatusPlayer;
            DuelCollider.OnSetStatusPlayer -= SetStatusPlayer;
            ComboCollider.OnSetStatusPlayer -= SetStatusPlayer;
            KeeperCollider.OnSetStatusPlayer -= SetStatusPlayer;
            DuelManager.OnSetStatusPlayerAndCommand -= SetStatusPlayerAndCommand;
        }
    }
    #endregion

    #region Status Panel Lock/Unlock

    public void LockStatus() => IsStatusLocked = true;
    public void UnlockStatus() => IsStatusLocked = false;

    #endregion

    #region Panel & Button Visibility

    public void SetImageCategoryVisible(bool visible)
    {
        SetActiveSafe(imageCategory.gameObject, visible);
    }
    public void SetPanelSecretVisible(bool visible)
    {
        SetActiveSafe(panelSecret, visible);
    }
    public void SetPanelCommandVisible(bool visible)
    {
        SetActiveSafe(panelCommand, visible);
    }
    public void SetButtonDuelToggleVisible(bool visible)
    {
        SetActiveSafe(buttonDuelToggle, visible);
        SetActiveSafe(buttonSwap, visible);
        SetImageCategoryVisible(true);
        imageCategory.sprite = SecretManager.Instance.GetCategoryIcon(GetLocalCategory());
    }
    public void HideDuelUi()
    {
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);
        SetButtonDuelToggleVisible(false);
        SetImageCategoryVisible(false);
    }

    #endregion

    #region Duel & Command Button Logic

    public void OnButtonDuelToggleTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        if (!IsPanelActive(panelSecret) && !IsPanelActive(panelCommand))
            SetPanelCommandVisible(true);
        else
        {
            SetPanelCommandVisible(false);
            SetPanelSecretVisible(false);
        }
    }

    public void OnButtonBackTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuBack");
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(true);
    }

    public void OnCommand0Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxSecretCommand");
        SetPanelSecretVisible(true);
        SetPanelCommandVisible(false);
        var localSel = _duelSelections[GameManager.Instance.GetLocalTeamIndex()];
        if (localSel.Player != null && panelSecret != null)
        {
            var secretPanel = panelSecret.GetComponent<SecretPanel>();
            if (secretPanel != null)
                secretPanel.UpdateSecretSlots(localSel.Player.CurrentSecret, localSel.Category);
        }
    }

    public void OnCommand1Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        DuelSelectionMade(GameManager.Instance.GetLocalTeamIndex(), DuelCommand.Phys, null);
    }
    public void OnCommand2Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        DuelSelectionMade(GameManager.Instance.GetLocalTeamIndex(), DuelCommand.Skill, null);
    }
    public void OnSecretCommandSlotTapped(SecretCommandSlot secretCommandSlot)
    {
        if (secretCommandSlot?.Secret == null) return;
        var localSel = _duelSelections[GameManager.Instance.GetLocalTeamIndex()];
        if (localSel.Player.GetStat(PlayerStats.Sp) < secretCommandSlot.Secret.Cost)
        {
            AudioManager.Instance.PlaySfx("SfxForbidden");
            return;
        }

        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);

        AudioManager.Instance.PlaySfx("SfxSecretSelect");
        DuelSelectionMade(GameManager.Instance.GetLocalTeamIndex(), DuelCommand.Secret, secretCommandSlot.Secret);
    }
    #endregion

    #region Duel Selection Registration

    public void SetShootTeamIndex (int teamIndex) {
        shootTeamIndex = teamIndex;
    } 

    public void SetDuelSelection(int teamIndex, Category category, int participantIndex, Player player)
    {
        _duelSelections[teamIndex] = new DuelTeamSelection
        {
            ParticipantIndex = participantIndex,
            Player = player,
            Category = category
        };
    }

    private Category GetLocalCategory()
    {
        int localTeamIndex = GameManager.Instance.GetLocalTeamIndex();
        return _duelSelections[localTeamIndex].Category;
    }
    #endregion

    #region Duel Status Display

    public void ShowTextDuelResult(DuelParticipant winningPart)
    {
        if (winningPart?.Player == null) return;
        SetActiveSafe(textWin, winningPart.Player.ControlType == ControlType.LocalHuman);
        SetActiveSafe(textLose, winningPart.Player.ControlType != ControlType.LocalHuman);
    }

    public void HideStatus()
    {
        panelStatusSideAlly?.SetPlayer(null);
        panelStatusSideOpp?.SetPlayer(null);
        SetActiveSafe(textWin, false);
        SetActiveSafe(textLose, false);
    }

    public void HideStatusPlayer(Player player)
    {
        if (player == null) return;
        if (player.ControlType == ControlType.LocalHuman)
            panelStatusSideAlly?.SetPlayer(null);
        else
            panelStatusSideOpp?.SetPlayer(null);
    }

    public void SetStatusPlayer(Player player)
    {
        if (player == null) return;
        if (player.ControlType == ControlType.LocalHuman)
            panelStatusSideAlly?.SetPlayer(player);
        else
            panelStatusSideOpp?.SetPlayer(player);
    }

    public void SetStatusPlayerAndCommand(DuelParticipant duelParticipant, float attackPressure)
    {
        var player = duelParticipant?.Player;
        if (player == null) return;
        if (player.ControlType == ControlType.LocalHuman)
            panelStatusSideAlly?.SetPlayerAndCommand(duelParticipant, attackPressure);
        else
            panelStatusSideOpp?.SetPlayerAndCommand(duelParticipant, attackPressure);
    }
    #endregion

    #region Utility

    private void SetActiveSafe(GameObject obj, bool shouldBeActive)
    {
        if (obj != null && obj.activeSelf != shouldBeActive)
            obj.SetActive(shouldBeActive);
    }
    private bool IsPanelActive(GameObject obj) => obj != null && obj.activeSelf;
    private bool IsLocalTeamIndex(int teamIndex)
    {
        return teamIndex == GameManager.Instance.GetLocalTeamIndex();
    }

    #endregion

    #region Duel Selection Phase Logic

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
#endif
public void RpcSetupFieldDuel(int[] teamIndices, int[] categories, int[] participantIndices, int[] playerViewIDs)
{
    // 1. Reconstruct player refs from PhotonViews
    for (int i = 0; i < 2; i++)
    {
        var playerGO = PhotonView.Find(playerViewIDs[i]).gameObject;
        // This will "stage" them, so both clients have exact same stagedParticipants order!
        DuelManager.Instance.RegisterTrigger(playerGO, false); // isDirect should match what master did
        SetDuelSelection(teamIndices[i], (Category)categories[i], i, playerGO.GetComponent<Player>()); // i is always [0] then [1]
    }
    _duelContextReady = true;
    foreach (var pick in _pendingPicks)
        ProcessDuelPick(pick.teamIndex, pick.commandInt, pick.secretIdOrNull);
    _pendingPicks.Clear();
    BeginDuelSelectionPhase();
}

    public void BeginDuelSelectionPhase()
    {

        _selectionsRegistered = false;

        // Reset selection states so AI/Human can pick fresh!
        for (int i = 0; i < 2; i++)
        {
            _isTeamReady[i] = false;
            _commands[i] = DuelCommand.Phys; // or whatever default you want
            _secrets[i] = null;
        }

        GameManager.Instance.FreezeGame();
        GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Duel);

        DuelMode duelMode = DuelManager.Instance.GetDuelMode();
        if (GameManager.Instance.IsMultiplayer)
        {
            _isTeamReady[0] = _isTeamReady[1] = false;
            _commands[0] = _commands[1] = DuelCommand.Phys;
            _secrets[0] = _secrets[1] = null;
            _selectionTimer = 10f;
            _waitingForMultiplayerDuel = true;

            if (duelMode == DuelMode.Field)
            {
                ShowDuelUIForLocal();
                StartCoroutine(MultiplayerFieldDuelSelectionTimerRoutine());
            }
            else if (duelMode == DuelMode.Shoot)
            {
                Player localPlayer = _duelSelections[GameManager.Instance.GetLocalTeamIndex()].Player;
                if (localPlayer?.ControlType == ControlType.LocalHuman)
                {
                    ShowDuelUIForLocal();
                    StartCoroutine(MultiplayerShootDuelSelectionTimerRoutine(GameManager.Instance.GetLocalTeamIndex()));
                }
                else
                {
                    HideDuelUi();
                }
            }
        }
        else
        {
            if (duelMode == DuelMode.Field)
            {
        for (int i = 0; i < 2; i++)
    {

                        ShowDuelUIForLocal();   

        var sel = _duelSelections[i];
        if (sel.Player != null && sel.Player.ControlType == ControlType.Ai && !_isTeamReady[i])
        {
            var ai = sel.Player.GetComponent<PlayerAi>();
            if (ai != null)
            {
                ai.RegisterAiSelections(i, sel.Category);
            }
            else
            {
                // fallback: pick default
                _commands[i] = DuelCommand.Phys;
                _secrets[i] = null;
                _isTeamReady[i] = true;
            }
        }

     
            
        }

            }
            else if (duelMode == DuelMode.Shoot)
            {
                /*
                var sel = _duelSelections[i];
                if (sel.Player != null && sel.Player.ControlType == ControlType.Ai)
                */
                if (shootTeamIndex == GameManager.Instance.GetLocalTeamIndex())
                {
                    ShowDuelUIForLocal();
                }
                else
                {
                    HideDuelUi();

                    var sel = _duelSelections[shootTeamIndex];
                    if (sel.Player != null && sel.Player.ControlType == ControlType.Ai && !_isTeamReady[shootTeamIndex])
                    {
                        var ai = sel.Player.GetComponent<PlayerAi>();
                        if (ai != null)
                        {
                            ai.RegisterAiSelections(shootTeamIndex, sel.Category);
                        }
                        else
                        {
                            _commands[shootTeamIndex] = DuelCommand.Phys;
                            _secrets[shootTeamIndex] = null;
                            _isTeamReady[shootTeamIndex] = true;
                        }
                    }
                }
            }
        }
    }

    private void ShowDuelUIForLocal()
    {
        SetButtonDuelToggleVisible(true);
    }

    private IEnumerator MultiplayerFieldDuelSelectionTimerRoutine()
    {
        while (_waitingForMultiplayerDuel && _selectionTimer > 0f)
        {
            _selectionTimer -= Time.deltaTime;
            if (_isTeamReady[0] && _isTeamReady[1])
                break;
            yield return null;
        }
        _waitingForMultiplayerDuel = false;

        for (int i = 0; i < 2; i++)
        {
            if (!_isTeamReady[i])
            {
                var sel = _duelSelections[i];
                if (sel.Player != null && sel.Player.ControlType == ControlType.Ai)
                {
                    bool isMaster = !GameManager.Instance.IsMultiplayer
#if PHOTON_UNITY_NETWORKING
                        || PhotonNetwork.IsMasterClient
#endif
                        ;
                    if (isMaster)
                    {
                        var ai = sel.Player.GetComponent<PlayerAi>();
                        if (ai != null)
                        {
                            ai.RegisterAiSelections(i, sel.Category);
                        }
                        else
                        {
                            _commands[i] = DuelCommand.Phys;
                            _secrets[i] = null;
                            _isTeamReady[i] = true;
                            if (IsLocalTeamIndex(i))
                            {
                                SendSelectionToRemote(i, DuelCommand.Phys, null);
                            }
                        }
                    }
                }
                else
                {
                    _commands[i] = DuelCommand.Phys;
                    _secrets[i] = null;
                    _isTeamReady[i] = true;
                    if (IsLocalTeamIndex(i))
                    {
                        SendSelectionToRemote(i, DuelCommand.Phys, null);
                    }
                }
            }
        }
        if (_isTeamReady[0] && _isTeamReady[1]) RegisterBothSelections();
    }

    private IEnumerator MultiplayerShootDuelSelectionTimerRoutine(int shooterTeamIndex)
    {
        float timer = 10f;
        while (timer > 0f && !_isTeamReady[shooterTeamIndex])
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        if (!_isTeamReady[shooterTeamIndex])
        {
            _commands[shooterTeamIndex] = DuelCommand.Phys;
            _secrets[shooterTeamIndex] = null;
            _isTeamReady[shooterTeamIndex] = true;
            if (IsLocalTeamIndex(shooterTeamIndex))
            {
                SendSelectionToRemote(shooterTeamIndex, DuelCommand.Phys, null);
            }
        }

        RegisterShootSelectionAndClose(shooterTeamIndex);
    }

private void RegisterShootSelectionAndClose(int teamIndex)
{
    var sel = _duelSelections[teamIndex];
    DuelManager.Instance.RegisterSelection(sel.ParticipantIndex, sel.Category, _commands[teamIndex], _secrets[teamIndex]);

    if (_selectionsRegistered) return;
    _selectionsRegistered = true;
    _waitingForMultiplayerDuel = false;
    StopAllCoroutines();


    /*
    // Only master resolves!
#if PHOTON_UNITY_NETWORKING
    if (GameManager.Instance.IsMultiplayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            DuelManager.Instance.TryResolveFromSelections();
        }
    }
    else
#endif
    {
        DuelManager.Instance.TryResolveFromSelections();
    }
    */

    HideDuelUi();
    GameManager.Instance.UnfreezeGame();
    GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Battle);
}

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
#endif
public void NetworkDuelSelectionReceived(int teamIndex, int commandInt, string secretIdOrNull)
{
    if (!_duelContextReady)
    {
        Debug.LogWarning("NetworkDuelSelectionReceived called BEFORE duel context. Queuing pick!");
        _pendingPicks.Add((teamIndex, commandInt, secretIdOrNull));
        return;
    }
    ProcessDuelPick(teamIndex, commandInt, secretIdOrNull);
}

private void ProcessDuelPick(int teamIndex, int commandInt, string secretIdOrNull)
{
    _isTeamReady[teamIndex] = true;
    _commands[teamIndex] = (DuelCommand)commandInt;
    _secrets[teamIndex] = string.IsNullOrEmpty(secretIdOrNull) ? null : SecretManager.Instance.GetSecretById(secretIdOrNull);

    // Defensive log!
    Debug.Log($"ProcessDuelPick: team={teamIndex} participant={_duelSelections[teamIndex].ParticipantIndex} obj={_duelSelections[teamIndex].Player?.name}");

    // Only register BOTH when ready
    if (_isTeamReady[0] && _isTeamReady[1])
        RegisterBothSelections();
}
    private void SendSelectionToRemote(int teamIndex, DuelCommand command, Secret secret)
    {
#if PHOTON_UNITY_NETWORKING
        string secretIdOrNull = (secret == null) ? null : secret.SecretId;
        PhotonView.Get(this).RPC(
            "NetworkDuelSelectionReceived",
            Photon.Pun.RpcTarget.Others,
            teamIndex, (int)command, secretIdOrNull
        );
#endif
    }


#if PHOTON_UNITY_NETWORKING
    [PunRPC]
#endif
    public void RpcDuelOutcome(string winnerPlayerId, int winnerTeamIndex, int winnerActionInt)
    {
        // Find the winner's Player object using the ID
        Player winnerPlayer = null;
        // Search for player. You might have a manager, here's a basic version:
        foreach (var sel in _duelSelections)
        {
            if (sel.Player != null && sel.Player.PlayerId == winnerPlayerId && sel.Player.TeamIndex == winnerTeamIndex)
            {
                winnerPlayer = sel.Player;
                break;
            }
        }

        // Show duel result using your existing UI code
        //ShowTextDuelResult(winnerPlayer);
        HideDuelUi();
        GameManager.Instance.UnfreezeGame();
        GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Battle);

        // Optionally, play sounds, effects etc depending on win/loss
    }

// At the end of RegisterBothSelections:
private void RegisterBothSelections()
{
    if (_selectionsRegistered) return;
    _selectionsRegistered = true;
    _waitingForMultiplayerDuel = false;
    StopAllCoroutines();

    // sort _duelSelections/commands/secrets so that offense goes before defense
    var list = new List<(DuelTeamSelection sel, DuelCommand cmd, Secret secret)>();
    for (int i = 0; i < 2; i++)
    {
        list.Add((_duelSelections[i], _commands[i], _secrets[i]));
    }
    // Make offense first (assuming you have Category and can map it)
    list.Sort((a, b) => a.sel.Category == Category.Dribble ? -1 : 1);

    foreach (var entry in list)
    {
        DuelManager.Instance.RegisterSelection(entry.sel.ParticipantIndex, entry.sel.Category, entry.cmd, entry.secret);
    }

/*
    // ... the rest should be as normal
#if PHOTON_UNITY_NETWORKING
    if (GameManager.Instance.IsMultiplayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            DuelManager.Instance.TryResolveFromSelections();
        } else {
            DuelManager.Instance.TryResolveFromSelections();
        }
    }
    else
#endif
    {
        DuelManager.Instance.TryResolveFromSelections();
    }
*/
    HideDuelUi();
    GameManager.Instance.UnfreezeGame();
    GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Battle);
}

    void ShowWaitingForOpponentUI() => SetActiveSafe(panelWaitingForOpponent, true);
    void HideWaitingForOpponentUI() => SetActiveSafe(panelWaitingForOpponent, false);

    public void DuelSelectionMade(int teamIndex, DuelCommand command, Secret secret)
    {
        _isTeamReady[teamIndex] = true;
        _commands[teamIndex] = command;
        _secrets[teamIndex] = secret;

        if (GameManager.Instance.IsMultiplayer && IsLocalTeamIndex(teamIndex))
            HideDuelUi();

        if (GameManager.Instance.IsMultiplayer)
            SendSelectionToRemote(teamIndex, command, secret);

        DuelMode mode = DuelManager.Instance.GetDuelMode();
        if (mode == DuelMode.Field)
        {
                if (_isTeamReady[0] && _isTeamReady[1])
                    RegisterBothSelections();
        }
        else if (mode == DuelMode.Shoot)
        {
            RegisterShootSelectionAndClose(teamIndex);
        }
    }

    #endregion
}
