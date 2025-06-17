using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public struct DuelTeamSelection
{
    public int ParticipantIndex;
    public Player Player;
    public Category Category;
}

public class UIManager : MonoBehaviour
{
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
    [SerializeField] private GameObject buttonDuelToggle;
    [SerializeField] private GameObject buttonSwap;
    [SerializeField] private GameObject textWin;
    [SerializeField] private GameObject textLose;
    [SerializeField] private Image imageCategory;

    #endregion

    #region Fields

    private DuelTeamSelection[] _duelSelections = new DuelTeamSelection[2];
    private bool[] _isTeamReady = new bool[2];
    private bool _selectionsRegistered = false;
    private DuelCommand[] _commands = new DuelCommand[2];
    private Secret[] _secrets = new Secret[2];

    private float _selectionTimer = 10f;
    private bool _waitingForMultiplayerDuel = false;

    #endregion

    // ============================== 
    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[UIManager] Instance created.");
            // Uncomment to persist across scenes if required
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("[UIManager] Duplicate instance detected, destroying object.");
            Destroy(gameObject);
            return;
        }
        HideStatus();
    }

    private void OnEnable()
    {
        Debug.Log("[UIManager] Subscribing to events.");
        SubscribeEvents(true);
    }

    private void OnDisable()
    {
        Debug.Log("[UIManager] Unsubscribing from events (OnDisable).");
        SubscribeEvents(false);
    }

    private void OnDestroy()
    {
        Debug.Log("[UIManager] Unsubscribing from events (OnDestroy).");
        SubscribeEvents(false);
    }

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
            Debug.Log("[UIManager] Subscribed to all relevant events.");
        }
        else
        {
            BallBehavior.OnSetStatusPlayer -= SetStatusPlayer;
            DuelManager.OnSetStatusPlayer -= SetStatusPlayer;
            DuelCollider.OnSetStatusPlayer -= SetStatusPlayer;
            ComboCollider.OnSetStatusPlayer -= SetStatusPlayer;
            KeeperCollider.OnSetStatusPlayer -= SetStatusPlayer;
            DuelManager.OnSetStatusPlayerAndCommand -= SetStatusPlayerAndCommand;
            Debug.Log("[UIManager] Unsubscribed from all relevant events.");
        }
    }
    #endregion

    // ==============================
    #region Status Panel Lock/Unlock

    public void LockStatus()
    {
        Debug.Log("[UIManager] Status locked.");
        IsStatusLocked = true;
    }

    public void UnlockStatus()
    {
        Debug.Log("[UIManager] Status unlocked.");
        IsStatusLocked = false;
    }

    #endregion

    // ==============================
    #region Panel & Button Visibility

    public void SetImageCategoryVisible(bool visible)
    {
        Debug.Log($"[UIManager] SetImageCategoryVisible: {visible}");
        SetActiveSafe(imageCategory.gameObject, visible);
    }

    public void SetPanelSecretVisible(bool visible)
    {
        Debug.Log($"[UIManager] SetPanelSecretVisible: {visible}");
        SetActiveSafe(panelSecret, visible);
    }

    public void SetPanelCommandVisible(bool visible)
    {
        Debug.Log($"[UIManager] SetPanelCommandVisible: {visible}");
        SetActiveSafe(panelCommand, visible);
    }

    public void SetButtonDuelToggleVisible(bool visible)
    {
        Debug.Log($"[UIManager] SetButtonDuelToggleVisible: {visible}");
        SetActiveSafe(buttonDuelToggle, visible);
        SetActiveSafe(buttonSwap, visible);
        SetImageCategoryVisible(true);
        imageCategory.sprite = SecretManager.Instance.GetCategoryIcon(GetLocalCategory());
    }

    public void HideDuelUi()
    {
        Debug.Log("[UIManager] HideDuelUi triggered.");
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);
        SetButtonDuelToggleVisible(false);
        SetImageCategoryVisible(false);
    }

    #endregion

    // ==============================
    #region Duel & Command Button Logic

    public void OnButtonDuelToggleTapped()
    {
        Debug.Log("[UIManager] ButtonDuelToggle tapped!");
        AudioManager.Instance.PlaySfx("SfxMenuTap");

        if (!IsPanelActive(panelSecret) && !IsPanelActive(panelCommand))
        {
            SetPanelCommandVisible(true);
        }
        else
        {
            SetPanelCommandVisible(false);
            SetPanelSecretVisible(false);
        }
    }

    public void OnButtonBackTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuBack");
        Debug.Log("[UIManager] ButtonBack tapped!");
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(true);
    }

    public void OnCommand0Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxSecretCommand");
        Debug.Log("[UIManager] Command0 tapped!");
        SetPanelSecretVisible(true);
        SetPanelCommandVisible(false);
        var localSel = _duelSelections[GameManager.Instance.GetLocalTeamIndex()];
        if (localSel.Player != null && panelSecret != null)
        {
            var secretPanel = panelSecret.GetComponent<SecretPanel>();
            if (secretPanel != null)
            {
                secretPanel.UpdateSecretSlots(localSel.Player.CurrentSecret, localSel.Category);
                Debug.Log("[UIManager] SecretPanel updated.");
            }
            else
            {
                Debug.LogWarning("[UIManager] SecretPanel component is null!");
            }
        }
    }

    public void OnCommand1Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        Debug.Log("[UIManager] Command1 tapped!");
        UIManager.Instance.DuelSelectionMade(GameManager.Instance.GetLocalTeamIndex(), DuelCommand.Phys, null);
    }

    public void OnCommand2Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        Debug.Log("[UIManager] Command2 tapped!");
        UIManager.Instance.DuelSelectionMade(GameManager.Instance.GetLocalTeamIndex(), DuelCommand.Skill, null);
    }

    public void OnSecretCommandSlotTapped(SecretCommandSlot secretCommandSlot)
    {
        Debug.Log($"[UIManager] SecretCommandSlot tapped: {secretCommandSlot?.name}");

        if (secretCommandSlot?.Secret == null)
        {
            Debug.LogWarning("[UIManager] SecretCommandSlot has no secret assigned.");
            return;
        }

        var localSel = _duelSelections[GameManager.Instance.GetLocalTeamIndex()];
        if (localSel.Player.GetStat(PlayerStats.Sp) < secretCommandSlot.Secret.Cost)
        {
            AudioManager.Instance.PlaySfx("SfxForbidden");
            Debug.LogWarning("[UIManager] Not enough SP to select this Secret!");
            return;
        }

        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);

        AudioManager.Instance.PlaySfx("SfxSecretSelect");
        UIManager.Instance.DuelSelectionMade(GameManager.Instance.GetLocalTeamIndex(), DuelCommand.Secret, secretCommandSlot.Secret);
    }

    #endregion

    // ==============================
    #region Duel Selection Registration

    public void SetDuelSelection(int teamIndex, Category category, int participantIndex, Player player)
    {
        Debug.Log($"[UIManager] SetDuelSelection: Team {teamIndex}, Category {category}, Index {participantIndex}, Player {player?.name}");
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
        Category myCategory = _duelSelections[localTeamIndex].Category;
        return myCategory;
    }

    public void RegisterDuelSelections(DuelCommand command, Secret secret)
    {
        // Always "local" team as primary
        int localTeamIndex = GameManager.Instance.GetLocalTeamIndex();
        var localSel = _duelSelections[localTeamIndex];

        Debug.Log($"[UIManager] Registering DuelSelections: Command {command}, Secret {(secret ? secret.name : "None")}");
        DuelManager.Instance.RegisterSelection(localSel.ParticipantIndex, localSel.Category, command, secret);

        // Optionally, register for the other team if needed (for singleplayer AI)
        /*
        int oppTeamIndex = 1 - localTeamIndex;
        var oppSel = _duelSelections[oppTeamIndex];
        if (oppSel.Category == Category.Dribble && oppSel.Player != null && oppSel.Player.ControlType == ControlType.Ai)
        {
            oppSel.Player.GetComponent<PlayerAi>().RegisterAiSelections(oppSel.ParticipantIndex, oppSel.Category);
        }
    */
    }
    #endregion

    // ==============================
    #region Duel Status Display

    public void ShowTextDuelResult(DuelParticipant winningPart)
    {
        if (winningPart?.Player == null)
            return;

        SetActiveSafe(textWin, winningPart.Player.ControlType == ControlType.LocalHuman);
        SetActiveSafe(textLose, winningPart.Player.ControlType != ControlType.LocalHuman);
        Debug.Log($"[UIManager] ShowTextDuelResult: Winner is {winningPart.Player?.name}");
    }

    public void HideStatus()
    {
        Debug.Log("[UIManager] HideStatus called.");
        panelStatusSideAlly?.SetPlayer(null);
        panelStatusSideOpp?.SetPlayer(null);
        SetActiveSafe(textWin, false);
        SetActiveSafe(textLose, false);
    }

    public void HideStatusPlayer(Player player)
    {
        if (player == null) return;

        Debug.Log($"[UIManager] HideStatusPlayer: {player?.name}");

        if (player.ControlType == ControlType.LocalHuman)
            panelStatusSideAlly?.SetPlayer(null);
        else
            panelStatusSideOpp?.SetPlayer(null);
    }

    public void SetStatusPlayer(Player player)
    {
        if (player == null) return;

        Debug.Log($"[UIManager] SetStatusPlayer: {player?.name}, {player.ControlType}");

        if (player.ControlType == ControlType.LocalHuman)
            panelStatusSideAlly?.SetPlayer(player);
        else
            panelStatusSideOpp?.SetPlayer(player);
    }

    public void SetStatusPlayerAndCommand(DuelParticipant duelParticipant, float attackPressure)
    {
        var player = duelParticipant?.Player;
        if (player == null) return;

        Debug.Log($"[UIManager] SetStatusPlayerAndCommand: {player?.name}, Pressure {attackPressure}");

        if (player.ControlType == ControlType.LocalHuman)
            panelStatusSideAlly?.SetPlayerAndCommand(duelParticipant, attackPressure);
        else
            panelStatusSideOpp?.SetPlayerAndCommand(duelParticipant, attackPressure);
    }
    #endregion

    // ==============================
    #region Utility

    /// <summary>
    /// Sets a GameObject active only if it exists and if the state is different than desired
    /// </summary>
    private void SetActiveSafe(GameObject obj, bool shouldBeActive)
    {
        if (obj != null && obj.activeSelf != shouldBeActive)
        {
            obj.SetActive(shouldBeActive);
            Debug.Log($"[UIManager] SetActiveSafe: {obj.name}, Active: {shouldBeActive}");
        }
    }

    /// <summary>
    /// Returns true if a panel GameObject is non-null and active.
    /// </summary>
    private bool IsPanelActive(GameObject obj) => obj != null && obj.activeSelf;

    private bool IsLocalTeamIndex(int teamIndex)
    {
        return teamIndex == GameManager.Instance.GetLocalTeamIndex();
    }

    #endregion

    // ==============================
    #region Duel Selection Phase Logic

 public void BeginDuelSelectionPhase()
{
    Debug.Log("[UIManager] BeginDuelSelectionPhase called.");
    _selectionsRegistered = false;
    GameManager.Instance.FreezeGame();
    GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Duel);

    DuelMode duelMode = DuelManager.Instance.GetDuelMode();

    if (GameManager.Instance.IsMultiplayer)
    {
        Debug.Log("[UIManager] Multiplayer duel selection phase started.");

        _isTeamReady[0] = _isTeamReady[1] = false;
        _commands[0] = _commands[1] = DuelCommand.Phys;
        _secrets[0] = _secrets[1] = null;
        _selectionTimer = 10f;
        _waitingForMultiplayerDuel = true;

        if (duelMode == DuelMode.Field)
        {
            Debug.Log("[UIManager] DuelMode.Field: Both teams need to select.");
            ShowDuelUIForLocal();
            StartCoroutine(MultiplayerFieldDuelSelectionTimerRoutine());
        }
        else if (duelMode == DuelMode.Shoot)
        {
            Player localPlayer = _duelSelections[GameManager.Instance.GetLocalTeamIndex()].Player;
            if (localPlayer?.ControlType == ControlType.LocalHuman)
            {
                Debug.Log("[UIManager] DuelMode.Shoot: Only shooter (local human) selects.");
                ShowDuelUIForLocal();
                StartCoroutine(MultiplayerShootDuelSelectionTimerRoutine(GameManager.Instance.GetLocalTeamIndex()));
            }
            else
            {
                Debug.Log("[UIManager] DuelMode.Shoot: Not local human shooter, hiding UI.");
                HideDuelUi();
            }
        }
    }
    else
    {
        Debug.Log("[UIManager] Singleplayer duel selection phase started. Showing duel UI for local player.");
        ShowDuelUIForLocal();
    }
}
    private void ShowDuelUIForLocal()
    {
        SetButtonDuelToggleVisible(true);
        Debug.Log("[UIManager] Showing duel UI for local player.");
    }

 private IEnumerator MultiplayerFieldDuelSelectionTimerRoutine()
{
    Debug.Log("[UIManager] MultiplayerFieldDuelSelectionTimerRoutine started.");
    while (_waitingForMultiplayerDuel && _selectionTimer > 0f)
    {
        _selectionTimer -= Time.deltaTime;
        // update timer UI if you want
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
                Debug.Log($"[UIManager] Timer expired: Forcing AI pick for Team {i} ({sel.Player.name})");
                var ai = sel.Player.GetComponent<PlayerAi>();
                if (ai != null)
                {
                    ai.RegisterAiSelections(i, sel.Category);
                }
                else
                {
                    // Fallback: if for some reason AI component is not found
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
            // For humans, default to Phys and notify remote as needed
            _commands[i] = DuelCommand.Phys;
            _secrets[i] = null;
            _isTeamReady[i] = true;
            Debug.Log($"[UIManager] Team {i} not ready - defaulted to Phys.");
            if (IsLocalTeamIndex(i))
            {
                SendSelectionToRemote(i, DuelCommand.Phys, null);
            }
        }
    }
}
    if (_isTeamReady[0] && _isTeamReady[1])
    {
        RegisterBothSelections();
    }
    Debug.Log("[UIManager] MultiplayerFieldDuelSelectionTimerRoutine complete.");
}

// NEW: This coroutine runs only for shooter selection in Shoot duel mode
private IEnumerator MultiplayerShootDuelSelectionTimerRoutine(int shooterTeamIndex)
{
    Debug.Log("[UIManager] MultiplayerShootDuelSelectionTimerRoutine started.");
    float timer = 10f;
    while (timer > 0f && !_isTeamReady[shooterTeamIndex])
    {
        timer -= Time.deltaTime;
        // update timer UI if you want
        yield return null;
    }

    if (!_isTeamReady[shooterTeamIndex])
    {
        _commands[shooterTeamIndex] = DuelCommand.Phys;
        _secrets[shooterTeamIndex] = null;
        _isTeamReady[shooterTeamIndex] = true;
        Debug.Log($"[UIManager] Shooter team {shooterTeamIndex} not ready - defaulted to Phys.");
        if (IsLocalTeamIndex(shooterTeamIndex))
        {
            SendSelectionToRemote(shooterTeamIndex, DuelCommand.Phys, null);
        }
    }

    // Register only the shooter's selection
    RegisterShootSelectionAndClose(shooterTeamIndex);
    Debug.Log("[UIManager] MultiplayerShootDuelSelectionTimerRoutine complete.");
}

private void RegisterShootSelectionAndClose(int teamIndex)
{
    // Always register the pick!
    var sel = _duelSelections[teamIndex];
    DuelManager.Instance.RegisterSelection(sel.ParticipantIndex, sel.Category, _commands[teamIndex], _secrets[teamIndex]);

    // Only resolve duel/ui/game ONCE
    if (_selectionsRegistered) return;
    _selectionsRegistered = true;
    _waitingForMultiplayerDuel = false;
    StopAllCoroutines();

    HideDuelUi();
    GameManager.Instance.UnfreezeGame();
    GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Battle);
}

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
#endif
    public void NetworkDuelSelectionReceived(int teamIndex, int commandInt, string secretIdOrNull)
    {
        _isTeamReady[teamIndex] = true;
        _commands[teamIndex] = (DuelCommand)commandInt;
        _secrets[teamIndex] = SecretManager.Instance.GetSecretById(secretIdOrNull);

        Debug.Log($"[UIManager] NetworkDuelSelectionReceived: Team {teamIndex} | Command {commandInt} | SecretId {(string.IsNullOrEmpty(secretIdOrNull) ? "None" : secretIdOrNull)}");

        if (_isTeamReady[0] && _isTeamReady[1])
        {
            RegisterBothSelections();
        }
    }

    private void SendSelectionToRemote(int teamIndex, DuelCommand command, Secret secret)
    {
#if PHOTON_UNITY_NETWORKING
        string secretIdOrNull = (secret == null) ? null : secret.SecretId;
        Photon.Pun.PhotonView.Get(this).RPC(
            "NetworkDuelSelectionReceived",
            Photon.Pun.RpcTarget.Others,
            teamIndex, (int)command, secretIdOrNull
        );
        Debug.Log($"[UIManager] Sent selection to remote: Team {teamIndex}, Command {command}, SecretId {(secret == null ? "None" : secret.SecretId)}");
#endif
    }

// Use this only for "field" mode:
private void RegisterBothSelections()
{
    if (_selectionsRegistered) return;
    _selectionsRegistered = true;
    _waitingForMultiplayerDuel = false;
    StopAllCoroutines();

    for (int i = 0; i < 2; i++)
    {
        var sel = _duelSelections[i];
        DuelManager.Instance.RegisterSelection(sel.ParticipantIndex, sel.Category, _commands[i], _secrets[i]);
        Debug.Log($"[UIManager] RegisterBothSelections: Team {i}, Command {_commands[i]}, Secret {(_secrets[i] != null ? _secrets[i].name : "None")}");
    }
    HideDuelUi();
    GameManager.Instance.UnfreezeGame();
    GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Battle);
}

public void DuelSelectionMade(int teamIndex, DuelCommand command, Secret secret)
{
    _isTeamReady[teamIndex] = true;
    _commands[teamIndex] = command;
    _secrets[teamIndex] = secret;
    Debug.Log($"[UIManager] DuelSelectionMade: Team {teamIndex}, Command {command}, Secret {(secret != null ? secret.name : "None")}");

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
    // For shoot duels, handle everything ONLY in RegisterShootSelectionAndClose
    RegisterShootSelectionAndClose(teamIndex);
}
}

    #endregion
}
